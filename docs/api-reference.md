# Valora API Reference

The Valora API is a RESTful service built with ASP.NET Core Minimal APIs. It provides endpoints for authentication, context report generation, map data, and administrative functions.

## 🚀 High-Level Structure

```mermaid
graph LR
    Client[Client App] -->|HTTPS| API[Valora API]

    subgraph "API Endpoints"
        API --> Auth[/api/auth]
        API --> Context[/api/context]
        API --> Listings[/api/listings]
        API --> Map[/api/map]
        API --> Admin[/api/admin]
        API --> AI[/api/ai]
    end
```

## 🔐 Authentication

Authentication is handled via JWT (JSON Web Tokens).

**Flow:**
1.  **Register/Login** to get an `accessToken`.
2.  Send the token in the `Authorization` header for all protected requests:
    `Authorization: Bearer <your-token>`

### Register
`POST /api/auth/register`

Create a new user account.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!",
  "confirmPassword": "StrongPassword123!"
}
```

### Login
`POST /api/auth/login`

Authenticate and receive access tokens.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "StrongPassword123!"
}
```

**Response:**
```json
{
  "accessToken": "eyJhbGciOiJIUz...",
  "refreshToken": "8f9d2...",
  "expiresIn": 3600
}
```

---

## 🌍 Context & Listings

### Generate Context Report
`POST /api/context/report`

Generate a comprehensive context report for a location.

**Request:**
```json
{
  "input": "Damrak 1, Amsterdam",
  "radiusMeters": 1000
}
```

**Response (Example):**
```json
{
  "location": {
    "displayAddress": "Damrak 1, Amsterdam",
    "latitude": 52.375,
    "longitude": 4.896,
    "municipalityName": "Amsterdam"
  },
  "compositeScore": 8.5,
  "categoryScores": {
    "Safety": 8.0,
    "Social": 7.5,
    "Amenities": 9.2
  },
  "socialMetrics": [
    {
      "key": "population_density",
      "label": "Population Density",
      "value": 5000,
      "unit": "p/km2",
      "score": 7.0,
      "source": "CBS"
    }
  ],
  "amenityMetrics": [
    {
      "key": "supermarket_count",
      "label": "Supermarkets",
      "value": 5,
      "source": "OSM"
    }
  ],
  "warnings": []
}
```

---

## 🗺️ Map Data

Endpoints for map visualization layers.

### Map Amenities
`GET /api/map/amenities`

Get amenities within a bounding box.

**Query Parameters:**
- `minLat`, `minLon`, `maxLat`, `maxLon`: Bounding box coordinates.
- `types`: Comma-separated list of amenity types (e.g., "school,park").

**Response (Example):**
```json
[
  {
    "id": "node/12345",
    "type": "school",
    "name": "De Basisschool",
    "latitude": 52.370,
    "longitude": 4.890,
    "metadata": {
      "operator": "Public"
    }
  },
  {
    "id": "node/67890",
    "type": "park",
    "name": "Vondelpark",
    "latitude": 52.358,
    "longitude": 4.868,
    "metadata": null
  }
]
```

### Map Overlays
`GET /api/map/overlays`

Get heat map data (e.g., price per m2) for a bounding box.

**Query Parameters:**
- `metric`: The metric to visualize (e.g., `PricePerM2`).

---

## 🛡️ Admin

Restricted to users with the "Admin" role.

### List Users
`GET /api/admin/users`

Get a paginated list of users.

**Query Parameters:**
- `page`: Page number (default 1).
- `pageSize`: Items per page (default 10).

### Delete User
`DELETE /api/admin/users/{id}`

Delete a user account.
