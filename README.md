# PCI DSS Ön Değerlendirme Platformu

Kurumların PCI DSS uyumluluklarını resmi denetimden **önce** kendi başlarına değerlendirip eksiklerini görebildikleri, yapay zekâ destekli bir ön değerlendirme platformu.

> **Durum:** Kapsam belirleme, beyan anketi, kanıt yükleme, yerel hibrit RAG ile YZ belge analizi, gap analizi paneli ve denetçi paneli çalışıyor. Sırada bulguların kalıcılaştırılması (`Finding`) ve raporlama var.

## Proje hakkında

PCI DSS denetimleri uzun, maliyetli ve çoğu zaman sürprizlerle dolu bir süreçtir. Kurumlar hangi kontrollerinin eksik olduğunu genellikle denetçi geldiğinde öğrenir.

Bu platform süreci denetimden öne çeker:

1. **Kapsam belirleme.** Kurum kart ödemelerini nasıl kabul ettiğini beyan eder. Platform buna göre uygun **SAQ** (öz değerlendirme anketi) tipini belirler.
2. **Adım adım beyan.** Kurum yalnızca kendi SAQ tipine ait PCI DSS gereksinimlerini görür ve her biri için durumunu beyan eder.
3. **Dijital kanıt.** Beyanlar politika belgeleri, sistem raporları ve konfigürasyon çıktılarıyla desteklenir.
4. **YZ ile analiz.** Yüklenen belgeler yerel bir hibrit RAG servisiyle gereksinimin denetçi kontrol listesine göre madde madde karşılaştırılır. Beyan, sistemin süzgecinden geçmeden kabul edilmez.
5. **Gap analizi paneli.** Kritik bulgular süreci durdurmaz; sayılır ve panelde tek tek takip edilir.
6. **Uzaktan denetim.** Kurumun yetki verdiği denetçi, sistemin kontrol ettiği kanıtlı beyanları kendi panelinden inceler, onaylar veya gerekçeyle reddeder.

## Özellikler

### Mevcut

- **Kurum kaydı ve çok kiracılı yapı.** Her kurum yalnızca kendi verisini görür. Kurum kimliği JWT içinden okunur, istekten alınmaz.
- **SAQ kapsam belirleme.** Kurum tipi, kart verisi saklama durumu ve ödeme kanallarına göre SAQ tipi belirlenir: A, A-EP, B, B-IP, C, C-VT, P2PE, SPoC, D (Üye İşyeri), D (Servis Sağlayıcı).
- **PCI DSS v4.0.1 soru seti.** 12 ana gereksinimi ve Ek A2'yi kapsayan 102 alt gereksinim, her biri için Türkçe soru ve beklenen kanıt türü. SAQ A, B, B-IP, C-VT ve P2PE soru listeleri resmi SAQ belgeleriyle birebir eşleşir.
- **SAQ cevap seçenekleri:**
  - Uygulanıyor
  - Telafi edici kontrolle uygulanıyor (CCW)
  - Uygulanamaz
  - Test edilmedi
  - Uygulanmıyor

  CCW, Uygulanamaz ve Test edilmedi cevaplarında gerekçe zorunludur.
- **Kanıt yükleme.**
  - Sürükle-bırak ile PDF, DOCX, TXT ve MD dosyaları (en fazla 10 MB).
  - Dosyalar diskte rastgele adla saklanır.
  - Kanıt bütünlüğü için her dosyanın SHA-256 özeti kaydedilir.
