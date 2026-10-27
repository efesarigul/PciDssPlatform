# PCI DSS Ön Değerlendirme Platformu

Kurumların PCI DSS uyumluluklarını resmi denetimden **önce** kendi başlarına değerlendirip eksiklerini görebildikleri, yapay zekâ destekli bir ön değerlendirme platformu.

> **Durum:** Faz 1 tamamlandı (kapsam belirleme, beyan anketi, kanıt yükleme). YZ destekli belge analizi ve gap analizi paneli geliştirme aşamasında.

## Proje hakkında

PCI DSS denetimleri uzun, maliyetli ve çoğu zaman sürprizlerle dolu bir süreçtir. Kurumlar hangi kontrollerinin eksik olduğunu genellikle denetçi geldiğinde öğrenir.

Bu platform süreci denetimden öne çeker:

1. **Kapsam belirleme.** Kurum kart ödemelerini nasıl kabul ettiğini beyan eder. Platform buna göre uygun **SAQ** (öz değerlendirme anketi) tipini belirler.
2. **Adım adım beyan.** Kurum yalnızca kendi SAQ tipine ait PCI DSS gereksinimlerini görür ve her biri için durumunu beyan eder.
3. **Dijital kanıt.** Beyanlar politika belgeleri, sistem raporları ve konfigürasyon çıktılarıyla desteklenir.
4. **YZ ile analiz** *(planlanan)*. Yüklenen belgeler NLP tabanlı bir RAG mimarisiyle ilgili gereksinimle karşılaştırılır ve anında uyum geri bildirimi verilir.
5. **Gap analizi paneli** *(planlanan)*. Kritik bulgular süreci durdurmaz; puanlanır ve panelde takip edilir.
6. **Uzaktan denetim** *(planlanan)*. Denetçiler kurumun hazırlık durumunu uzaktan inceleyerek resmi denetimi hızlandırır.

## Özellikler

### Mevcut (Faz 1)

- **Kurum kaydı ve çok kiracılı yapı.** Her kurum yalnızca kendi verisini görür. Kurum kimliği JWT içinden okunur, istekten alınmaz.
- **SAQ kapsam belirleme.** Kurum tipi, kart verisi saklama durumu ve ödeme kanallarına göre SAQ tipi belirlenir: A, A-EP, B, B-IP, C, C-VT, P2PE, SPoC, D (Üye İşyeri), D (Servis Sağlayıcı).
- **PCI DSS v4.0.1 soru seti.** 12 ana gereksinimi kapsayan 55 alt gereksinim, her biri için Türkçe soru ve beklenen kanıt türü.
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

### Yol haritası

| Faz | Kapsam |
|---|---|
| **Faz 1** ✅ | Kayıt, kapsam belirleme, beyan anketi, kanıt yükleme |
| **Faz 2** | Python RAG servisi: belgeleri gereksinimlerle karşılaştırma, `Policy.Status` güncelleme |
| **Faz 3** | `Finding` varlığı (Kritik/Orta/Düşük), risk puanlama, gap analizi paneli |
| **Faz 4** | Denetçi rolü, birden fazla kurumu uzaktan değerlendirme ekranı |

## SAQ tipine göre soru sayısı

| Örnek senaryo | SAQ | Soru |
|---|---|---|
| E-ticaret, ödeme sayfası tamamen ödeme kuruluşunda (iframe/yönlendirme) | A | 14 |
| E-ticaret, ödeme formu kurumun sitesinden geliyor (direct post / JS) | A-EP | 46 |
| Telefon hattına bağlı, bağımsız POS cihazı | B | 14 |
| Kart verisini elektronik ortamda saklayan üye işyeri | D (Üye İşyeri) | 53 |
| Servis sağlayıcı | D (Servis Sağlayıcı) | 55 |

