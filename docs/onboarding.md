# Valora Onboarding

## Goal

Get a local environment running and generate your first context report.

## First 10 Minutes

1. Start infrastructure:

```bash
docker-compose -f docker/docker-compose.yml up -d
```

2. Run backend:

```bash
cd backend
cp .env.example .env
dotnet run --project Valora.Api
```

Minimum backend `.env` values to verify before running:

- `DATABASE_URL`
- `JWT_SECRET`
- `JWT_ISSUER`
- `JWT_AUDIENCE`

3. Verify backend:

```bash
curl http://localhost:5001/api/health
```

4. Run frontend:

```bash
cd apps/flutter_app
cp .env.example .env
flutter pub get
flutter run
```

Minimum Flutter `.env` value:

- `API_URL` (defaults to `http://localhost:5001/api` in `.env.example`)

5. Login, open the `Report` tab, and submit an address/link.

## Product Mental Model

- Valora does not harvest listing content.
- Listing links are treated as location hints.
- The main output is a public-data context report, not listing replication.

## Backend Data Flow

1. Endpoint receives input.
2. Location resolver normalizes input to coordinates/admin codes.
3. Source clients fetch data.
4. Report service aggregates + scores + attributes sources.
5. API returns report.

## Where To Change What

- Add a new data source: `backend/Valora.Application/Common/Interfaces` + `backend/Valora.Infrastructure/Enrichment`
- Adjust scoring: `backend/Valora.Infrastructure/Enrichment/ContextReportService.cs`
- Update endpoint behavior: `backend/Valora.Api/Program.cs`
- Update UI: `apps/flutter_app/lib/screens/context_report_screen.dart`

## Common Issues

- `401` on report endpoint: authenticate first.
- Empty/partial reports: one or more upstream sources may be unavailable; check `warnings` in response.
- Connectivity issues: verify `API_URL` in Flutter `.env` and backend `DATABASE_URL`.
