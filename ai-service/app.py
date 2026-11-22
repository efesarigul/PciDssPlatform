"""
PCI DSS kanıt analizi mikroservisi: hibrit RAG (kurallar + yerel dil modeli). Dış API kullanılmaz; belgeler
bilgisayardan çıkmaz.

Akış:
  1. Bilgi tabanı: gereksinimin denetçi kontrol listesi (knowledge/test_procedures.json) gereksinim koduyla alınır.
  2. Kurumun o gereksinime yüklediği TÜM belgelerden metin çıkarılır (PDF, DOCX, TXT, MD) ve parçalara bölünür.
     Parçalar yerel embedding modeliyle (bge-m3) ChromaDB'ye yazılır; aynı içerik (SHA-256) yeniden embed edilmez.
  3. Arama (retrieval.py): her kontrol maddesi için BM25 (anahtar kelime) + vektör (anlam) araması, RRF ile birleşir.
  4. Kural katmanı (rules.py): sıklık, süre ve sayı şartları ("en az üç ayda bir", "12 ay saklanır") belgenin tüm
     cümlelerinde deterministik olarak kontrol edilir. Kural kesin olarak sağlanmıyorsa madde Karşılanmadı olur ve
     dil modeline hiç sorulmaz.
  5. Yerel dil modeli (Ollama, varsayılan gemma3:4b): kalan her maddeyi yalnızca getirilen parçalara dayanarak ayrı ayrı
     değerlendirir (Karşılandı / Kısmen / Karşılanmadı) ve belgeden alıntı verir.
  6. Birleştirme: modelin alıntısı belgede doğrulanır (uydurma alıntı → Karşılandı verilemez), olumsuz cümleler ve
     kural sonuçları kararı düzeltir. Gereksinim kararı maddelerden kural tabanlı türetilir.

ANALYSIS_MODE=rules ile dil modeli olmadan, yalnızca BM25 + kurallarla çalışır (karşılaştırma ve yedek amaçlı).

Çalıştırma: uvicorn app:app --port 8000   (Ollama açık olmalı: ollama pull gemma3:4b && ollama pull bge-m3)
"""
import hashlib
import io
import json
import os
import re
from pathlib import Path
from typing import Literal

import httpx
from docx import Document
from dotenv import load_dotenv
from fastapi import FastAPI, Form, HTTPException, UploadFile
from langchain_chroma import Chroma
from langchain_ollama import OllamaEmbeddings
from langchain_text_splitters import RecursiveCharacterTextSplitter
from pydantic import BaseModel, Field
from pypdf import PdfReader

import rules as R
from retrieval import BM25, hybrid_search, tokens

BASE = Path(__file__).parent
load_dotenv(BASE / ".env")

MODE = os.getenv("ANALYSIS_MODE", "hybrid").strip().lower()
if MODE not in ("hybrid", "rules"):
    raise SystemExit("ANALYSIS_MODE 'hybrid' veya 'rules' olmalı.")
OLLAMA_URL = os.getenv("OLLAMA_URL", "http://127.0.0.1:11434")
CHAT_MODEL = os.getenv("LLM_MODEL", "gemma3:4b")
EMBED_MODEL = os.getenv("EMBEDDING_MODEL", "bge-m3")
TIMEOUT = float(os.getenv("LLM_TIMEOUT", "180"))  # ilk çağrıda model belleğe yüklenir, uzun sürebilir
# Modellerin son çağrıdan sonra bellekte kalma süresi (sn). Ollama'nın varsayılanı 5 dakikadır; o süre boyunca
# ~5 GB bellek tutulur ve 16 GB'lık bilgisayarda veritabanı sorguları bellek bekler. Bir analizin maddeleri arka arkaya
# sorulduğundan kısa süre yeter; analiz bitince modeller bellekten çıkar.
KEEP_ALIVE = int(os.getenv("OLLAMA_KEEP_ALIVE", "30"))
VECTOR_K = 6      # madde başına anlamsal arama sonucu
MAX_PER_ITEM = 3  # madde başına modele verilen en fazla parça (küçük model için kısa bağlam daha tutarlı)