- **Eksik kanıt takibi.** "Uygulanıyor" beyan edilip zorunlu kanıtı yüklenmemiş gereksinimler ayrıca sayılır.
- **YZ destekli kanıt analizi (yerel hibrit RAG).** Dış API kullanılmaz; tüm modeller bilgisayarda çalışır ve belgeler kurum dışına çıkmaz. Bir gereksinimle ilişkilendirilen her belge arka planda Python servisine gönderilir:
  1. Metin çıkarılır (PDF, DOCX, TXT, MD) ve LangChain ile parçalara bölünür.
  2. Parçalar yerel embedding modeliyle (`bge-m3`) ChromaDB vektör veritabanına yazılır.
  3. **Arama:** her denetçi kontrol maddesi için BM25 (anahtar kelime, Türkçe için 5 harfli kökleme) ve vektör (anlam) araması yapılır, sonuçlar RRF ile birleştirilir.
  4. **Kural katmanı:** sıklık, süre ve sayı şartları ("en az üç ayda bir", "12 ay saklanır", "en az 12 karakter") belgenin cümlelerinde deterministik olarak kontrol edilir. Kural sağlanmıyorsa madde doğrudan "Karşılanmadı" olur. Olumsuz cümleler ("MFA kullanılmamaktadır") ve standarttan alıntılar kanıt sayılmaz.
  5. **Yerel dil modeli** (Ollama, `gemma3:4b`) kalan her maddeyi yalnızca getirilen parçalara dayanarak ayrı ayrı değerlendirir ve belgeden alıntı verir.
  6. **Birleştirme:** modelin alıntısı belgede doğrulanır; belgede bulunamayan alıntıyla "Karşılandı" verilemez. Gereksinim kararı maddelerden kural tabanlı türetilir.

  Her analiz, aynı gereksinime yüklenmiş **tüm** belgeleri birlikte değerlendirir (bir belgede eksik kalan madde diğerinde karşılanabilir). Bu yüzden gereksinimin durumu en iyi değil, **en son** analizden alınır. Sonuç ekranında her kontrol maddesi durumu, gerekçesi ve belgeden alıntısıyla listelenir.

  `ANALYSIS_MODE=rules` ile dil modeli olmadan, yalnızca arama ve kurallarla da çalışır. Kural testleri: `cd ai-service && .venv\Scripts\python tests\test_rules.py`.

  **Ölçüm** (`ai-service/evaluate.py`, `eval/cases.json`: 5 belge, elle etiketlenmiş 14 kontrol maddesi; küçük bir set olduğu için yön göstergesidir):

  | Yapılandırma | Doğru | Süre |
  |---|---|---|
  | Yalnızca kurallar (`ANALYSIS_MODE=rules`) | 12/14 | < 1 sn |
  | Hibrit + `qwen3:4b` | 8/14 | ~70 sn |
  | Hibrit + `gemma3:4b` (varsayılan) | 12/14 | ~40 sn |

  Kurallar kesin şartlarda, dil modeli ise anlam gerektiren maddelerde ve Türkçe gerekçe yazmada güçlüdür. Hibrit yapı ikisini birleştirir: model kesin kuralları çiğneyemez, kural da modelin anladığı nüansı kaybetmez.
- **Gap analizi paneli.** Kaç gereksinimin eksik, kaçının tamam olduğu; 12 gereksinim için eksik sayısını gösteren ısı haritası ve Kritik/Orta/Düşük bulgular. Yüzdelik skor yoktur, eksikler tek tek listelenir. YZ'nin uyumsuz bulduğu kanıtlar kritik bulgu olarak işaretlenir. Bulgudan tek tıkla ilgili soruya veya belgeye gidilir. Denetçinin reddettiği gereksinimler de bulgu olarak görünür.
- **Denetçi paneli.**
  - Kayıt ekranında "Denetçi" seçilerek denetçi hesabı açılır. Denetçi, denetim firmasının kurumuna bağlıdır ve müşteri kurumların verisini yalnızca verilen yetkiyle görür.
  - Müşteri kurum **Denetçiler** sayfasından denetçiye e-posta adresiyle erişim verir ve istediği an geri alır.
  - Denetçi yetkili olduğu kurumları listeler; her gereksinim için kurumun beyanını, yüklenen belgeleri (indirerek) ve YZ'nin madde madde sonucunu görür, **Onayla** veya **Reddet** der. Reddetmek için not zorunludur.
  - Belgelerden biri hâlâ YZ analizindeyse karar verilemez ("Sistem kontrolü bekleniyor").
  - Karardan sonra yeni kanıt yüklenir veya yeniden analiz yapılırsa karar **eskimiş** sayılır ve gap analizinde hesaba katılmaz; denetçinin yeniden bakması gerekir.
  - Roller ayrıdır: denetçi müşteri ekranlarına, müşteri denetçi ekranlarına erişemez (403).

### Yol haritası

