# PCI DSS Ön Değerlendirme Platformu

Kurumların PCI DSS uyumluluklarını resmi denetimden **önce** kendi başlarına değerlendirip eksiklerini görebildikleri bir ön değerlendirme platformu.

> **Durum:** Kapsam belirleme, beyan anketi, kanıt yükleme, gap analizi paneli ve denetçi paneli çalışıyor. Sırada bulguların kalıcılaştırılması (`Finding`) ve raporlama var. Yapay zekâ ile belge analizi sonraki aşamaya bırakıldı; o çalışma [`yz-analizi`](https://github.com/efesarigul/PciDssPlatform/tree/yz-analizi) dalında duruyor.

## Proje hakkında

PCI DSS denetimleri uzun, maliyetli ve çoğu zaman sürprizlerle dolu bir süreçtir. Kurumlar hangi kontrollerinin eksik olduğunu genellikle denetçi geldiğinde öğrenir.

Bu platform süreci denetimden öne çeker:

1. **Kapsam belirleme.** Kurum kart ödemelerini nasıl kabul ettiğini beyan eder. Platform buna göre uygun **SAQ** (öz değerlendirme anketi) tipini belirler.
2. **Adım adım beyan.** Kurum yalnızca kendi SAQ tipine ait PCI DSS gereksinimlerini görür ve her biri için durumunu beyan eder.
3. **Dijital kanıt.** Beyanlar politika belgeleri, sistem raporları ve konfigürasyon çıktılarıyla desteklenir.
4. **Kanıtlı beyan.** "Uygulanıyor" beyanı, zorunlu kanıtı yüklenmeden tamam sayılmaz.
5. **Gap analizi paneli.** Kritik bulgular süreci durdurmaz; sayılır ve panelde tek tek takip edilir.
6. **Uzaktan denetim.** Kurumun yetki verdiği denetçi, kanıtlı beyanları kendi panelinden inceler, onaylar veya gerekçeyle reddeder.

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
- **Gap analizi paneli.** Kaç gereksinimin eksik, kaçının tamam olduğu; 12 gereksinim için eksik sayısını gösteren ısı haritası ve Kritik/Orta/Düşük bulgular. Yüzdelik skor yoktur, eksikler tek tek listelenir. Bulgudan tek tıkla ilgili soruya veya belgeye gidilir. Denetçinin reddettiği gereksinimler de bulgu olarak görünür.
- **Denetçi paneli.**
  - Kayıt ekranında "Denetçi" seçilerek denetçi hesabı açılır. Denetçi, denetim firmasının kurumuna bağlıdır ve müşteri kurumların verisini yalnızca verilen yetkiyle görür.
  - Müşteri kurum **Denetçiler** sayfasından denetçiye e-posta adresiyle erişim verir ve istediği an geri alır.
  - Denetçi yetkili olduğu kurumları listeler; her gereksinim için kurumun beyanını ve yüklenen belgeleri (indirerek) görür, **Onayla** veya **Reddet** der. Reddetmek için not zorunludur.
  - Karardan sonra yeni kanıt yüklenirse karar **eskimiş** sayılır ve gap analizinde hesaba katılmaz; denetçinin yeniden bakması gerekir.
  - Roller ayrıdır: denetçi müşteri ekranlarına, müşteri denetçi ekranlarına erişemez (403).

### Yol haritası

| Faz | Kapsam |
|---|---|
| **Faz 1** ✅ | Kayıt, kapsam belirleme, beyan anketi, kanıt yükleme |
| **Faz 2** ✅ | Gap analizi paneli, demo kurumu, arayüz yenilemesi |
| **Faz 3** ✅ | Denetçi rolü, kurum bazlı erişim yetkisi, birden fazla kurumu uzaktan değerlendirme paneli |
| **Faz 4** | `Finding` varlığıyla bulguların kalıcılaştırılması, raporlama |
| **Sonraki aşama** | Yapay zekâ ile kanıt belgelerinin gereksinimlerle karşılaştırılması |

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

## Mimari

```
backend/
  PciDss.Entities     → Varlıklar ve enum'lar (Organization, User, ScopeProfile, Requirement, SurveyQuestion, SurveyAnswer, Policy, AuditorAccess, AuditorReview)
  PciDss.DataAccess   → AppDbContext (EF Core + seed verisi), Generic Repository, migration'lar
  PciDss.Business     → DTO'lar ve servisler (Auth, Scope/SAQ kuralları, Survey, File, Gap, Demo, Denetçi erişimi, Denetim)
  PciDss.API          → Controller'lar, JWT / CORS / Swagger yapılandırması
frontend/
  src/api.js          → Backend'e giden tek axios istemcisi
  src/pages/          → Giriş/Kayıt, Kapsam Belirleme, Beyan Anketi, Kanıt Yükleme, Gap Analizi, Denetçiler, Denetçi paneli
demo/                 → Sunumda canlı yüklenecek örnek kanıtlar (biri bilerek eksik yazılmıştır)
```

Denetim akışı: `Müşteri denetçiye erişim verir → Denetçi kanıtlı beyanı ve belgeleri inceler → Onay / Ret (not) → Gap analizi`

- **Tek yönlü katman bağımlılığı:** `API → Business → DataAccess → Entities`.
- **Controller'lar** entity değil, DTO döner.
- **İş kuralı hataları** `BusinessException` ile fırlatılır; API bunları `400 { "error": "..." }` olarak döner.

## Kurulum

### En kolay yol (Windows)

Gereksinimleri kurduktan sonra depo klasöründeki **`baslat.bat`** dosyasına çift tıklayın. Paketleri kurar, LocalDB'yi başlatır, backend ve frontend'i ayrı pencerelerde açar ve tarayıcıda http://localhost:5173 adresini açar. Veritabanı ilk açılışta otomatik oluşturulur.

Giriş ekranındaki **"Demo kurumla giriş"** butonu hazır verili örnek bir kurum açar (her girişte sıfırlanır). **"Demo denetçiyle giriş"** butonu, demo kurumu sıfırlamadan onun beyanlarını denetçi gözüyle açar. `demo/` klasöründe canlı yüklemek için iki örnek kanıt vardır: 1.2.7 için bir tutanak ve 10.4.1 için bir log yönetimi prosedürü.

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org) (LTS)
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
| GET | /api/gap | Tamam/eksik sayıları, bölüm bazında eksikler ve bulgular |
| POST | /api/auth/demo | Demo kurumu sıfırlar ve JWT döner (`Demo:Enabled`) |
| POST | /api/auth/demo-auditor | Demo kuruma erişimi olan demo denetçiyle giriş |
| GET | /api/auditors | Kuruma erişimi olan denetçiler |
| POST | /api/auditors | Denetçiye e-posta adresiyle erişim ver |
| DELETE | /api/auditors/{id} | Denetçi erişimini geri al |
| GET | /api/audit/orgs | *(denetçi)* Yetkili olunan kurumlar |
| GET | /api/audit/orgs/{orgId} | *(denetçi)* Kurumun beyanları, belgeleri ve kararlar |
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