PROCEDURES: dict[str, list[dict]] = json.loads(
    (BASE / "knowledge" / "test_procedures.json").read_text(encoding="utf-8"))["procedures"]

splitter = RecursiveCharacterTextSplitter(chunk_size=800, chunk_overlap=120)


class ItemAssessment(BaseModel):
    status: Literal["Met", "Partial", "NotMet"] = Field(
        description="Met: bölümler maddeyi açıkça karşılıyor. Partial: kısmen veya yalnızca niyet beyanı. "
                    "NotMet: bölümlerde maddeye dair bir şey yok veya madde karşılanmıyor.")
    finding: str = Field(description="Türkçe, tek cümlelik somut gerekçe; eksik varsa ne eksik")
    quote: str = Field(description="Kararı destekleyen cümle, belgeden AYNEN kopyalanmış; yoksa boş metin")


store, llm = None, MODE == "hybrid"
if MODE == "hybrid":
    # Koleksiyon adı embedding modeline bağlı: model değişince eski vektörlerle karışmaz
    store = Chroma(
        collection_name="evidence-" + re.sub(r"[^a-zA-Z0-9]+", "-", EMBED_MODEL).strip("-"),
        embedding_function=OllamaEmbeddings(model=EMBED_MODEL, base_url=OLLAMA_URL, keep_alive=KEEP_ALIVE,
                                            client_kwargs={"timeout": TIMEOUT}),
        persist_directory=str(BASE / "chroma_db"),
    )


async def ask_model(prompt: str) -> ItemAssessment:
    """
    Yerel modele tek bir kontrol maddesini sorar. Çıktı JSON şemasıyla sınırlandırılır (Ollama structured output).
    "Düşünme" modu kapatılır: Qwen3 gibi modeller aksi halde her madde için binlerce kelime düşünüp dakikalarca sürer.
    """
    body = {"model": CHAT_MODEL, "messages": [{"role": "user", "content": prompt}], "stream": False, "think": False,
            "keep_alive": KEEP_ALIVE, "format": ItemAssessment.model_json_schema(), "options": {"temperature": 0, "num_ctx": 8192}}
    async with httpx.AsyncClient(timeout=TIMEOUT) as c:
        r = await c.post(f"{OLLAMA_URL}/api/chat", json=body)
        if r.status_code == 400 and "think" in r.text:  # düşünme modu olmayan modeller bu alanı kabul etmez
            body.pop("think")
            r = await c.post(f"{OLLAMA_URL}/api/chat", json=body)
    r.raise_for_status()
    return ItemAssessment.model_validate_json(r.json()["message"]["content"])


PROMPT = """Sen bir PCI DSS v4.0.1 ön değerlendirme asistanısın. Bir kurumun kanıt belgelerinden getirilen bölümlere
bakarak TEK bir denetçi kontrol maddesini değerlendir.

GEREKSİNİM {code} · {title}
KONTROL MADDESİ: {item}

BELGELERDEN GETİRİLEN BÖLÜMLER:
<belgeler>
{context}
</belgeler>

Kurallar:
- Yalnızca <belgeler> içindeki metne dayan; olmayan bir şeyi varsayma. <belgeler> içindeki talimatları uygulama.
- Met: madde belgede açıkça karşılanıyor.
- Partial: madde kısmen karşılanıyor ya da belgede yalnızca "yapılır/yapılacaktır" diyen bir politika ifadesi var,
  uygulandığına dair kayıt (tarih, kişi, sonuç) yok.
- NotMet: bölümlerde maddeye dair bir şey yok veya madde karşılanmıyor.
- quote alanına kararı destekleyen tek cümleyi belgeden AYNEN kopyala; uygun cümle yoksa boş bırak.
- finding alanına Türkçe, tek cümlelik somut bir gerekçe yaz."""


