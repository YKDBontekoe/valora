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
        API --> Profile[/api/user]
        API --> Workspace[/api/workspaces]
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

### Map Overlay Tiles
`GET /api/map/overlays/tiles`

Get pre-computed rasterized overlay tiles for map visualization. Output is generated using a spatial prefilter to efficiently match raster points to polygon geometries.

**Query Parameters:**
- `minLat`, `minLon`, `maxLat`, `maxLon`: Bounding box coordinates (Required).
- `zoom`: Map zoom level used to determine tile resolution.
- `metric`: The metric to visualize.

**Caching Behavior:**
Responses are cached in-memory with a 10-minute TTL. The cache key is generated from the bounding box (normalized/rounded to 4 decimal places), zoom bucket, and metric:
`{minLat}_{minLon}_{maxLat}_{maxLon}_{zoom}_{metric}`

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

## 👥 User Profile

Manage AI personalization and user settings.

### Get User AI Profile
`GET /api/user/ai-profile`

Retrieve the current user's AI personalization profile.

### Update User AI Profile
`PUT /api/user/ai-profile`

Update the current user's AI personalization profile.

**Request Body:**
```json
{
  "preferences": "I prefer quiet neighborhoods with easy access to the highway.",
  "householdProfile": "Couple, 30s, working from home.",
  "disallowedSuggestions": ["Apartments on busy streets", "Houses without a garden"],
  "isEnabled": true,
  "isSessionOnlyMode": false
}
```

### Export User AI Profile
`GET /api/user/ai-profile/export`

Export the current user's AI profile as a JSON file.

---

## 📁 Workspaces

Manage collaborative environments for organizing saved context reports.

### List User Workspaces
`GET /api/workspaces`

Retrieve a list of workspaces the current user is a member of.

### Create Workspace
`POST /api/workspaces`

Create a new workspace.

**Request Body:**
```json
{
  "name": "Amsterdam Search",
  "description": "Properties we are considering in Amsterdam."
}
```

### Get Workspace Details
`GET /api/workspaces/{id}`

Retrieve the details of a specific workspace, including its members and recent activity.

### Delete Workspace
`DELETE /api/workspaces/{id}`

Permanently delete a workspace. Only the workspace owner can perform this action.

### List Workspace Members
`GET /api/workspaces/{id}/members`

List all members of the workspace.

### Invite Workspace Member
`POST /api/workspaces/{id}/members`

Invite a user to the workspace.

**Request Body:**
```json
{
  "email": "colleague@example.com",
  "role": "Viewer"
}
```

### Remove Workspace Member
`DELETE /api/workspaces/{id}/members/{memberId}`

Remove a member from the workspace.

### List Saved Properties
`GET /api/workspaces/{id}/properties`

Retrieve all properties (saved context reports) in the workspace.

### Save Property (By ID)
`POST /api/workspaces/{id}/properties`

Save an existing property to the workspace using its ID.

**Request Body:**
```json
{
  "propertyId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "notes": "Good potential."
}
```

### Save Property (From Existing Report)
`POST /api/workspaces/{id}/properties/from-report`

Save a dynamically generated context report directly to the workspace for persistence.

**Request Body:**
```json
{
  "report": { ... },
  "notes": "Looks like a great neighborhood!"
}
```

### Delete Saved Property
`DELETE /api/workspaces/{id}/properties/{savedPropertyId}`

Remove a saved property from the workspace.

### List Property Comments
`GET /api/workspaces/{id}/properties/{savedPropertyId}/comments`

Retrieve all comments on a specific saved property.

### Add Property Comment
`POST /api/workspaces/{id}/properties/{savedPropertyId}/comments`

Add a new comment to a saved property.

**Request Body:**
```json
{
  "content": "Is the price negotiable?",
  "parentId": null
}
```

### List Workspace Activity
`GET /api/workspaces/{id}/activity`

Retrieve an audit log of recent actions in the workspace (e.g., members joining, properties added).

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