| Faz | Kapsam |
|---|---|
| **Faz 1** ✅ | Kayıt, kapsam belirleme, beyan anketi, kanıt yükleme |
| **Faz 2** ✅ | Python yerel hibrit RAG servisi (LangChain, ChromaDB, Ollama, kural katmanı): belgeleri gereksinimlerle karşılaştırma |
| **Faz 3** | Gap analizi paneli ✅; `Finding` varlığıyla bulguların kalıcılaştırılması, raporlama |
| **Faz 4** ✅ | Denetçi rolü, kurum bazlı erişim yetkisi, birden fazla kurumu uzaktan değerlendirme paneli |

## SAQ tipine göre soru sayısı

| Örnek senaryo | SAQ | Soru | Resmi SAQ'da |
|---|---|---|---|
| E-ticaret, ödeme sayfası tamamen ödeme kuruluşunda (iframe/yönlendirme) | A | 27 | 27 |
| E-ticaret, ödeme formu kurumun sitesinden geliyor (direct post / JS) | A-EP | 82 | 139 |
| Telefon hattına bağlı, bağımsız POS cihazı | B | 27 | 27 |
| IP bağlantılı, bağımsız PTS onaylı POS cihazı | B-IP | 49 | 49 |
| İnternete bağlı ödeme uygulaması / POS sistemi | C | 80 | 125 |
| Sağlayıcının sanal terminaline elle giriş | C-VT | 54 | 54 |
| Doğrulanmış P2PE çözümü | P2PE | 21 | 21 |
| Kart verisini elektronik ortamda saklayan üye işyeri | D (Üye İşyeri) | 100 | 252 |
| Servis sağlayıcı | D (Servis Sağlayıcı) | 102 | 259 |

> ⚠️ SAQ A, B, B-IP, C-VT ve P2PE soru listeleri resmi SAQ belgeleriyle (A için v4.0.1 r1, diğerleri v4.0) karşılaştırılarak tamamlanmıştır. A-EP, C ve D'deki eksikler ile SPoC eşlemesi henüz tamamlanmadı. SAQ belirleme kuralları **ön değerlendirme amaçlı sadeleştirilmiştir**. Platform gerçek kurumlarla kullanılmadan önce PCI SSC'nin güncel SAQ belgeleriyle doğrulanmalıdır. Kesin SAQ tipini kurumun edinci bankası (acquirer) onaylar. Soru metinleri resmi belgelerden kopyalanmamış, Türkçe özet olarak yazılmıştır.

## Teknolojiler

| Katman | Teknoloji |
|---|---|
| Backend | .NET 9, ASP.NET Core Web API, Entity Framework Core 9 (Code-First) |
| Veritabanı | SQL Server (geliştirmede LocalDB) |
| Kimlik doğrulama | JWT, BCrypt ile parola özeti |
| Frontend | React 18, Vite, React Router, Axios, react-dropzone |
| YZ servisi | Python, FastAPI, LangChain, ChromaDB, Ollama (yerel `gemma3:4b` + `bge-m3`), BM25 ve kural katmanı |

## Mimari

```
backend/
  PciDss.Entities     → Varlıklar ve enum'lar (Organization, User, ScopeProfile, Requirement, SurveyQuestion, SurveyAnswer, Policy, AuditorAccess, AuditorReview)
  PciDss.DataAccess   → AppDbContext (EF Core + seed verisi), Generic Repository, migration'lar
  PciDss.Business     → DTO'lar ve servisler (Auth, Scope/SAQ kuralları, Survey, File, Gap, Demo, Denetçi erişimi, Denetim, YZ analiz kuyruğu)
  PciDss.API          → Controller'lar, JWT / CORS / Swagger yapılandırması
frontend/
  src/api.js          → Backend'e giden tek axios istemcisi
  src/pages/          → Giriş/Kayıt, Kapsam Belirleme, Beyan Anketi, Kanıt Yükleme, Gap Analizi, Denetçiler, Denetçi paneli
ai-service/
  app.py              → FastAPI hibrit RAG servisi (/analyze, /health), kararların birleştirilmesi
  retrieval.py        → BM25 + vektör araması, RRF ile birleştirme
  rules.py            → Kural katmanı: sıklık/süre/sayı şartları, olumsuzluk, alıntı doğrulama
  knowledge/          → Denetçi kontrol listesi ve maddelerin kuralları (test_procedures.json)
  tests/              → Kural katmanı testleri
demo/                 → Sunumda canlı yüklenecek örnek kanıtlar (biri bilerek eksik yazılmıştır)
docs/                 → RAG mimarisi ve "Sistem nasıl çalışır" anlatım belgeleri
```

