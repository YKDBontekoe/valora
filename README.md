# Valora

**Valora is a public-context intelligence platform for residential locations in the Netherlands.**

It helps users understand the "vibe" and statistics of a neighborhood by aggregating data from public sources (CBS, PDOK, OpenStreetMap, Luchtmeetnet) into a unified, explainable context report.

> **Valora is NOT a scraper.** It does not copy listing photos or descriptions. It enriches location data with public context.

---

## 🚀 Quick Start (10 Minutes)

Follow these steps to get the entire system running locally.

### Prerequisites
- **Docker Desktop** (for database)
- **.NET 8.0 SDK** (for backend)
- **Flutter SDK** (for mobile app)

### 1. Start Infrastructure
Run the database container.
```bash
docker-compose -f docker/docker-compose.yml up -d
```

### 2. Configure & Run Backend
The backend aggregates data and serves the API.

```bash
cd backend
cp .env.example .env
# default .env values work out-of-the-box for local dev
dotnet run --project Valora.Api
```
*Verify: Open `http://localhost:5001/api/health` in your browser.*

### 3. Configure & Run Mobile App
The Flutter app is the primary interface for users.

```bash
cd ../apps/flutter_app
cp .env.example .env
# critical: check API_URL in .env (use 10.0.2.2 for Android Emulator)
flutter pub get
flutter run
```

---

## 🏗️ Architecture

Valora follows **Clean Architecture** principles to ensure modularity and testability.

```mermaid
graph TD
    User((User)) -->|Input Address| App[Flutter App]
    Admin((Admin)) -->|Manage Users| AdminApp[Admin Dashboard]

    subgraph "Valora Ecosystem"
        App -->|API Request| API[Valora API (.NET)]
        AdminApp -->|API Request| API

        API -->|Orchestrates| Core[Application Layer]
        Core -->|Defines| Domain[Domain Entities]

        API -->|Persists| DB[(PostgreSQL)]
    end

    subgraph "External Data Sources (Fan-Out)"
        Core -->|Geocoding| PDOK[PDOK Locatieserver]
        Core -->|Demographics| CBS[CBS StatLine]
        Core -->|Amenities| OSM[OpenStreetMap]
        Core -->|Air Quality| Air[Luchtmeetnet]
    end
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

### 2. Listing Lifecycle
Properties (Listings) are only persisted when a user explicitly "saves" or "tracks" them.

1.  **Discovery:** User searches for an address.
2.  **Resolution:** Valora resolves coordinates via PDOK.
3.  **Context:** Valora generates a context report (transient).
4.  **Persistence:** User clicks "Save". Valora stores a `Listing` entity with the context score.

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
- **[Data Flow: Reports](docs/onboarding-data-flow.md)**: Deep dive into the aggregation engine.

---

*Missing documentation? Open an issue or check the `docs/` folder.*
