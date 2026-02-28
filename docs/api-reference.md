# Valora API Reference

The Valora API is a RESTful service built with ASP.NET Core Minimal APIs. It provides endpoints for authentication, context report generation, map data, AI assistance, and administrative functions.

## 🚀 High-Level Structure

```mermaid
graph LR
    Client[Client App] -->|HTTPS| API[Valora API]

    subgraph "API Endpoints"
        API --> Auth[/api/auth]
        API --> Context[/api/context]
        API --> Map[/api/map]
        API --> Admin[/api/admin]
        API --> AI[/api/ai]
        API --> Notify[/api/notifications]
    end
```

## 🔐 Authentication

Authentication is handled via JWT (JSON Web Tokens).

### Register
`POST /api/auth/register`

Create a new user account.

**Request Body:**
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

**Request Body:**
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

**Request Body:**
```json
{
  "refreshToken": "8f9d2..."
}
```

### External Login
`POST /api/auth/external-login`

Login with an external provider (Google, Apple).

**Request Body:**
```json
{
  "provider": "Google",
  "idToken": "eyJhbGciOiJIUz..."
}
```

---

## 🌍 Context Reports

The core feature of Valora.

### Generate Context Report
`POST /api/context/report`

Generate a comprehensive context report for a location. This endpoint uses the "Fan-Out" pattern to query multiple sources in parallel.

**Request Body:**
```json
{
  "input": "Damrak 1, Amsterdam",
  "radiusMeters": 1000
}
```

**Response:**
```json
{
  "location": {
    "latitude": 52.375,
    "longitude": 4.895,
    "address": "Damrak 1, Amsterdam",
    "neighborhoodCode": "BU03630101"
  },
  "socialMetrics": [...],
  "crimeMetrics": [...],
  "amenityMetrics": [...],
  "compositeScore": 8.5,
  "categoryScores": {
    "social": 8.0,
    "safety": 9.0,
    "amenities": 8.5
  },
  "warnings": ["Luchtmeetnet API timed out - using historical average."]
}
```

---

## 🗺️ Map Data

Endpoints for map visualization layers.

### City Insights
`GET /api/map/cities`

Get aggregated scores for major cities to display as markers or polygons.

### Map Amenities
`GET /api/map/amenities`

Get amenities within a bounding box.

**Query Parameters:**
- `minLat`, `minLon`, `maxLat`, `maxLon`: Bounding box coordinates (Required).
- `types`: Comma-separated list of amenity types (e.g., "school,park").

### Map Overlays
`GET /api/map/overlays`

Get heat map data (e.g., price per m2) for a bounding box.

**Query Parameters:**
- `minLat`, `minLon`, `maxLat`, `maxLon`: Bounding box coordinates (Required).
- `metric`: The metric to visualize. Allowed values: `PricePerSquareMeter`, `CrimeRate`, `PopulationDensity`, `AverageWoz`.

---

## 🔔 Notifications

Manage user notifications.

### List Notifications
`GET /api/notifications`

**Query Parameters:**
- `unreadOnly`: Filter by unread status (default: false).
- `limit`: Max items (default: 50, max: 100).
- `offset`: Pagination offset (default: 0).

### Unread Count
`GET /api/notifications/unread-count`

Get the number of unread notifications.

### Mark as Read
`POST /api/notifications/{id}/read`

Mark a specific notification as read.

### Mark All as Read
`POST /api/notifications/read-all`

Mark all notifications for the user as read.

### Delete Notification
`DELETE /api/notifications/{id}`

Permanently remove a notification.

---

## 🤖 AI Features

### Chat with Valora
`POST /api/ai/chat`

Chat with the AI assistant about real estate.

**Request Body:**
```json
{
  "prompt": "Is Amsterdam Safe?",
  "model": "openai/gpt-4o"
}
```

### Analyze Report
`POST /api/ai/analyze-report`

Generate a textual summary of a context report.

**Request Body:**
```json
{
  "report": {
    "location": {
      "latitude": 52.375,
      "longitude": 4.895,
      "displayAddress": "Damrak 1, Amsterdam",
      "neighborhoodCode": "BU03630101"
    },
    "compositeScore": 8.5,
    "categoryScores": {
      "social": 8.0,
      "safety": 9.0,
      "amenities": 8.5
    },
    "socialMetrics": [
      { "key": "income", "label": "Avg Income", "value": 45000, "score": 85, "source": "CBS" }
    ],
    "crimeMetrics": [],
    "amenityMetrics": [],
    "demographicsMetrics": [],
    "housingMetrics": [],
    "mobilityMetrics": [],
    "environmentMetrics": [],
    "sources": [],
    "warnings": []
  }
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

### System Statistics
`GET /api/admin/stats`

Get system-wide statistics (user count, report count, cache hits).

### Create Batch Job
`POST /api/admin/jobs`

Trigger a background job (e.g., cache warming, data ingestion).

**Request Body:**
```json
{
  "type": "CityIngestion",
  "target": "Amsterdam"
}
```

### List Batch Jobs
`GET /api/admin/jobs`

Get a list of recent background jobs.

**Query Parameters:**
- `limit`: Number of jobs to retrieve (default 10, max 100).

---

## 🏗️ Error Handling

The API uses standard HTTP status codes.

| Code | Meaning | Description |
|---|---|---|
| `200` | OK | Success. |
| `201` | Created | Resource successfully created. |
| `204` | No Content | Success, but no data to return. |
| `400` | Bad Request | Validation failure or malformed input. |
| `401` | Unauthorized | Missing or invalid JWT token. |
| `403` | Forbidden | Valid token but insufficient permissions (e.g., Admin only). |
| `404` | Not Found | Resource does not exist. |
| `429` | Too Many Requests | Rate limit exceeded. |
| `500` | Internal Server Error | Something went wrong on the server. |

Errors are returned in the standard `ProblemDetails` format (RFC 7807).

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Validation Failed",
  "status": 400,
  "errors": {
    "Email": ["The Email field is not a valid e-mail address."]
  }
}
```

## Event Catalog (Domain Events)

The following events are dispatched within the application layer using `IEventDispatcher` and handled in the infrastructure layer (e.g., `NotificationEventHandlers`).

| Event | Dispatched By | Handled By | Action |
| --- | --- | --- | --- |
| `WorkspaceInviteAcceptedEvent` | `WorkspaceMemberService` | `NotificationEventHandlers` | Sends Info notification to inviter |
| `CommentAddedEvent` | `WorkspaceListingService` | `NotificationEventHandlers` | Sends Info notification to all other workspace members |
| `ReportSavedToWorkspaceEvent` | `WorkspaceListingService` | `NotificationEventHandlers` | Sends Info notification to all other workspace members |
| `BatchJobCompletedEvent` | `BatchJobExecutor` | `NotificationEventHandlers` | Logs completion for sysadmin |
| `BatchJobFailedEvent` | `BatchJobExecutor` | `NotificationEventHandlers` | Logs failure and error message |
| `AiAnalysisCompletedEvent` | `ContextAnalysisService` | `NotificationEventHandlers` | Sends System notification to requesting user |
