"""
Arama katmanı (RAG'in "Retrieval" kısmı). İki yöntem birleştirilir:

  1. BM25 (anahtar kelime): klasik bilgi erişimi formülü. Türkçe eklemeli bir dil olduğu için kelimeler ilk 5
     harfine indirilir ("incelenir", "inceleme", "incelenmesi" → "incel"). Bu "F5 kökleme" Türkçe bilgi erişiminde
     bilinen, basit ve etkili bir yöntemdir. "MFA", "6 ay" gibi kesin ifadeleri kaçırmaz.
  2. Vektör araması (anlam): yerel embedding modeliyle ChromaDB'de benzerlik araması. "Kayıtların incelenmesi"
     ile "log review" gibi farklı kelimelerle aynı şeyi anlatan bölümleri bulur.

İki sıralama Reciprocal Rank Fusion (RRF) ile tek listeye indirilir: puan = Σ 1 / (60 + sıra).
"""
import math
import re
from collections import Counter

from rules import fold

STOPWORDS = {"ve", "ile", "bir", "bu", "şu", "için", "da", "de", "en", "olarak", "her", "veya", "gibi", "olan",
             "ya", "ki", "mi", "çok", "daha", "tüm", "ise", "kadar", "göre", "the", "and", "of", "or", "to"}


def tokens(text: str) -> list[str]:
    return [w[:5] for w in re.findall(r"\w+", fold(text)) if len(w) > 1 and w not in STOPWORDS]


class BM25:
    def __init__(self, docs: list[str], k1: float = 1.5, b: float = 0.75):
        self.k1, self.b = k1, b
        self.docs = [Counter(tokens(d)) for d in docs]
        self.lens = [sum(c.values()) for c in self.docs]
        self.avg = sum(self.lens) / max(len(self.docs), 1)
        df = Counter(t for c in self.docs for t in c)
        n = len(self.docs)
        self.idf = {t: math.log(1 + (n - f + 0.5) / (f + 0.5)) for t, f in df.items()}

    def scores(self, query: str) -> list[float]:
        q = tokens(query)
        out = []
        for c, length in zip(self.docs, self.lens):
            s = 0.0
            for t in q:
                if t in c:
                    tf = c[t]
                    s += self.idf[t] * tf * (self.k1 + 1) / (tf + self.k1 * (1 - self.b + self.b * length / self.avg))
            out.append(s)
        return out


def rrf(rankings: list[list[str]], k: int = 60) -> list[str]:
    """Reciprocal Rank Fusion: birden fazla sıralamayı tek sıralamaya indirger."""
    score: dict[str, float] = {}
    for ranking in rankings:
        for rank, cid in enumerate(ranking):
            score[cid] = score.get(cid, 0) + 1 / (k + rank + 1)
    return sorted(score, key=score.get, reverse=True)


def hybrid_search(query: str, keywords: list[str], ids: list[str], bm25: BM25,
                  vector_ids: list[str] | None, top: int) -> list[str]:
    """
    query: kontrol maddesi metni; keywords: maddenin anahtar kelimeleri (sorguda iki kat ağırlık alır).
    vector_ids: vektör aramasının sıraladığı parça id'leri (yalnızca kural modunda None).
    """
    s = bm25.scores(f"{query} {' '.join(keywords) * 2}")
    keyword_rank = [ids[i] for i in sorted(range(len(ids)), key=lambda i: -s[i]) if s[i] > 0]
    rankings = [keyword_rank] + ([vector_ids] if vector_ids else [])
    return rrf(rankings)[:top]
