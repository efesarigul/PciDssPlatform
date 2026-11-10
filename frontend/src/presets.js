// Gerekçe zorunlu cevaplar (CCW, Uygulanamaz, Test edilmedi) için hazır metinler.
// Tıklanınca gerekçe alanına yazılır; kullanıcı düzenleyebilir. Gerekçe yine zorunlu kalır ve kayıtta saklanır
// (resmi SAQ'daki Ek B/C/D karşılığı). ":" ile biten metinler kullanıcının tamamlaması içindir.

const GENERAL = {
  NotApplicable: [
    "İlgili teknoloji veya süreç kart verisi ortamında (CDE) bulunmuyor.",
    "Bu işlem tamamen PCI DSS uyumlu bir hizmet sağlayıcıya devredilmiş; sağlayıcının uyum belgesi (AOC) mevcut.",
    "Kurum kart verisini elektronik ortamda saklamıyor, işlemiyor veya iletmiyor.",
  ],
  NotTested: [
    "Kontrol yakın zamanda devreye alındı; testi bir sonraki değerlendirme dönemine planlandı.",
    "Bu gereksinim, edinci bankayla mutabık kalınan değerlendirme kapsamı dışında bırakıldı.",
    "Test için gerekli ortam veya erişim henüz hazır değil; planlanan test tarihi:",
  ],
  InPlaceWithCcw: [
    "Eski (legacy) sistem gereksinimi desteklemiyor; risk ek erişim kısıtlaması, MFA ve artırılmış izleme ile azaltılıyor.",
    "Meşru teknik kısıt nedeniyle gereksinim doğrudan uygulanamıyor. Uygulanan telafi edici kontrol:",
    "İş kısıtı nedeniyle gereksinim doğrudan uygulanamıyor; aynı riski karşılayan alternatif kontrol uygulanıyor ve yılda bir gözden geçiriliyor.",
  ],
};

// Gereksinime özel gerekçeler: SAQ'larda en sık "Uygulanamaz" denen gereksinimler
const NOT_STORED = "Kurum kart numarasını (PAN) elektronik ortamda saklamıyor; kart verisi doğrudan ödeme kuruluşuna iletiliyor.";
const NO_POI = "Kurum kart okuyucu (POI) cihazı kullanmıyor; yalnızca kart-mevcut-olmayan (e-ticaret/MOTO) ödeme alınıyor.";
const NO_TPSP = "Kart verisini paylaşan veya güvenliğini etkileyebilecek üçüncü taraf hizmet sağlayıcı bulunmuyor.";
const HOSTED_PAGE = "Ödeme sayfası tamamen ödeme kuruluşunda barındırılıyor; kurumun sitesinde ödeme sayfası betiği çalışmıyor.";

const BY_REQUIREMENT = {
  "2.3.1": ["Kart verisi ortamında kablosuz ağ bulunmuyor; CDE'ye bağlı kablosuz erişim noktası yok."],
  "3.2.1": [NOT_STORED],
  "3.4.1": ["PAN hiçbir ekranda, raporda veya çıktıda gösterilmiyor."],
  "3.5.1": [NOT_STORED],
  "3.6.1": [NOT_STORED + " Bu nedenle PAN şifrelemesinde kullanılan anahtar yok."],
  "4.2.2": ["PAN, e-posta veya anlık mesajlaşma gibi son kullanıcı mesajlaşma teknolojileriyle gönderilmiyor ve alınmıyor."],
  "6.2.1": ["Kurum kendi yazılımını geliştirmiyor; yalnızca hazır (ticari) yazılım kullanılıyor."],
  "6.4.3": [HOSTED_PAGE],
  "11.6.1": [HOSTED_PAGE],
  "8.2.2": ["Ortak, grup veya paylaşılan hesap kullanılmıyor; her kullanıcının benzersiz hesabı var."],
  "8.4.3": ["Kart verisi ortamına kurum ağı dışından uzaktan erişim yapılmıyor."],
  "9.4.1": ["Kart verisi içeren fiziksel medya (kağıt, harici disk vb.) bulunmuyor."],
  "9.4.6": ["Kart verisi kağıda yazdırılmıyor veya basılı olarak tutulmuyor."],
  "9.5.1": [NO_POI],
  "9.5.1.3": [NO_POI],
  "12.3.1": ["Kurum, sıklığını kendisinin belirleyebildiği (esnek sıklıklı) hiçbir gereksinimi uygulamıyor; tüm sıklıklar standartta tanımlı olanlardır."],
  "12.8.1": [NO_TPSP],
  "12.8.2": [NO_TPSP],
  "12.8.4": [NO_TPSP],
};

/** Gereksinime özel gerekçeler (yalnızca Uygulanamaz için) önce, genel gerekçeler sonra. */
export function presetsFor(requirementCode, answer) {
  const specific = answer === "NotApplicable" ? (BY_REQUIREMENT[requirementCode] ?? []) : [];
  return [
    ...specific.map((text) => ({ text, specific: true })),
    ...(GENERAL[answer] ?? []).map((text) => ({ text, specific: false })),
  ];
}