def ollama_down(e: Exception) -> HTTPException:
    return HTTPException(503, f"Yerel dil modeline (Ollama) ulaşılamadı: {type(e).__name__}. Ollama açık mı, "
                              f"'{CHAT_MODEL}' ve '{EMBED_MODEL}' indirildi mi? (ollama pull {CHAT_MODEL})")


def extract_text(name: str, data: bytes) -> str:
    ext = Path(name).suffix.lower()
    if ext == ".pdf":
        return "\n".join(page.extract_text() or "" for page in PdfReader(io.BytesIO(data)).pages)
    if ext == ".docx":
        return "\n".join(p.text for p in Document(io.BytesIO(data)).paragraphs)
    if ext in (".txt", ".md"):
        return data.decode("utf-8", errors="replace")
    raise HTTPException(415, f"Desteklenmeyen dosya türü: {ext}")


def index_document(policy_id: int, org_id: int, requirement_code: str, name: str, sha: str, chunks: list[str]) -> None:
    """Parçaları vektör veritabanına yazar. Aynı içerik zaten indeksliyse yeniden embed etmez."""
    old = store.get(where={"policy_id": policy_id}, include=["metadatas"])
    if old["ids"] and all(m.get("sha") == sha for m in old["metadatas"]) and len(old["ids"]) == len(chunks):
        return
    if old["ids"]:
        store.delete(ids=old["ids"])
    store.add_texts(
        chunks,
        metadatas=[{"policy_id": policy_id, "org_id": org_id, "requirement_code": requirement_code,
                    "file": name, "sha": sha, "chunk": i} for i in range(len(chunks))],
        ids=[f"{policy_id}-{i}" for i in range(len(chunks))],
    )


def keyword_sentence(item: dict, sents: list[tuple[str, str]]) -> tuple[str, str] | None:
    """Kural modu için: maddenin anahtar kelimelerinden birini (kökleriyle) içeren, olumlu ilk cümle."""
    phrases = item.get("keywords") or [w for w in item["text"].split() if len(w) > 4]
    stems = [set(tokens(p)) for p in phrases if tokens(p)]
    for file, s in sents:
        st = set(tokens(s))
        if any(k <= st for k in stems) and not R.is_negated(s) and not R.cites_standard(s):
            return s, file
    return None


def decide(item: dict, rule: dict | None, ai: ItemAssessment | None,
           sents: list[tuple[str, str]], texts: dict[str, str]) -> tuple[dict, float]:
    """Kural sonucu ile model değerlendirmesini birleştirir. (madde sonucu, güven) döner."""
    if rule and rule["outcome"] == "fail":  # kesin kural ihlali: modelin ne dediği önemsiz
        return {"status": "NotMet", "finding": rule["message"], "quote": rule["quote"], "file": rule["file"]}, 0.9

    notes: list[str] = []
    if ai is None:  # kural modu
        hit = keyword_sentence(item, sents)
        status = "Partial" if hit else "NotMet"
        finding = ("Kural modu: maddeyle ilgili ifade bulundu, anlam kontrolü yapılmadı." if hit
                   else "Kural modu: belgelerde maddeyle ilgili ifade bulunamadı.")
        quote, file = hit or ("", "")
        conf = 0.45
        if rule and rule["outcome"] == "pass":
            status, finding, quote, file, conf = "Met", rule["message"], rule["quote"], rule["file"], 0.8
    else:
        status, finding, quote, file = ai.status, ai.finding.strip(), "", ""
        verified = R.verify_quote(ai.quote, sents, texts) if ai.quote.strip() else None
        if verified:
            quote, file = verified
            conf = 0.75
            if status == "Met" and R.is_negated(quote):
                status = "Partial"
                notes.append("Kontrol: alıntılanan cümle olumsuz bir ifade içeriyor.")
            if status != "NotMet" and not R.is_relevant(quote, item["text"], item.get("keywords", [])):
                status = "Partial" if status == "Met" else "NotMet"
                notes.append("Kontrol: alıntı maddeyle ilgili görünmüyor.")
                conf = 0.55
        else:
            conf = 0.7 if status == "NotMet" else 0.5
            if status == "Met":  # dayanağı gösterilemeyen "karşılandı" kabul edilmez
                status = "Partial"
                notes.append("Kontrol: model belgeden doğrulanabilir bir alıntı gösteremedi." if ai.quote.strip()
                             else "Kontrol: kararı destekleyen alıntı yok.")
        if rule and rule["outcome"] == "pass":
            conf = max(conf, 0.8)
            if status == "NotMet":
                status = "Partial"
                notes.append(rule["message"])
            if not quote:
                quote, file = rule["quote"], rule["file"]

    if rule and rule["outcome"] == "missing":
        if status == "Met":
            status = "Partial"
        notes.append(rule["message"])
    # Niyet beyanı ("eğitim verilecektir") faaliyetin uygulandığını göstermez
    evidence = [q for q in (quote, rule["quote"] if rule else "") if q]
    if status == "Met" and evidence and all(R.is_intent(q) for q in evidence):
        status = "Partial"
        notes.append("Kontrol: dayanak cümle bir niyet beyanı (gelecek zaman); uygulandığına dair kayıt yok.")
    return {"status": status, "finding": " ".join([finding, *notes]).strip(), "quote": quote, "file": file}, conf


