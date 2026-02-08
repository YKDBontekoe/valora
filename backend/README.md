# Valora Backend

.NET 10 backend for location context enrichment.

## Responsibilities

- Authentication and authorization
- Context report generation (`/api/context/report`)
- Persistence layer (EF Core/PostgreSQL)
- Public API connector orchestration

## Run

```bash
cd backend
cp .env.example .env
dotnet run --project Valora.Api
```

Required `.env` keys:

- `DATABASE_URL`
- `JWT_SECRET`
- `JWT_ISSUER`
- `JWT_AUDIENCE`

Optional keys:

- `OPENROUTER_API_KEY` (only for AI chat)
- `OPENROUTESERVICE_API_KEY`, `KNMI_API_KEY`, `DUO_API_KEY` (future/optional connectors)

## Test

```bash
cd backend
dotnet test
```

Integration tests are configured for EF Core InMemory in this environment.

## Projects

- `Valora.Api`
- `Valora.Application`
- `Valora.Domain`
- `Valora.Infrastructure`
- `Valora.UnitTests`
- `Valora.IntegrationTests`
