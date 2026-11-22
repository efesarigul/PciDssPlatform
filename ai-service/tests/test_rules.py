"""Kural katmanı testleri. Çalıştırma: ai-service klasöründe  python -m pytest tests  veya  python tests/test_rules.py"""
import json
import sys
from pathlib import Path

BASE = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(BASE))
import rules as R  # noqa: E402

sys.stdout.reconfigure(encoding="utf-8")

PROC = json.loads((BASE / "knowledge" / "test_procedures.json").read_text(encoding="utf-8"))["procedures"]

# (gereksinim, madde sırası, cümle, beklenen sonuç)
CASES = [
    ("10.5.1", 0, "Kayıtlar 12 ay saklanır", "pass"),
    ("10.5.1", 0, "Denetim kayıtları 6 ay tutulur", "fail"),
    ("10.5.1", 0, "Kayıtlar en az bir yıl süreyle saklanır", "pass"),
    ("1.2.7", 0, "Kural setleri 6 ayda bir gözden geçirilir", "pass"),
    ("1.2.7", 0, "Kural setleri yılda bir gözden geçirilir", "fail"),
    ("11.3.1", 0, "İç zafiyet taraması üç ayda bir yapılır", "pass"),
    ("11.3.1", 0, "İç zafiyet taraması 90 günde bir yapılır", "pass"),
    ("11.3.1", 0, "Zafiyet taraması yılda bir yapılır", "fail"),
    ("6.3.3", 0, "Kritik yamalar bir ay içinde uygulanır", "pass"),
    ("6.3.3", 0, "Kritik yamalar 60 gün içinde uygulanır", "fail"),
    ("8.2.6", 0, "90 gün kullanılmayan hesaplar devre dışı bırakılır", "pass"),
    ("8.3.6", 0, "Parolalar en az 8 karakter olmalıdır", "fail"),
    ("8.3.6", 0, "Parola uzunluğu en az 12 karakterdir", "pass"),
    ("12.6.1", 1, "Farkındalık eğitimi yıllık olarak verilir", "pass"),
    ("10.4.1", 0, "Loglar SIEM'de sürekli izlenir ve her gün incelenir", "pass"),
    ("10.4.1", 0, "Kayıtlar ayda bir kez gözden geçirilir", "fail"),
    ("10.4.1", 0, "Loglar incelenmez", "missing"),                                   # olumsuz cümle sayılmaz
    ("10.4.1", 0, "PCI DSS logların günde bir incelenmesini ister", "missing"),     # standarttan alıntı kanıt değil
    ("10.4.1", 0, "Son 3 aylık kayıtlar erişilebilir durumdadır", "missing"),       # bağlam kelimesi yok
    ("6.4.1", 0, "Uygulamalar WAF ile korunur", "skip"),                            # alternatif yol, karar modelde
]


def test_rules():
    wrong = []
    for code, i, sentence, want in CASES:
        got = R.evaluate(PROC[code][i]["rule"], [("belge", sentence)])["outcome"]
        if got != want:
            wrong.append(f"{code} '{sentence}': beklenen {want}, çıkan {got}")
    assert not wrong, "\n".join(wrong)


def test_negation_and_quotes():
    assert R.is_negated("MFA kullanılmamaktadır")
    assert not R.is_negated("Loglar her gün incelenir")
    text = "Kayıtlar, Sistem Yöneticisi tarafından ayda bir kez gözden geçirilir."
    sents = [("belge", s) for s in R.sentences(text)]
    assert R.verify_quote("kayıtlar sistem yöneticisi tarafından ayda bir kez gözden geçirilir", sents, {"belge": text})
    assert R.verify_quote("Loglar her gün SIEM ile incelenir", sents, {"belge": text}) is None  # uydurma alıntı
    # Model iki cümleyi birleştirerek alıntılayabilir
    two = "1. Parolalar en az 8 karakter olmalıdır. 2. Parolalar harf ve rakam içermelidir."
    assert R.verify_quote("1. Parolalar en az 8 karakter olmalıdır. 2. Parolalar harf ve rakam içermelidir",
                          [("belge", x) for x in R.sentences(two)], {"belge": two})
    # ...ya da liste satırlarını virgülle birleştirebilir
    lst = "1. Gözden geçirilen kural sayısı: 142\n2. İş gerekçesi bulunmayan ve kaldırılan kural sayısı: 6"
    assert R.verify_quote("1. Gözden geçirilen kural sayısı: 142, 2. İş gerekçesi bulunmayan ve kaldırılan kural sayısı: 6",
                          [("belge", x) for x in R.sentences(lst)], {"belge": lst})


def test_intent_and_relevance():
    assert R.is_intent("Tüm personele eğitim verilecektir")
    assert R.is_intent("Eğitimler yılda bir düzenlenecektir")
    for s in ["Kayıtlar 12 ay saklanır", "Ocak ayında yapılır", "Ancak loglar incelenir",
              "Loglar günlük incelenmektedir", "Parolalar harf içermelidir"]:
        assert not R.is_intent(s), s
    item, kw = "İnceleme otomatik araçla (SIEM vb.) destekleniyor", ["SIEM", "otomatik", "korelasyon", "alarm"]
    assert not R.is_relevant("Kayıtlar ayda bir kez gözden geçirilir", item, kw)
    assert R.is_relevant("Loglar Splunk SIEM aracıyla otomatik incelenir", item, kw)


if __name__ == "__main__":
    test_rules()
    test_negation_and_quotes()
    test_intent_and_relevance()
    print(f"Tamam: {len(CASES)} kural senaryosu + olumsuzluk/alıntı/niyet testleri geçti.")
