# AGENTS.md

This file provides guidance to Codex (Codex.ai/code) when working with code in this repository.

## Project Overview

**SISGE/OFTIC** is a full-stack police administration system for the Colombian National Police (OFTIC unit). It consists of an Angular 20 frontend and an ASP.NET Core 8 backend with Oracle database, featuring JWT authentication integrated with external OUD/PIP identity services.

---

## Commands

### Backend (.NET 8)

```bash
cd backend/oftic/oftic
dotnet restore
dotnet build
dotnet run                            # Dev server on port 7095
dotnet publish -c Release -o ../../publish/api
```

### Frontend (Angular 20)

```bash
cd frontend
npm ci
ng serve --proxy-config proxy.conf.json   # Dev on port 4200, proxies /api → localhost:7095
npm run build                             # Production build → dist/policiadev-app/
npm run test                              # Unit tests with Karma/Jasmine
```

### Docker (recommended for full-stack)

```bash
docker-compose up --build -d   # Runs sisge-api on port 8088
docker-compose logs -f
docker-compose down
```

### Build Scripts

```powershell
# Windows — builds and packages both backend and frontend
./scripts/build_windows.ps1
```

```bash
# Linux deployment (requires sudo)
./scripts/deploy_linux.sh
```

---

## Architecture

### Backend — 4-Layer Clean Architecture

```
oftic/              ← ASP.NET Core API (controllers, middleware, Program.cs)
oftic.bl/           ← Business logic (Gestion/ services, Interfaz/ interfaces)
oftic.dl/           ← Data access (Gestion/ repositories, Interfaz/ interfaces)
oftic.sl/           ← External service clients (OUD auth, PIP token, API integrations)
oftic.cl/           ← Shared DTOs and models (Dtos/, settings/)
```

Each domain entity follows the pattern: `I{Entity}Service` → `{Entity}Service` (bl) calls `I{Entity}Repository` → `{Entity}Repository` (dl). External API calls go through `oftic.sl` typed HTTP clients registered in DI.

**Key backend config** — `backend/oftic/oftic/appsettings.json`:
- Oracle connection string
- JWT key/issuer/audience
- External OUD/PIP API endpoints
- Upload paths and cache settings

**Program.cs** registers all DI services, two CORS policies (`Dev` for localhost:4200/4300, `Public` for external API endpoints), JWT Bearer auth, and static file serving. Max request size is 150MB (video uploads).

### Frontend — Angular 20 Standalone

```
src/app/
  pages/        ← Route-level components (login, home, administration modules)
  core/         ← Auth service, guards, HTTP interceptors, core providers
  services/     ← Feature-level business services
  components/   ← Reusable UI components
  layout/       ← Shell/layout wrapper component
  shared/       ← Utilities, shared types
  app.routes.ts ← Route definitions
  app.config.ts ← Angular providers/config
```

**Dev proxy** (`proxy.conf.json`): `/api` and `/uploads` → `http://localhost:5224` (HTTP port); `/jsonapi` → `https://www.policia.gov.co`. El backend también escucha en HTTPS en el puerto 7095 pero el proxy apunta al HTTP para evitar el warning `NODE_TLS_REJECT_UNAUTHORIZED`.

### Authentication Flow

1. User logs in via Angular login page.
2. Backend (`LoginController`) calls the external OUD service (via `oftic.sl`) with credentials.
3. On success, the backend generates a JWT and returns it to the frontend.
4. Angular stores the JWT and attaches it via an HTTP interceptor on every subsequent request.
5. Backend validates JWT on protected endpoints.

### Database

Oracle DB accessed through repositories in `oftic.dl`. SQL scripts for schema/seed data are in `docs/sql/`.

### Public API

Endpoints under `/api/branding` are CORS-enabled for external consumers. Documented in `README-API.md` with examples for multiple languages.

### Accessibility

The frontend implements a 7-level font-scaling system (WCAG 2.1 AA) stored in `localStorage`. Changes propagate via a service in `core/`.

---

## CI/CD

GitHub Actions (`.github/workflows/deploy.yml`) runs on push/PR to `main`:
1. Builds .NET backend (restore → build → publish)
2. Builds Angular frontend (`npm ci` → `ng build`)
3. On `main` branch: uploads artifacts and deploys to server via SSH

---

## Environment Setup

Copy `.env.example` to `.env` and fill in the Oracle connection string before running Docker. Backend secrets (JWT key, external API credentials) go in `appsettings.json` or environment variables — never committed.

**Local dev vs Docker:** `appsettings.json` tiene `UploadsPath: /opt/oftic/uploads` (ruta Docker Linux). Para desarrollo local existe `appsettings.Development.json` con `UploadsPath: uploads` (relativo al proyecto). Los archivos subidos en Docker no estarán disponibles al correr localmente.