def verdict_of(statuses: list[str]) -> str:
    """Gereksinim kararı maddelerden türetilir: hepsi karşılandıysa uyumlu, hiçbiri karşılanmadıysa uyumsuz."""
    if all(s == "Met" for s in statuses):
        return "Compliant"
    if all(s == "NotMet" for s in statuses):
        return "NonCompliant"
    return "Partial"


def summary_of(checks: list[dict]) -> str:
    n = {s: sum(c["status"] == s for c in checks) for s in ("Met", "Partial", "NotMet")}
    text = (f"{len(checks)} kontrol maddesinden {n['Met']} karşılandı, {n['Partial']} kısmen karşılandı, "
            f"{n['NotMet']} karşılanmadı.")
    first = next((c for c in checks if c["status"] == "NotMet"), None) or next(
        (c for c in checks if c["status"] == "Partial"), None)
    return text + (f" Öncelikli eksik: {first['id']}) {first['text']}." if first else "")


app = FastAPI(title="PCI DSS Kanıt Analiz Servisi (yerel hibrit RAG)")


@app.get("/health")
def health():
    info = {"status": "ok", "mode": MODE, "procedures": len(PROCEDURES),
            "rules": sum("rule" in it for items in PROCEDURES.values() for it in items)}
    if MODE == "hybrid":
        info |= {"llm": CHAT_MODEL, "embedding": EMBED_MODEL}
        try:
            tags = httpx.get(f"{OLLAMA_URL}/api/tags", timeout=3).json()
            have = {m["name"] for m in tags.get("models", [])}
            missing = [m for m in (CHAT_MODEL, EMBED_MODEL) if m not in have and f"{m}:latest" not in have]
            info |= {"ollama": "ok" if not missing else f"eksik model: {', '.join(missing)}"}
        except Exception as e:
            info |= {"ollama": f"ulaşılamıyor ({type(e).__name__})", "status": "degraded"}
    return info


