# Valora Developer Guide

## Overview

Valora is an enrichment backend + Flutter client. It does not scrape listing sites. It builds context reports from public APIs at request time.

## Clean Architecture Layers

1. `Valora.Domain`
- Pure entities and domain concerns.
- No dependencies on external packages/layers.

2. `Valora.Application`
- Interfaces and DTO contracts for use cases.
- Key contracts include `IContextReportService`, `ILocationResolver`, and source client interfaces.

3. `Valora.Infrastructure`
- Implements application interfaces.
- Handles EF Core persistence and HTTP connectors for external/public data sources.

4. `Valora.Api`
- Minimal APIs for auth, health, and context report generation.
- No business logic in endpoints.

## Context Report Flow

1. Receive input (`address` or `listing link`).
2. Resolve location (PDOK): display address, coordinates, admin codes.
3. Query enrichment clients in parallel (CBS, Overpass, Luchtmeetnet, etc.).
4. Build normalized metrics and composite score.
5. Return report with source attribution and warnings for missing sources.

## Key Endpoint

### `POST /api/context/report`

Auth required.

Request:

```json
{
  "input": "Damrak 1 Amsterdam",
  "radiusMeters": 1000
}
```

Response includes:

- `location`
- `socialMetrics`
- `crimeMetrics`
- `demographicsMetrics`
- `housingMetrics`
- `mobilityMetrics`
- `amenityMetrics`
- `environmentMetrics`
- `compositeScore`
- `categoryScores`
- `sources`
- `warnings`

## Configuration

Core environment variables:

| Variable | Description |
|---|---|
| `DATABASE_URL` | PostgreSQL connection string |
| `JWT_SECRET` | JWT signing secret |
| `JWT_ISSUER` | JWT issuer |
| `JWT_AUDIENCE` | JWT audience |
| `API_URL` | Flutter app backend URL (`apps/flutter_app/.env`) |
| `CONTEXT_PDOK_BASE_URL` | Optional PDOK base URL override |
| `CONTEXT_CBS_BASE_URL` | Optional CBS base URL override |
| `CONTEXT_OVERPASS_BASE_URL` | Optional Overpass base URL override |
| `CONTEXT_LUCHTMEETNET_BASE_URL` | Optional Luchtmeetnet base URL override |
| `CONTEXT_*_CACHE_MINUTES` | Per-source cache TTL tuning |

API key expectations:

- No key required by default for PDOK, CBS StatLine, Overpass, and Luchtmeetnet base connectors.
- No key required for Flutter-side Kadaster/PDOK WMS property imagery (`service.pdok.nl/hwh/luchtfotorgb/wms/v1_0`).
- `OPENROUTER_API_KEY` is required only for AI chat endpoints.
- Optional future connectors may require keys (for example ORS/KNMI/DUO); keep those values in `.env` and never commit secrets.

## Testing

### Backend

```bash
cd backend
dotnet test
```

Integration tests use EF Core InMemory in this environment.

### Frontend

```bash
cd apps/flutter_app
flutter analyze
flutter test
```

## Coding Notes

- Keep source connectors isolated behind application interfaces.
- Favor graceful degradation: missing source data should return warnings, not 500.
- Keep scoring explainable with clear metric/source mapping.