YZ akışı: `Kanıt yükle → Policy (Pending) → AnalysisWorker → ai-service /analyze → Policy (Completed + karar) → Gap analizi`

Denetim akışı: `Müşteri denetçiye erişim verir → Denetçi kanıtlı beyanı ve YZ sonucunu inceler → Onay / Ret (not) → Gap analizi`

- **Tek yönlü katman bağımlılığı:** `API → Business → DataAccess → Entities`.
- **Controller'lar** entity değil, DTO döner.
- **İş kuralı hataları** `BusinessException` ile fırlatılır; API bunları `400 { "error": "..." }` olarak döner.

## Kurulum

### En kolay yol (Windows)

Gereksinimleri kurduktan sonra depo klasöründeki **`baslat.bat`** dosyasına çift tıklayın. Paketleri kurar, LocalDB'yi başlatır, YZ servisi, backend ve frontend'i ayrı pencerelerde açar ve tarayıcıda http://localhost:5173 adresini açar. Veritabanı ilk açılışta otomatik oluşturulur.

Giriş ekranındaki **"Demo kurumla giriş"** butonu hazır verili örnek bir kurum açar (her girişte sıfırlanır). **"Demo denetçiyle giriş"** butonu, demo kurumu sıfırlamadan onun beyanlarını denetçi gözüyle açar. `demo/` klasöründe canlı yüklemek için iki örnek kanıt vardır: 1.2.7 için uyumlu bir tutanak ve 10.4.1 için YZ'nin yakalaması gereken eksik bir prosedür.

