# Valora

Valora is a public-context intelligence platform for residential locations in the Netherlands.

The app accepts a listing link or plain address as input, resolves it to a location, and generates a context report using public/open APIs (CBS, PDOK, OSM/Overpass, Luchtmeetnet, and others as configured).

## Product Direction

- Input: listing URL or address text
- Resolution: normalized address + coordinates + admin codes
- Enrichment: public API connectors queried on demand
- Output: explainable context report (social, amenities, environment, accessibility-ready)

## What Changed

Valora no longer runs a Funda scraper pipeline. Scraping jobs and scraper endpoints were removed.

## Configuration and API Keys

### Backend (`backend/.env`)

Required for local run:

- `DATABASE_URL`
- `JWT_SECRET`
- `JWT_ISSUER`
- `JWT_AUDIENCE`

Optional (feature-dependent):

- `OPENROUTER_API_KEY`: only required when using `/api/ai/chat`
- `OPENROUTER_MODEL`
- `OPENROUTER_BASE_URL`
- `OPENROUTER_SITE_URL`
- `OPENROUTER_SITE_NAME`

Optional source overrides (default public endpoints are already set in `.env.example`):

- `CONTEXT_PDOK_BASE_URL`
- `CONTEXT_CBS_BASE_URL`
- `CONTEXT_OVERPASS_BASE_URL`
- `CONTEXT_LUCHTMEETNET_BASE_URL`

Optional cache tuning:

- `CONTEXT_RESOLVER_CACHE_MINUTES`
- `CONTEXT_CBS_CACHE_MINUTES`
- `CONTEXT_AMENITIES_CACHE_MINUTES`
- `CONTEXT_AIR_CACHE_MINUTES`
- `CONTEXT_REPORT_CACHE_MINUTES`

### Frontend (`apps/flutter_app/.env`)

- `API_URL` (required)
- `SENTRY_DSN` (optional)

## Quick Start

### Prerequisites

- Docker Desktop
- .NET 10 SDK
- Flutter SDK

### 1. Start infrastructure

```bash
docker-compose -f docker/docker-compose.yml up -d
```

### 2. Run backend

```bash
cd backend
cp .env.example .env
dotnet run --project Valora.Api
```

Backend health check:

```bash
curl http://localhost:5001/api/health
```

### 3. Run frontend

```bash
cd apps/flutter_app
cp .env.example .env
flutter pub get
flutter run
```

## Core API Endpoints

| Method | Endpoint | Auth | Description |
|---|---|---|---|
| `GET` | `/api/health` | No | Service + DB connectivity check |
| `POST` | `/api/auth/register` | No | Register user |
| `POST` | `/api/auth/login` | No | Login and receive JWT |
| `POST` | `/api/auth/refresh` | No | Refresh token |
| `POST` | `/api/context/report` | Yes | Generate enrichment report from link/address |

## Architecture

Valora follows Clean Architecture:

- `Valora.Domain`: entities and core business concepts
- `Valora.Application`: interfaces, DTOs, use case contracts
- `Valora.Infrastructure`: EF Core, external API connectors, service implementations
- `Valora.Api`: Minimal API entrypoint, auth, endpoint wiring

## Documentation

- `docs/onboarding.md`
- `docs/developer-guide.md`
- `docs/user-guide.md`
- `backend/README.md`
- `apps/flutter_app/README.md`