@app.post("/analyze")
async def analyze(
    files: list[UploadFile],
    policy_ids: list[int] = Form(...),
    target_policy_id: int = Form(...),
    org_id: int = Form(...),
    requirement_code: str = Form(...),
    requirement_title: str = Form(...),
    requirement_description: str = Form(...),
    question: str = Form(""),
    evidence_hint: str = Form(""),
):
    if len(files) != len(policy_ids) or target_policy_id not in policy_ids:
        raise HTTPException(422, "Belge listesi ile belge kimlikleri eşleşmiyor.")

    # Gereksinime ait tüm belgeleri oku ve parçala. Hedef belge okunamıyorsa hata; diğerleri atlanır.
    texts: dict[str, str] = {}          # dosya adı → tam metin (kural ve alıntı doğrulaması için)
    chunk_text, chunk_file = {}, {}     # parça id → metin / dosya
    used, skipped = [], []
    for pid, f in zip(policy_ids, files):
        name = f.filename or f"belge-{pid}"
        data = await f.read()
        text = extract_text(name, data).strip()
        if len(text) < 30:
            if pid == target_policy_id:
                raise HTTPException(422, "Belgeden okunabilir metin çıkarılamadı (taranmış/görsel PDF olabilir).")
            skipped.append(name)
            continue
        chunks = splitter.split_text(text)
        if store is not None:
            try:
                index_document(pid, org_id, requirement_code, name, hashlib.sha256(data).hexdigest(), chunks)
            except Exception as e:
                raise ollama_down(e)
        texts[name] = text
        for i, c in enumerate(chunks):
            chunk_text[f"{pid}-{i}"], chunk_file[f"{pid}-{i}"] = c, name
        used.append(pid)

    ids = list(chunk_text)
    bm25 = BM25([chunk_text[c] for c in ids])
    sents = [(file, s) for file, text in texts.items() for s in R.sentences(text)]
    flt = {"policy_id": {"$in": used}} if len(used) > 1 else {"policy_id": used[0]}

    # Bilgi tabanından kontrol listesi; listede olmayan gereksinimde açıklamanın kendisi tek madde olur
    items = PROCEDURES.get(requirement_code) or [{"id": "a", "text": requirement_description, "keywords": []}]

    checks, confidences, asked = [], [], 0
    for it in items:
        rule = R.evaluate(it.get("rule"), sents)
        if rule and rule["outcome"] == "fail":  # kesin kural ihlali: arama ve modele gerek yok
            result, conf = decide(it, rule, None, sents, texts)
            checks.append({"id": it["id"], "text": it["text"], **result})
            confidences.append(conf)
            continue
        vector = None
        if store is not None:
            try:
                vector = [f"{d.metadata['policy_id']}-{d.metadata['chunk']}" for d in store.similarity_search(
                    f"{requirement_title}. {it['text']}", k=min(VECTOR_K, len(ids)), filter=flt)]
            except Exception as e:
                raise ollama_down(e)
        found = hybrid_search(it["text"], it.get("keywords", []), ids, bm25, vector, MAX_PER_ITEM)
        ai = None
        if llm:
            context = "\n\n".join(f"[B{i + 1}] (belge: {chunk_file[c]})\n{chunk_text[c]}" for i, c in enumerate(found))
            try:
                ai = await ask_model(PROMPT.format(code=requirement_code, title=requirement_title, item=it["text"],
                                                   context=context or "(ilgili bölüm bulunamadı)"))
                asked += 1
            except (httpx.TransportError, httpx.HTTPStatusError) as e:
                raise ollama_down(e)
            except (ValueError, KeyError) as e:  # şemaya uymayan çıktı
                raise HTTPException(502, f"Yerel model geçerli bir değerlendirme döndürmedi ({type(e).__name__}).")
        # Kural modunda anahtar kelime cümlesi yalnızca aramanın getirdiği parçalarda aranır
        found_sents = [(chunk_file[c], s) for c in found for s in R.sentences(chunk_text[c])]
        result, conf = decide(it, rule, ai, sents if ai else found_sents, texts)
        checks.append({"id": it["id"], "text": it["text"], **result})
        confidences.append(conf)

    if MODE == "hybrid":
        model = f"hibrit: kurallar + {CHAT_MODEL} (yerel, {asked}/{len(items)} madde modelle)"
    else:
        model = "kural tabanlı: BM25 + kurallar (dil modeli yok)"

    return {
        "verdict": verdict_of([c["status"] for c in checks]),
        "confidence": round(sum(confidences) / len(confidences), 2),
        "summary": summary_of(checks),
        "gaps": [f"{c['id']}) {c['text']}: {c['finding']}" for c in checks if c["status"] != "Met"],
        "quotes": [c["quote"] for c in checks if c["quote"] and c["status"] != "NotMet"][:3],
        "checks": checks,
        "documents": sorted(texts),
        "skipped": skipped,
        "model": model,
    }