**Yerel dil modeli:** API anahtarı gerekmez. [Ollama](https://ollama.com/download)'yı kurun; `baslat.bat` ilk açılışta `gemma3:4b` (~3,4 GB) ve `bge-m3` (~1,2 GB) modellerini indirir. Elle: `ollama pull gemma3:4b` ve `ollama pull bge-m3`. Ollama yoksa YZ servisi dil modeli olmadan, yalnızca kural moduyla çalışır. 6 GB ekran kartında rahat çalışır; ayarlar `ai-service/.env.example` içindedir.

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org) (LTS)
- [Python](https://www.python.org) 3.11–3.13 (YZ servisi için)
- [Ollama](https://ollama.com/download) (yerel dil modeli için; yoksa YZ servisi kural moduyla çalışır)
- SQL Server LocalDB. Visual Studio ile gelir; ayrıca SQL Server Express kurulumunda "LocalDB" seçilerek de kurulabilir.
- EF Core aracı (yalnızca migration eklerken gerekir): `dotnet tool install --global dotnet-ef --version 9.0.9`

> **Mac / Linux:** LocalDB yalnızca Windows'ta çalışır. SQL Server'ı Docker ile çalıştırıp `backend/PciDss.API/appsettings.json` içindeki `ConnectionStrings:Default` değerini ona göre değiştirin.

### Backend

```bash
cd backend
dotnet run --project PciDss.API   # http://localhost:5000; veritabanını ve soru setini açılışta oluşturur/günceller
```

Swagger arayüzü: http://localhost:5000/swagger

### Frontend

```bash
cd frontend
npm install
npm run dev        # http://localhost:5173
```

`frontend/.env` dosyasındaki `VITE_API_URL` değeri backend adresini gösterir (varsayılan `http://localhost:5000/api`). CORS yalnızca `http://localhost:5173` adresine izin verir.

### Kullanım

1. http://localhost:5173 adresinden kurum ve kullanıcı kaydı oluşturun. Parola en az 12 karakter olmalı.
2. **Kapsam Belirleme** adımında ödeme kanallarınızı seçin ve SAQ tipinizi görün.
3. **Beyan Anketi**'nde size ait gereksinimleri cevaplayın.
4. **Kanıt Yükleme**'de belgelerinizi ilgili gereksinimle ilişkilendirerek yükleyin.
5. **Gap Analizi**'nde kaç eksik kaldığını, gereksinim bazlı ısı haritasını ve eksiklerin listesini görün.
6. İsterseniz **Denetçiler** sayfasından bir denetçiye e-posta adresiyle erişim verin.

Denetçi olarak: kayıtta "Denetçi"yi seçin, giriş yapınca yetkili olduğunuz kurumları görürsünüz. Kurumu açıp gereksinimleri tek tek onaylayın veya notla reddedin.

## API uç noktaları

| Metot | Yol | Açıklama |
|---|---|---|
| POST | /api/auth/register | Kurum + kullanıcı kaydı (parola ≥ 12 karakter; `isAuditor` ile denetçi hesabı) |
| POST | /api/auth/login | JWT döner |
| GET | /api/scope | Kurumun kapsam beyanı ve SAQ tipi (yoksa 204) |
| PUT | /api/scope | Kapsam beyanını kaydet, SAQ tipini belirle |
| GET | /api/survey/questions | SAQ tipine göre sorular + kurumun mevcut cevapları |
| POST | /api/survey/answers | Cevapları kaydet (upsert) |
| GET | /api/survey/progress | Cevap ve eksik kanıt istatistikleri |
| POST | /api/files | multipart dosya yükleme (`file`, `requirementCode`) |
| GET | /api/files | Kurumun yüklediği belgeler |
| POST | /api/files/{id}/analyze | Belgeyi yeniden YZ analizine al |
| GET | /api/gap | Tamam/eksik sayıları, bölüm bazında eksikler ve bulgular |
| POST | /api/auth/demo | Demo kurumu sıfırlar ve JWT döner (`Demo:Enabled`) |
| POST | /api/auth/demo-auditor | Demo kuruma erişimi olan demo denetçiyle giriş |
| GET | /api/auditors | Kuruma erişimi olan denetçiler |
| POST | /api/auditors | Denetçiye e-posta adresiyle erişim ver |
| DELETE | /api/auditors/{id} | Denetçi erişimini geri al |
| GET | /api/audit/orgs | *(denetçi)* Yetkili olunan kurumlar |
| GET | /api/audit/orgs/{orgId} | *(denetçi)* Kurumun beyanları, belgeleri, YZ sonuçları ve kararlar |
| PUT | /api/audit/orgs/{orgId}/reviews/{questionId} | *(denetçi)* Onay / ret kararı (retle not zorunlu) |
| GET | /api/audit/orgs/{orgId}/files/{id} | *(denetçi)* Kanıt belgesini indir |

Müşteri uç noktaları yalnızca Admin/OrgUser, `/api/audit/*` yalnızca Auditor rolüne açıktır; denetçi uç noktaları her çağrıda erişim yetkisini kontrol eder.

## Birlikte geliştirme notları

- **Veritabanı yapısı değişince migration ekleyin:**
  ```bash
  dotnet ef migrations add <Ad> -p PciDss.DataAccess -s PciDss.API
  ```
  Diğer geliştiricide migration backend açılırken otomatik uygulanır. İki kişi aynı anda migration eklerse çakışır; önceden haberleşin.
- **Seed verisindeki tarihler sabit kalmalı.** Gereksinim ve soru seed satırlarında `CreatedAt` sabit tutulur; aksi halde EF her derlemede gereksiz migration üretir.
- **Veritabanı ve yüklenen dosyalar yereldir.** Kullanıcılar, cevaplar ve `uploads/` klasörü geliştiriciler arasında paylaşılmaz.
- Kod içi açıklamalar, hata mesajları ve arayüz metinleri **Türkçe** tutulur.

## Üretim öncesi yapılacaklar

- `Jwt:Key` değeri `appsettings.json`'dan çıkarılıp user-secrets veya ortam değişkenine taşınmalı.
- Token şu an `localStorage`'da; üretimde HttpOnly cookie tercih edilmeli.
- Yüklenen dosyalar için içerik (magic byte) doğrulaması ve virüs taraması eklenmeli.
- SAQ eşlemesi resmi PCI SSC belgeleriyle doğrulanmalı.
