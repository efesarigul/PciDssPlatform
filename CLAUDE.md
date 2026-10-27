# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Phase 1 skeleton of an AI-assisted PCI DSS pre-assessment platform: organizations register, fill in a self-declaration survey mapped to PCI DSS requirements, and upload evidence documents. The AI gap analysis (Phase 2/3) does not exist yet.

User-facing strings, error messages, code comments and the README are in Turkish — keep new ones in Turkish.

## Commands

Backend (.NET 9, run from `backend/`):

```bash
dotnet build PciDss.sln
dotnet run --project PciDss.API          # Swagger at /swagger (Development only)

# EF Core migrations: DbContext lives in DataAccess, startup project is API
dotnet ef migrations add <Name> -p PciDss.DataAccess -s PciDss.API
dotnet ef database update      -p PciDss.DataAccess -s PciDss.API
```

`dotnet ef` needs the global tool: `dotnet tool install --global dotnet-ef --version 9.0.9`.

Frontend (React 18 + Vite, run from `frontend/`):

```bash
npm install
npm run dev        # http://localhost:5173
npm run build
```

There are no test projects, linters or formatters configured on either side.

## Local environment

- Database is SQL Server LocalDB (`(localdb)\MSSQLLocalDB`, database `PciDssDb`), configured in `backend/PciDss.API/appsettings.json`. LocalDB puts the `.mdf`/`.ldf` files in the user profile folder, not in the repo.
- There is no `launchSettings.json`, so the API listens on whatever Kestrel defaults to / `ASPNETCORE_URLS` says. `frontend/.env` (`VITE_API_URL`, must end in `/api`) has to match it; the fallback in `frontend/src/api.js` is `https://localhost:7001/api`.
- CORS allows only `http://localhost:5173` (policy `spa` in `Program.cs`). Changing the Vite port means changing that too.
- Uploaded files land in `Storage:Root` (`uploads`), resolved relative to the API's working directory — i.e. `backend/PciDss.API/uploads/{orgId}/{guid}.ext`.

## Architecture

### Backend layering

Strict one-way dependency chain, enforced by project references: `PciDss.API → PciDss.Business → PciDss.DataAccess → PciDss.Entities`.

Each layer is deliberately a handful of consolidated files rather than one class per file: all entities and enums in `Entities.cs`, all DTOs in `Dtos.cs`, all service interfaces + implementations in `Services.cs`, all controllers in `Controllers.cs`. Add to these files rather than creating new ones per class.

- **Entities** — POCOs deriving from `BaseEntity` (`Id`, `CreatedAt`).
- **DataAccess** — `AppDbContext` (Code-First config and seed data via `HasData` in `OnModelCreating`) and a generic `IRepository<T>`. Services never touch `AppDbContext` directly; they inject `IRepository<T>` and use `Query()` for anything beyond simple lookups. `SaveChangesAsync()` on any repository commits the whole scoped context, so one call persists changes made through several repositories.
- **Business** — services take primary-constructor dependencies, accept/return DTOs (records), and signal user-facing failures by throwing `BusinessException`.
- **API** — thin controllers that return DTOs, never entities. `Program.cs` wires DI, JWT, CORS and Swagger; new services/repositories must be registered there.

### Cross-cutting conventions

- **Multi-tenancy by JWT claim.** The token carries an `org` claim (organization id). Controllers inherit `OrgControllerBase` and pass its `OrgId` into every service call; services filter every query by `orgId`. Any new org-owned data must follow this — never take an organization id from the request body.
- **Errors.** An inline middleware in `Program.cs` turns `BusinessException` into `400 { "error": "<message>" }`. The frontend's axios response interceptor surfaces `error` as the thrown `Error` message, so the exception text is shown to the user verbatim.
- **Enums over the wire are strings** (`JsonStringEnumConverter`): `AnswerValue`, `AnalysisStatus`, `UserRole`, `SaqType`, the scope enums. The frontend sends/compares names like `"InPlace"`, `"NotApplicable"`, `"Pending"`.
- **Survey questions are filtered by SAQ type.** The org's `ScopeProfile` (payment channels etc.) determines its `SaqType` via `SaqRules.Determine` in `Services.cs`; `SaqRules.QuestionsFor` filters questions by `Requirement.Applicability` (flags; SAQ D gets everything, SAQ D merchant excludes `ServiceProviderOnly`). The survey endpoints throw until scope is set. `InPlaceWithCcw`, `NotApplicable` and `NotTested` answers require a comment (enforced server-side).
- **Seed data changes require a migration.** `Requirement` and `SurveyQuestion` rows are seeded with fixed ids and fixed `CreatedAt` values in `AppDbContext`; keep `CreatedAt` constant in seed rows or EF generates a spurious migration each time.
- **Survey answers are upserts**, one per (organization, question), backed by a unique index — the wizard saves on every step.
- **File uploads** never use the client-supplied filename on disk (stored as a GUID; original name kept in `Policy.OriginalFileName`), are limited to `.pdf/.docx/.txt/.md` and 10 MB (limit is duplicated in `FileService` and the controller's `RequestSizeLimit`), and record a SHA-256 for evidence integrity.

### Frontend

`src/api.js` is the single gateway to the backend: one axios instance that attaches the bearer token from `localStorage`, redirects to `/login` on 401, and exports `authApi` / `surveyApi` / `filesApi`. Pages call those rather than axios directly. Auth state is just `localStorage` (`token`, `org`); `Protected` in `App.jsx` gates routes on the token's presence. Styling is one global `src/styles.css`.

### Planned (from README, not yet built)

- `Policy.Status` (`Pending → Processing → Completed/Failed`) is the field a future Python RAG service will update.
- A `Finding` entity (RiskLevel: Critical/Medium/Low) is planned for gap results.
- Before production: move `Jwt:Key` out of `appsettings.json` into user-secrets/env vars; consider HttpOnly cookies instead of `localStorage`; add magic-byte validation and virus scanning for uploads.
