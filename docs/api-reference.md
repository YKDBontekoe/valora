# API Reference

Valora exposes a RESTful API powered by ASP.NET Core Minimal APIs. All endpoints are prefixed with `/api`.

## Authentication

Most endpoints require a valid JWT token in the `Authorization` header.

**Header Format:**
`Authorization: Bearer <your-token>`

## Listings

### 1. Search Listings
Retrieve a paginated list of listings based on filters.

**Endpoint:** `GET /api/listings`

**Query Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `searchTerm` | string | Search by address or city. |
| `minPrice` | decimal | Minimum price. |
| `maxPrice` | decimal | Maximum price. |
| `city` | string | Filter by city name. |
| `minBedrooms` | int | Minimum number of bedrooms. |
| `minLivingArea` | int | Minimum living area (m²). |
| `maxLivingArea` | int | Maximum living area (m²). |
| `minSafetyScore` | double | Filter by minimum safety score (0-100). |
| `minCompositeScore` | double | Filter by minimum Valora composite score (0-100). |
| `sortBy` | string | Sort field: `Price`, `Date`, `LivingArea`, `City`, `ContextCompositeScore`, `ContextSafetyScore`. |
| `sortOrder` | string | `asc` or `desc`. |
| `page` | int | Page number (default: 1). |
| `pageSize` | int | Items per page (default: 10, max: 100). |

**Response (200 OK):**
```json
{
  "items": [ ... ],
  "pageIndex": 1,
  "totalPages": 5,
  "totalCount": 42,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

### 2. Get Listing Details
Retrieve detailed information for a specific listing, including its enriched context report if available.

**Endpoint:** `GET /api/listings/{id}`

**Path Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `id` | uuid | The unique identifier of the listing. |

**Response (200 OK):**
Returns a `ListingDto` object.

### 3. PDOK Lookup
Lookup a property by its PDOK ID and enrich it with neighborhood analytics.

**Endpoint:** `GET /api/listings/lookup`

**Query Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `id` | string | The PDOK ID of the property. |

## Context Reports

### 1. Generate Context Report
Generate an on-demand context report for any Dutch address or Funda URL.

**Endpoint:** `POST /api/context/report`

**Request Body (`application/json`):**
```json
{
  "input": "Damrak 1, Amsterdam",
  "radiusMeters": 1000
}
```

| Field | Type | Required | Description |
|---|---|---|---|
| `input` | string | Yes | Address string (e.g., "Damrak 1, Amsterdam") or a valid Funda URL. |
| `radiusMeters` | int | No | Analysis radius in meters. Default: 1000. Range: 100-5000. |

**Response (200 OK):**
Returns a `ContextReportDto`.

```json
{
  "location": {
    "displayAddress": "Damrak 1, 1012 LG Amsterdam",
    "latitude": 52.374,
    "longitude": 4.896,
    ...
  },
  "compositeScore": 82.5,
  "categoryScores": {
    "Social": 75.0,
    "Safety": 88.0,
    ...
  },
  "socialMetrics": [ ... ],
  "crimeMetrics": [ ... ],
  "amenityMetrics": [ ... ],
  "sources": [ ... ],
  "warnings": []
}
```

## Enrichment

### 1. Enrich Listing
Trigger the enrichment process for an existing listing. This generates a context report and persists scores to the database for filtering.

**Endpoint:** `POST /api/listings/{id}/enrich`

**Path Parameters:**
| Parameter | Type | Description |
|---|---|---|
| `id` | uuid | The unique identifier of the listing. |

**Response (200 OK):**
```json
{
  "message": "Listing enriched successfully",
  "compositeScore": 82.5
}
```
