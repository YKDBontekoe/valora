# Valora

Valora is a public-context intelligence platform for residential locations in the Netherlands.

It helps users understand the "vibe" and statistics of a neighborhood by aggregating data from public sources (CBS, PDOK, OpenStreetMap, Luchtmeetnet) into a unified, explainable context report.

## 🚀 Start Here

> **[Get Started in 10 Minutes](docs/onboarding.md)**: Follow the step-by-step onboarding guide to set up your environment and run the app.

---

## System Context

Valora acts as an intelligent aggregator between the user and Dutch open data ecosystems.

```mermaid
graph TD
    User((User)) -->|Input Address/URL| App[Flutter App]
    Admin((Admin)) -->|Manage| AdminApp[Admin Dashboard]
    App -->|API Request| Backend[Valora Backend]
    AdminApp -->|API Request| Backend

    subgraph "External Data Sources"
        Backend -->|Geocoding| PDOK[PDOK Locatieserver]
        Backend -->|Demographics/Crime| CBS[CBS StatLine]
        Backend -->|Amenities| OSM[OpenStreetMap (Overpass)]
        Backend -->|Air Quality| Air[Luchtmeetnet]
    end

    Backend -->|Persist| DB[(PostgreSQL)]
    Backend -->|Cache| Cache[(Memory/Redis)]
```

## Key Features

-   **Context Reports**: Generate on-demand reports for any Dutch address with scores for Safety, Social, Amenities, and Environment.
-   **Listing Enrichment**: Enhance real estate listings with persistent context data for advanced filtering.
-   **Admin Dashboard**: Web-based interface for user management, system statistics, and listing oversight.
-   **Explainable Scoring**: Transparent 0-100 scores derived from raw data (e.g., crime rates, distance to schools).
-   **Clean Architecture**: Modular, testable backend design separating Domain, Application, and Infrastructure.
-   **Resilience**: "Fan-out" data fetching ensures partial reports are generated even if one data source is down.

## Architecture

Valora follows **Clean Architecture** principles to separate concerns and maintain testability.

```mermaid
classDiagram
    class Domain {
        +Entities (Listing, Notification)
        +Business Rules
        +No Dependencies
    }
    class Application {
        +Use Cases (GetContextReport)
        +Interfaces (IContextReportService)
        +DTOs
    }
    class Infrastructure {
        +External Clients (CbsClient)
        +Persistence (EfCore)
        +Implements Interfaces
    }
    class API {
        +Endpoints
        +Configuration
        +Dependency Injection
    }

    API --> Application
    Infrastructure --> Application
    Application --> Domain
    Infrastructure --> Domain
    API ..> Infrastructure : DI Wiring Only
```

### Layer Responsibilities

-   **Valora.Domain**: Core entities and business rules. The heart of the system.
-   **Valora.Application**: Defines *what* the system does (Interfaces, Use Cases). Depends only on Domain.
-   **Valora.Infrastructure**: Defines *how* it works (External APIs, Database). Implements Application interfaces.
-   **Valora.Api**: The entry point. Wires everything together and handles HTTP requests.

## Documentation Index

-   **[Onboarding Guide](docs/onboarding.md)**: Setup instructions.
-   **[API Reference](docs/api-reference.md)**: Detailed API documentation.
-   **[Data Flow: Report Generation](docs/onboarding-data-flow.md)**: Deep dive into the "Fan-Out" process.
-   **[Data Flow: Enrichment](docs/data-flow-enrichment.md)**: How listings are stored and updated.
-   **[Developer Guide](docs/developer-guide.md)**: Coding standards, patterns, and testing.
-   **[User Guide](docs/user-guide.md)**: App features walkthrough.
-   **[Admin App Guide](apps/admin_page/README.md)**: Setup and features for the admin dashboard.

## Quick Setup

### Prerequisites
-   Docker Desktop
-   .NET 10 SDK
-   Flutter SDK
-   Node.js (for Admin App)

### 1. Infrastructure
```bash
docker-compose -f docker/docker-compose.yml up -d
```

### 2. Backend
```bash
cd backend
cp .env.example .env  # CRITICAL: Configure JWT_SECRET and DATABASE_URL
dotnet run --project Valora.Api
```

### 3. Frontend (Mobile)
```bash
cd apps/flutter_app
cp .env.example .env  # CRITICAL: Set API_URL (use 10.0.2.2 for Android Emulator)
flutter pub get
flutter run
```

### 4. Admin Dashboard (Web)
```bash
cd apps/admin_page
cp .env.example .env
npm install
npm run dev
```

## API Reference

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/context/report` | **Core:** Generate report from address/URL. |
| `POST` | `/api/listings/{id}/enrich` | Update listing with context data. |
| `POST` | `/api/auth/login` | Authenticate user. |

See [API Reference](docs/api-reference.md) for full details.
