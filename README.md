# PCI DSS Ön Değerlendirme Platformu

Kurumların PCI DSS uyumluluklarını resmi denetimden **önce** kendi başlarına değerlendirip eksiklerini görebildikleri, yapay zekâ destekli bir ön değerlendirme platformu.

> **Durum:** Faz 1 ve Faz 2 tamamlandı: kapsam belirleme, beyan anketi, kanıt yükleme, RAG tabanlı YZ belge analizi ve gap analizi paneli. Faz 3'te bulguların kalıcılaştırılması ve raporlama sürüyor.

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

### Mevcut

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
- **YZ destekli kanıt analizi (RAG).** Bir gereksinimle ilişkilendirilen her belge arka planda Python servisine gönderilir:
  1. Metin çıkarılır (PDF, DOCX, TXT, MD) ve LangChain ile parçalara bölünür.
  2. Parçalar Gemini embedding'leriyle ChromaDB vektör veritabanına yazılır.
  3. Gereksinimle anlamca en ilgili parçalar geri getirilir.
  4. Gemini, yalnızca bu parçalara dayanarak "Uyumlu / Kısmen uyumlu / Uyumsuz" kararını, eksikleri ve belgeden alıntıları JSON olarak döner.

  Model yoğunsa sıradaki yedek modele geçilir.
- **Gap analizi paneli.** Uyum skoru, 12 gereksinim için ısı haritası ve Kritik/Orta/Düşük bulgular. YZ'nin uyumsuz bulduğu kanıtlar kritik bulgu olarak işaretlenir. Bulgudan tek tıkla ilgili soruya veya belgeye gidilir.

### Yol haritası

| Faz | Kapsam |
|---|---|
| **Faz 1** ✅ | Kayıt, kapsam belirleme, beyan anketi, kanıt yükleme |
| **Faz 2** ✅ | Python RAG servisi (LangChain, ChromaDB, Gemini): belgeleri gereksinimlerle karşılaştırma |
| **Faz 3** | Gap analizi paneli ✅; `Finding` varlığıyla bulguların kalıcılaştırılması, raporlama |
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
| YZ servisi | Python, FastAPI, LangChain, ChromaDB, Google Gemini (sohbet + embedding) |

## Mimari

```
backend/
  PciDss.Entities     → Varlıklar ve enum'lar (Organization, User, ScopeProfile, Requirement, SurveyQuestion, SurveyAnswer, Policy)
  PciDss.DataAccess   → AppDbContext (EF Core + seed verisi), Generic Repository, migration'lar
  PciDss.Business     → DTO'lar ve servisler (Auth, Scope/SAQ kuralları, Survey, File, Gap, Demo, YZ analiz kuyruğu)
  PciDss.API          → Controller'lar, JWT / CORS / Swagger yapılandırması
frontend/
  src/api.js          → Backend'e giden tek axios istemcisi
  src/pages/          → Giriş/Kayıt, Kapsam Belirleme, Beyan Anketi, Kanıt Yükleme, Gap Analizi
ai-service/
  app.py              → FastAPI RAG servisi (/analyze, /health)
demo/                 → Sunumda canlı yüklenecek örnek kanıtlar (biri bilerek eksik yazılmıştır)
```

YZ akışı: `Kanıt yükle → Policy (Pending) → AnalysisWorker → ai-service /analyze → Policy (Completed + karar) → Gap analizi`

- **Tek yönlü katman bağımlılığı:** `API → Business → DataAccess → Entities`.
- **Controller'lar** entity değil, DTO döner.
- **İş kuralı hataları** `BusinessException` ile fırlatılır; API bunları `400 { "error": "..." }` olarak döner.

## Kurulum

### En kolay yol (Windows)

Gereksinimleri kurduktan sonra depo klasöründeki **`baslat.bat`** dosyasına çift tıklayın. Paketleri kurar, LocalDB'yi başlatır, YZ servisi, backend ve frontend'i ayrı pencerelerde açar ve tarayıcıda http://localhost:5173 adresini açar. Veritabanı ilk açılışta otomatik oluşturulur.

Giriş ekranındaki **"Demo kurumla giriş"** butonu hazır verili örnek bir kurum açar (her girişte sıfırlanır). `demo/` klasöründe canlı yüklemek için iki örnek kanıt vardır: 1.2.7 için uyumlu bir tutanak ve 10.4.1 için YZ'nin yakalaması gereken eksik bir prosedür.

**YZ anahtarı:** `ai-service/.env.example` dosyasını `ai-service/.env` olarak kopyalayıp `GOOGLE_API_KEY` değerini yazın ([anahtar al](https://aistudio.google.com/apikey)). `.env` depoya girmez; her geliştirici kendi anahtarını kullanır. Anahtar yoksa platform çalışır ama belgeler "Analiz sırada" kalır.

### Gereksinimler

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Node.js](https://nodejs.org) (LTS)
- [Python](https://www.python.org) 3.11–3.13 (YZ servisi için)
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
5. **Gap Analizi**'nde uyum skorunu, gereksinim bazlı ısı haritasını ve bulguları görün.

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
| POST | /api/files/{id}/analyze | Belgeyi yeniden YZ analizine al |
| GET | /api/gap | Uyum skoru, bölüm skorları ve bulgular |
| POST | /api/auth/demo | Demo kurumu sıfırlar ve JWT döner (`Demo:Enabled`) |

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