> ⚠️ Gereksinimlerin SAQ tiplerine eşlemesi ve SAQ belirleme kuralları **ön değerlendirme amaçlı sadeleştirilmiştir**. Platform gerçek kurumlarla kullanılmadan önce PCI SSC'nin güncel SAQ belgeleriyle doğrulanmalıdır. Kesin SAQ tipini kurumun edinci bankası (acquirer) onaylar. Soru metinleri resmi belgelerden kopyalanmamış, Türkçe özet olarak yazılmıştır.

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
  PciDss.Entities     → Varlıklar ve enum'lar (Organization, User, ScopeProfile, Requirement, SurveyQuestion, SurveyAnswer, Policy)
  PciDss.DataAccess   → AppDbContext (EF Core + seed verisi), Generic Repository, migration'lar
  PciDss.Business     → DTO'lar ve servisler (Auth, Scope/SAQ kuralları, Survey, File)
  PciDss.API          → Controller'lar, JWT / CORS / Swagger yapılandırması
frontend/
  src/api.js          → Backend'e giden tek axios istemcisi
  src/pages/          → Giriş/Kayıt, Kapsam Belirleme, Beyan Anketi, Kanıt Yükleme
```

- **Tek yönlü katman bağımlılığı:** `API → Business → DataAccess → Entities`.
- **Controller'lar** entity değil, DTO döner.
- **İş kuralı hataları** `BusinessException` ile fırlatılır; API bunları `400 { "error": "..." }` olarak döner.

## Kurulum

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org) (LTS)
- SQL Server LocalDB. Visual Studio ile gelir; ayrıca SQL Server Express kurulumunda "LocalDB" seçilerek de kurulabilir.
- EF Core aracı: `dotnet tool install --global dotnet-ef --version 9.0.9`

> **Mac / Linux:** LocalDB yalnızca Windows'ta çalışır. SQL Server'ı Docker ile çalıştırıp `backend/PciDss.API/appsettings.json` içindeki `ConnectionStrings:Default` değerini ona göre değiştirin.

### Backend

```bash
cd backend
dotnet ef database update -p PciDss.DataAccess -s PciDss.API   # veritabanını ve soru setini oluşturur
dotnet run --project PciDss.API --urls http://localhost:5000
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

## API uç noktaları

| Metot | Yol | Açıklama |
|---|---|---|
| POST | /api/auth/register | Kurum + kullanıcı kaydı (parola ≥ 12 karakter) |
| POST | /api/auth/login | JWT döner |
| GET | /api/scope | Kurumun kapsam beyanı ve SAQ tipi (yoksa 204) |
| PUT | /api/scope | Kapsam beyanını kaydet, SAQ tipini belirle |
| GET | /api/survey/questions | SAQ tipine göre sorular + kurumun mevcut cevapları |
| POST | /api/survey/answers | Cevapları kaydet (upsert) |
| GET | /api/survey/progress | Cevap ve eksik kanıt istatistikleri |
| POST | /api/files | multipart dosya yükleme (`file`, `requirementCode`) |
| GET | /api/files | Kurumun yüklediği belgeler |

## Birlikte geliştirme notları

- **Veritabanı yapısı değişince migration ekleyin:**
  ```bash
  dotnet ef migrations add <Ad> -p PciDss.DataAccess -s PciDss.API
  ```
  Diğer geliştirici kodu çektikten sonra `dotnet ef database update` çalıştırmalıdır. İki kişi aynı anda migration eklerse çakışır; önceden haberleşin.
- **Seed verisindeki tarihler sabit kalmalı.** Gereksinim ve soru seed satırlarında `CreatedAt` sabit tutulur; aksi halde EF her derlemede gereksiz migration üretir.
- **Veritabanı ve yüklenen dosyalar yereldir.** Kullanıcılar, cevaplar ve `uploads/` klasörü geliştiriciler arasında paylaşılmaz.
- Kod içi açıklamalar, hata mesajları ve arayüz metinleri **Türkçe** tutulur.

## Üretim öncesi yapılacaklar

- `Jwt:Key` değeri `appsettings.json`'dan çıkarılıp user-secrets veya ortam değişkenine taşınmalı.
- Token şu an `localStorage`'da; üretimde HttpOnly cookie tercih edilmeli.
- Yüklenen dosyalar için içerik (magic byte) doğrulaması ve virüs taraması eklenmeli.
- SAQ eşlemesi resmi PCI SSC belgeleriyle doğrulanmalı.
