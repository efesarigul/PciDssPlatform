"""
Kural katmanı (deterministik). Dil modeline bırakılmayacak kadar kesin olan kontrolleri yapar:

  - frequency     : faaliyetin sıklığı ("ayda bir", "üç ayda bir", "yıllık") istenen aralıktan seyrek mi?
  - min_duration  : en az süre ("kayıtlar 12 ay saklanır")
  - max_duration  : en fazla süre ("yamalar bir ay içinde", "90 gün kullanılmayan hesap")
  - min_count     : en az sayı ("parolalar en az 12 karakter")

Kurallar knowledge/test_procedures.json içinde maddenin "rule" alanında tanımlıdır. Yalnızca maddenin bağlam
kelimelerinden birini (context) içeren cümleler değerlendirilir; böylece "3 aylık kayıtlar erişilebilir" gibi
ilgisiz bir cümle "inceleme sıklığı" sanılmaz. Ayrıca olumsuz cümle tespiti ve alıntı doğrulaması da buradadır.
"""
import re
from difflib import SequenceMatcher


def fold(s: str) -> str:
    """Türkçe büyük/küçük harf duyarsız karşılaştırma için normalleştirme."""
    return s.replace("İ", "i").replace("I", "ı").lower()


def norm(s: str) -> str:
    """Alıntı karşılaştırması için: küçük harf, tek boşluk, tırnak/işaret farkları yok sayılır."""
    return re.sub(r"\s+", " ", re.sub(r"[\"'“”‘’«»]", "", fold(s))).strip()


# ---------- Cümleler ----------

def sentences(text: str) -> list[str]:
    """Metni cümlelere böler. "1.2.7" gibi madde numaralarındaki noktalar cümle sonu sayılmaz."""
    parts = re.split(r"(?<!\d)[.!?;](?!\d)|\n+", text)
    return [p.strip() for p in parts if len(p.strip()) > 3]


NEGATION = re.compile(
    r"\w+m[ae]makta|\w+m[ae]mekte|\w+m[ıiuü]yor|\w+m[ae]m[ıi]şt[ıi]r|\w{3,}m[ae]z(?:lar|ler)?\b|\byok(?:tur)?\b|\bdeğil")


def is_negated(sentence: str) -> bool:
    """Cümle olumsuz mu? ("MFA kullanılmamaktadır", "SIEM yoktur", "loglar incelenmez")"""
    return bool(NEGATION.search(fold(sentence)))


# Standardın ne istediğini anlatan cümle ("PCI DSS günde bir incelenmesini ister") kurumun uygulamasının kanıtı değildir
CITES_STANDARD = re.compile(r"\b(?:ister|istemektedir|gerektirir|gerektirmektedir|öngörür|öngörmektedir|şart koşar)\b")


def cites_standard(sentence: str) -> bool:
    return bool(CITES_STANDARD.search(fold(sentence)))


# Gelecek zaman eki (-acak/-ecek): "eğitim verilecektir", "yılda bir düzenlenecektir". Bu bir plan/niyet beyanıdır;
# faaliyetin uygulandığını göstermez. ("ocak", "ancak" gibi kelimeler eşleşmez: ekten önce ünlü gerekir.)
FUTURE = re.compile(r"\w+?(?:y?[ae]c[ae]k|y?[ae]c[ae]ğ)(?:t[ıi]r|lar|ler|s[ıi]n[ıi]z|[ıi]z)?\b")


def is_intent(sentence: str) -> bool:
    return bool(FUTURE.search(fold(sentence)))


def is_relevant(quote: str, item_text: str, keywords: list[str]) -> bool:
    """Alıntı maddeyle en az bir kelime kökü paylaşıyor mu? (SIEM maddesine "ayda bir incelenir" alıntısı ilgisizdir)"""
    from retrieval import tokens
    item = set(tokens(item_text)) | {t for k in keywords for t in tokens(k)}
    return bool(item & set(tokens(quote)))


def has_any(sentence: str, stems: list[str]) -> bool:
    s = fold(sentence)
    return any(fold(k) in s for k in stems)


# ---------- Sayı, süre ve sıklık ifadeleri ----------

