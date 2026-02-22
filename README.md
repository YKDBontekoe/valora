# Valora

**Valora is a public-context intelligence platform for residential locations in the Netherlands.**

It helps users understand the "vibe" and statistics of a neighborhood by aggregating data from public sources (CBS, PDOK, OpenStreetMap, Luchtmeetnet) into a unified, explainable context report.

> **Valora is NOT a scraper.** It does not copy listing photos or descriptions. It enriches location data with public context.

---

## 🚀 Quick Start (10 Minutes)

Follow these steps to get the entire system running locally.

### Prerequisites
- **Docker Desktop** (for database)
- **.NET 10.0 SDK** (for backend)
- **Flutter SDK** (for mobile app)
- **Node.js 18+** (for admin dashboard)

### 1. Start Infrastructure
Run the database container.
```bash
docker-compose -f docker/docker-compose.yml up -d
```
*Troubleshooting:*
- If `docker-compose` fails, ensure Docker Desktop is running.
- Ensure port `5432` is not already in use by another Postgres instance.

### 2. Configure & Run Backend
The backend aggregates data and serves the API.

**Note:** The `.env` file is primarily used by Docker. When running locally via `dotnet run`, configuration is read from `appsettings.Development.json` or Environment Variables.

```bash
cd backend
# Option A: Set secrets via User Secrets (Recommended)
dotnet user-secrets init --project Valora.Api
dotnet user-secrets set "JWT_SECRET" "YourStrongSecretKeyHere_MustBeAtLeast32CharsLong!" --project Valora.Api

# Option B: Set Environment Variable manually
export JWT_SECRET="YourStrongSecretKeyHere_MustBeAtLeast32CharsLong!"

# Run the API
dotnet run --project Valora.Api
```
*Verify: Open `http://localhost:5253/api/health` in your browser. You should see `{"status":"healthy", "timestamp": "..."}`.*

### 3. Configure & Run Mobile App
The Flutter app is the primary interface for users.

> ⚠️ **CRITICAL**: The default `.env` points to the PRODUCTION API.
> Change `API_URL` in `.env` to your local backend:
> - Android Emulator: `http://10.0.2.2:5001/api`
> - iOS Simulator / Desktop: `http://localhost:5001/api`

```bash
cd ../apps/flutter_app
cp .env.example .env
flutter pub get
flutter run
```

### 4. Configure & Run Admin Dashboard
The web dashboard for managing users and system settings.

```bash
cd ../apps/admin_page
cp .env.example .env
npm install
npm run dev
```

---

## 🏗️ Architecture

Valora follows **Clean Architecture** principles to ensure modularity and testability.

### System Overview

```mermaid
graph TD
    subgraph "Clients"
        Flutter[Flutter App]
        Admin[Admin Dashboard]
    end

    subgraph "Valora Backend"
        API[API Layer (Valora.Api)]
        App[Application Layer (MediatR)]
        Domain[Domain Layer (Entities)]
        Infra[Infrastructure Layer]
    end

    subgraph "Persistence"
        DB[(PostgreSQL)]
        Cache[(Memory Cache)]
    end

    subgraph "External Data Sources"
        PDOK[PDOK Locatieserver]
        CBS[CBS Open Data]
        OSM[OpenStreetMap]
        Air[Luchtmeetnet]
    end

    Flutter -->|HTTPS| API
    Admin -->|HTTPS| API

    API --> App
    App --> Domain
    App --> Infra

    Infra --> DB
    Infra --> Cache
    Infra --> PDOK
    Infra --> CBS
    Infra --> OSM
    Infra --> Air
```

### The "Fan-Out" Aggregation Pattern
When a user requests a context report, the system queries multiple external sources in parallel ("Fan-Out") and then aggregates the results ("Fan-In") into a unified score.

```mermaid
graph TD
    User((User)) -->|1. Request Report| API[Valora API]
    API -->|2. Orchestrate| Service[ContextReportService]

    subgraph "Fan-Out (Parallel Execution)"
        Service -->|Geocode| PDOK[PDOK Locatieserver]
        Service -->|Stats| CBS[CBS Open Data]
        Service -->|Amenities| OSM[OpenStreetMap / Overpass]
        Service -->|Air Quality| Air[Luchtmeetnet]
    end

    PDOK -->|Coords| Service
    CBS -->|Demographics| Service
    OSM -->|Shops/Parks| Service
    Air -->|PM2.5| Service

    Service -->|3. Normalize & Score| Service
    Service -->|4. Return Report| API
    API -->|5. Response| User

    API -.->|Async Cache| RAM[(In-Memory Cache)]
    API -.->|Persist (Optional)| DB[(PostgreSQL)]
```

### Key Components

| Layer | Responsibility | Key Tech |
|---|---|---|
| **Valora.Domain** | Core business rules and entities. Zero dependencies. | C# |
| **Valora.Application** | Use cases (e.g., `GetContextReport`). Orchestrates data flow. | MediatR |
| **Valora.Infrastructure** | External integrations (Database, APIs). | EF Core, HttpClient |
| **Valora.Api** | Entry point. Configuration, Auth, and HTTP handling. | ASP.NET Core Minimal APIs |
| **Flutter App** | Cross-platform mobile client. | Flutter, Provider |

---

## 💡 Key Concepts

### 1. The "Fan-Out" Context Report
When a user requests a report for an address, Valora does **not** look up a pre-existing record. It generates the report in real-time by querying multiple external sources in parallel.

- **Why?** Data freshness and coverage. We don't need to scrape or store millions of records.
- **How?** See `ContextReportService.cs`. It uses `Task.WhenAll` to fetch data from CBS, PDOK, and OSM simultaneously.

---

## 📂 Project Structure

```
├── apps/
│   ├── flutter_app/      # The primary mobile application
│   └── admin_page/       # Web dashboard for user management
├── backend/
│   ├── Valora.Api/           # API Entry point
│   ├── Valora.Application/   # Business logic & Use cases
│   ├── Valora.Domain/        # Core entities (Enterprise logic)
│   └── Valora.Infrastructure/# External implementations (DB, APIs)
├── docker/               # Docker Compose files
└── docs/                 # Detailed documentation
```

## 📚 Documentation Index

- **[Onboarding Guide](docs/onboarding.md)**: Detailed setup & troubleshooting.
- **[Developer Guide](docs/developer-guide.md)**: Coding standards & patterns.
- **[API Reference](docs/api-reference.md)**: Endpoints & contracts.
- **[Data Flow: Reading (Reports)](docs/onboarding-data-flow.md)**: Deep dive into the aggregation engine.
- **[Data Flow: Writing (Persistence)](docs/onboarding-persistence-flow.md)**: User registration and data saving.
- **[Admin App Guide](apps/admin_page/README.md)**: Setup and features for the admin dashboard.

---

*Missing documentation? Open an issue or check the `docs/` folder.*
