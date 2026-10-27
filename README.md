# PCI DSS YZ Destekli Ön Değerlendirme Platformu — Faz 1 İskeleti

## Yapı

```
backend/
  PciDss.Entities     → Varlıklar (Organization, User, Requirement, SurveyQuestion, SurveyAnswer, Policy)
  PciDss.DataAccess   → AppDbContext (EF Core Code-First + seed), Generic Repository
  PciDss.Business     → DTO'lar, AuthService (JWT + BCrypt), SurveyService, FileService
  PciDss.API          → Controller'lar, JWT/CORS/Swagger konfigürasyonu
frontend/             → React (Vite): Giriş/Kayıt, Anket Sihirbazı, Sürükle-bırak Kanıt Yükleme
```

Katman bağımlılığı tek yönlüdür: `API → Business → DataAccess → Entities`. Controller'lar entity değil DTO döner.

## Backend'i çalıştırma

```bash
cd backend
dotnet new sln -n PciDss
dotnet sln add PciDss.Entities PciDss.DataAccess PciDss.Business PciDss.API
dotnet tool install --global dotnet-ef --version 9.0.9

# appsettings.json içindeki connection string'i kendi MSSQL sunucuna göre düzenle
dotnet ef migrations add Initial -p PciDss.DataAccess -s PciDss.API
dotnet ef database update      -p PciDss.DataAccess -s PciDss.API
dotnet run --project PciDss.API
```

Swagger: `https://localhost:<port>/swagger`

## Frontend'i çalıştırma

```bash
cd frontend
npm install
echo "VITE_API_URL=https://localhost:<api-port>/api" > .env
npm run dev        # http://localhost:5173
```

## API Uç Noktaları

| Metot | Yol | Açıklama |
|---|---|---|
| POST | /api/auth/register | Kurum + kullanıcı kaydı (parola ≥ 12 karakter) |
| POST | /api/auth/login | JWT döner |
| GET | /api/scope | Kurumun kapsam beyanı ve SAQ tipi (yoksa 204) |
| PUT | /api/scope | Kapsam beyanını kaydet, SAQ tipini belirle |
| GET | /api/survey/questions | SAQ tipine göre sorular + kurumun mevcut cevapları |
| POST | /api/survey/answers | Cevapları kaydet (upsert) |
| GET | /api/survey/progress | Cevap istatistikleri |
| POST | /api/files | multipart dosya yükleme (`file`, `requirementCode`) |
| GET | /api/files | Kurumun yüklediği belgeler |

## Faz 2'ye hazırlık

- `Policy.Status` alanı (`Pending → Processing → Completed/Failed`) Python RAG servisinin güncelleyeceği alan.
- Dosyalar `uploads/{orgId}/{guid}.ext` altında; SHA-256 hash'i kaydediliyor (kanıt bütünlüğü).
- Gap bulguları için ileride `Finding` (RiskLevel: Critical/Medium/Low) varlığı eklenecek.

## Üretim öncesi notlar

- `Jwt:Key` user-secrets / ortam değişkenine taşınmalı.
- Token şu an localStorage'da; üretimde HttpOnly cookie tercih edilebilir.
- Yüklenen dosyalar için içerik (magic byte) doğrulaması ve virüs taraması eklenebilir.