NUMBERS = {"bir": 1, "iki": 2, "üç": 3, "dört": 4, "beş": 5, "altı": 6, "yedi": 7, "sekiz": 8, "dokuz": 9,
           "on": 10, "on bir": 11, "on iki": 12, "on beş": 15, "yirmi": 20, "otuz": 30, "kırk beş": 45,
           "altmış": 60, "doksan": 90, "yüz": 100, "yüz seksen": 180}
NUM = r"(\d+|" + "|".join(sorted(NUMBERS, key=len, reverse=True)) + r")"
# Ay 365/12 gün sayılır ("12 ay" = 1 yıl). Karşılaştırmalarda %3 tolerans: "6 ayda bir" (182,5 gün) = 180 gün şartı
UNIT_DAYS = {"saat": 1 / 24, "gün": 1, "hafta": 7, "ay": 365 / 12, "yıl": 365}
TOL = 0.03
UNIT = r"(saat|gün|hafta|ay|yıl)"


def number(s: str) -> int:
    return int(s) if s.isdigit() else NUMBERS[s]


# "ayda bir", "üç ayda bir", "6 ayda bir kez", "yılda en az bir", "her gün", "her 90 günde"
FREQ_PATTERNS = [
    (re.compile(rf"(?:\b{NUM}\s+)?\b{UNIT}(?:da|de|ta|te)\s+(?:en az\s+)?(?:bir|1)\b"),
     lambda m: (number(m.group(1)) if m.group(1) else 1) * UNIT_DAYS[m.group(2)]),
    (re.compile(rf"\b{UNIT}(?:da|de|ta|te)\s+{NUM}\s+(?:kez|defa|kere)"),
     lambda m: UNIT_DAYS[m.group(1)] / number(m.group(2))),
    (re.compile(rf"\bher\s+(?:{NUM}\s+)?{UNIT}"),
     lambda m: (number(m.group(1)) if m.group(1) else 1) * UNIT_DAYS[m.group(2)]),
    (re.compile(rf"(?:\b{NUM}\s+)?\b{UNIT}l[ıiuü]k\b"),
     lambda m: (number(m.group(1)) if m.group(1) else 1) * UNIT_DAYS[m.group(2)]),
    (re.compile(r"\b(?:sürekli|gerçek zamanlı|anlık|real-time|continuous)"), lambda m: 0),
    (re.compile(r"\bdaily\b"), lambda m: 1), (re.compile(r"\bweekly\b"), lambda m: 7),
    (re.compile(r"\bmonthly\b"), lambda m: 30), (re.compile(r"\bquarterly\b|\bçeyrek"), lambda m: 90),
    (re.compile(r"\b(?:annually|yearly)\b"), lambda m: 365),
]

# "12 ay", "bir yıl süreyle", "30 gün içinde" (ardından "da bir" gelmeyen süreler)
DURATION = re.compile(rf"\b{NUM}\s+{UNIT}(?!(?:da|de|ta|te)\s+(?:en az\s+)?(?:bir|1)\b)(?:lık|lik|luk|lük)?\b")
COUNT = re.compile(rf"\b{NUM}\s+(\w+)")


def frequencies(sentence: str) -> list[tuple[float, str]]:
    s, found = fold(sentence), []
    for rx, days in FREQ_PATTERNS:
        for m in rx.finditer(s):
            found.append((days(m), m.group(0)))
    return found


def durations(sentence: str) -> list[tuple[float, str]]:
    return [(number(m.group(1)) * UNIT_DAYS[m.group(2)], m.group(0)) for m in DURATION.finditer(fold(sentence))]


def describe_interval(days: float) -> str:
    names = {0: "sürekli", 1: "günde bir", 7: "haftada bir", 30: "ayda bir", 90: "üç ayda bir",
             180: "altı ayda bir", 365: "yılda bir"}
    return next((n for d, n in names.items() if abs(days - d) <= d * TOL), f"{round(days)} günde bir")


def describe_duration(days: float) -> str:
    if days >= 365 and days % 365 == 0:
        return f"{int(days // 365)} yıl"
    if days >= 30 and days % 30 == 0:
        return f"{int(days // 30)} ay"
    return f"{int(days)} gün"


# ---------- Kural değerlendirme ----------

