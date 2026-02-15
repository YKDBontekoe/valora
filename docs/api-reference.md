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

    subgraph "Services"
        Auth --> Identity[Identity Service]
        Context --> Report[Context Report Service]
        Listings --> Repo[Listing Repository]
        Map --> MapSvc[Map Service]
        Admin --> AdminSvc[Admin Services]
        AI --> AISvc[AI Service]
    end
```

## 🔐 Authentication

Authentication is handled via JWT (JSON Web Tokens).

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

### Refresh Token
`POST /api/auth/refresh`

Obtain a new access token using a valid refresh token.

**Request:**
```json
{
  "refreshToken": "8f9d2..."
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

**Response:**
Returns a `ContextReportDto` containing scores for Safety, Social, Amenities, and more.

### Enrich Listing
`POST /api/listings/{id}/enrich`

Update an existing listing with context data.

---

## 🗺️ Map Data

Endpoints for map visualization layers.

### City Insights
`GET /api/map/cities`

Get aggregated scores for major cities.

### Map Amenities
`GET /api/map/amenities`

Get amenities within a bounding box.

**Query Parameters:**
- `minLat`, `minLon`, `maxLat`, `maxLon`: Bounding box coordinates.
- `types`: Comma-separated list of amenity types (e.g., "school,park").

### Map Overlays
`GET /api/map/overlays`

Get heat map data (e.g., price per m2) for a bounding box.

**Query Parameters:**
- `metric`: The metric to visualize (e.g., `PricePerM2`).

---

## 🤖 AI Features

### Chat with Valora
`POST /api/ai/chat`

Chat with the AI assistant about real estate.

**Request:**
```json
{
  "prompt": "Is Amsterdam Safe?",
  "model": "openai/gpt-4o"
}
```

### Analyze Report
`POST /api/ai/analyze-report`

Generate a textual summary of a context report.

**Request:**
```json
{
  "report": { ...ContextReportDto... }
}
```

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