def evaluate(rule: dict | None, doc_sentences: list[tuple[str, str]]) -> dict | None:
    """
    Bir maddenin kuralını belgelerin tüm cümleleri üzerinde değerlendirir.
    doc_sentences: (dosya adı, cümle) çiftleri.
    Dönüş: None (kural yok) veya {"outcome": pass|fail|missing|skip, "message", "quote", "file"}
      pass    : istenen değeri karşılayan bir ifade bulundu
      fail    : ilgili ifadeler var ama hiçbiri istenen değeri karşılamıyor (kesin karar)
      missing : belgede bu faaliyete dair sayısal bir ifade yok (madde tam karşılanmış sayılamaz)
      skip    : madde alternatif bir yolla karşılanıyor olabilir (ör. WAF), karar dil modeline kalır
    """
    if not rule:
        return None
    if any(has_any(s, rule.get("alternatives", [])) for _, s in doc_sentences):
        return {"outcome": "skip", "message": "", "quote": "", "file": ""}

    kind = rule["type"]
    hits = []  # (uygun mu, değer, ifade, dosya, cümle)
    for file, s in doc_sentences:
        if not has_any(s, rule["context"]) or is_negated(s) or cites_standard(s):
            continue
        if kind == "frequency":
            values = frequencies(s)
            hits += [(d <= rule["max_days"] * (1 + TOL), d, e, file, s) for d, e in values]
        elif kind == "min_duration":
            hits += [(d >= rule["days"] * (1 - TOL), d, e, file, s) for d, e in durations(s)]
        elif kind == "max_duration":
            hits += [(d <= rule["days"] * (1 + TOL), d, e, file, s) for d, e in durations(s)]
        elif kind == "min_count":
            for m in COUNT.finditer(fold(s)):
                if m.group(2).startswith(fold(rule["unit"])[:5]):
                    n = number(m.group(1))
                    hits.append((n >= rule["value"], n, m.group(0), file, s))

    want = {
        "frequency": lambda: f"en az {describe_interval(rule['max_days'])}",
        "min_duration": lambda: f"en az {describe_duration(rule['days'])}",
        "max_duration": lambda: f"en fazla {describe_duration(rule['days'])}",
        "min_count": lambda: f"en az {rule['value']} {rule['unit']}",
    }[kind]()

    if not hits:
        what = "sıklık" if kind == "frequency" else "süre" if "duration" in kind else "sayısal değer"
        return {"outcome": "missing", "message": f"Kural: belgede bu madde için bir {what} belirtilmemiş ({want} isteniyor).",
                "quote": "", "file": ""}
    ok = [h for h in hits if h[0]]
    best = ok[0] if ok else hits[0]
    if ok:
        return {"outcome": "pass", "message": f"Kural: belgedeki '{best[2]}' ifadesi şartı karşılıyor ({want}).",
                "quote": best[4], "file": best[3]}
    return {"outcome": "fail", "message": f"Kural: belgede '{best[2]}' yazıyor; madde {want} istiyor.",
            "quote": best[4], "file": best[3]}


# ---------- Alıntı doğrulama ----------

def verify_quote(quote: str, doc_sentences: list[tuple[str, str]], full_texts: dict[str, str]) -> tuple[str, str] | None:
    """
    Dil modelinin verdiği alıntı belgede gerçekten geçiyor mu? Geçiyorsa (alıntı, dosya) döner; birebir değilse
    en çok benzeyen cümle (%85+) kullanılır. Bulunamazsa None: model alıntıyı uydurmuş olabilir.
    """
    if len(norm(quote)) < 8:
        return None
    normed = {f: norm(t) for f, t in full_texts.items()}

    def find(part: str) -> tuple[str, str] | None:
        q = norm(part)
        for file, text in normed.items():
            if q in text:
                return part.strip(), file
        best = max(((SequenceMatcher(None, q, norm(s)).ratio(), s, f) for f, s in doc_sentences), default=(0, "", ""))
        return (best[1], best[2]) if best[0] >= 0.85 else None

    whole = find(quote)
    if whole:
        return whole
    # Model birden fazla cümleyi ya da liste satırını ("1. ... 142, 2. ... 6") birleştirmiş olabilir:
    # her parça ayrı ayrı belgede bulunmalı
    pieces = re.split(r"[,;]?\s+(?=\d+\.\s)", quote)
    parts = [p for piece in pieces for p in sentences(piece) if len(norm(p)) >= 8]
    found = [find(p) for p in parts]
    if len(parts) > 1 and all(found):
        return quote.strip(), found[0][1]
    return None
