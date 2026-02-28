# Valora

**Valora is a public-context intelligence platform for residential locations in the Netherlands.**

It helps users understand the "vibe" and statistics of a neighborhood by aggregating data from public sources (CBS, PDOK, OpenStreetMap, Luchtmeetnet) into a unified, explainable context report.

> **Valora is NOT a scraper.** It does not copy listing photos or descriptions. It enriches location data with public context.

---

## Table of Contents
<!-- markdownlint-disable MD051 -->
- [Quick Start](#quick-start-10-minutes)
- [Architecture](#architecture)
- [Key Concepts](#key-concepts)
- [Project Structure](#project-structure)
- [Documentation Index](#documentation-index)
<!-- markdownlint-enable MD051 -->

---

## 🚀 Quick Start (10 Minutes)

Follow these steps to get the entire system running locally. For a detailed walkthrough, see the **[Onboarding Guide](docs/onboarding.md)**.

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
> - Android Emulator: `http://10.0.2.2:5253/api`
> - iOS Simulator / Desktop: `http://localhost:5253/api`
>
> *(Note: Use port `5001` if running backend via Docker)*

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

Valora is built on two core pillars: **Clean Architecture** for code organization and the **Fan-Out/Fan-In Pattern** for data retrieval. This ensures the system is modular, testable, and respects data providers by avoiding scraping.

### System Context

The following diagram shows how Valora fits into the broader ecosystem.

```mermaid
C4Container
    title System Context Diagram

    Person(user, "Mobile User", "A user looking for neighborhood insights")
    Person(admin, "Admin User", "Platform administrator")

    Container_Boundary(valora, "Valora Platform") {
        Container(app, "Flutter App", "Dart/Flutter", "Provides the mobile interface for users")
        Container(web, "Admin Dashboard", "React/Vite", "Provides management tools for admins")
        Container(api, "Valora API", ".NET 10 Minimal API", "Aggregates data and serves reports")
        ContainerDb(db, "Database", "PostgreSQL", "Stores users, logs, and cache")
    }

    System_Ext(pdok, "PDOK Locatieserver", "Geocoding and Map WMS")
    System_Ext(cbs, "CBS Open Data", "Demographics and Stats")
    System_Ext(osm, "OpenStreetMap / Overpass", "Amenities and POIs")
    System_Ext(lucht, "Luchtmeetnet", "Air Quality Data")

    Rel(user, app, "Uses")
    Rel(admin, web, "Uses")
    Rel(app, api, "HTTPS / JSON", "Requests reports")
    Rel(web, api, "HTTPS / JSON", "Manages system")
    Rel(api, db, "EF Core", "Persists data")

    Rel(api, pdok, "HTTPS", "Geocoding")
    Rel(api, cbs, "HTTPS", "Fetch Stats")
    Rel(api, osm, "HTTPS", "Fetch Amenities")
    Rel(api, lucht, "HTTPS", "Fetch Air Quality")
```

For a deeper dive into *why* we built it this way, see **[Architecture Decisions](docs/architecture-decisions.md)**.

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
    API -.->|"Persist (Optional)"| DB[(PostgreSQL)]
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

### 📘 Onboarding & Guides
*Start here to understand how to run and use Valora.*

- **[Onboarding Guide](docs/onboarding.md)** (⭐ Recommended)
    - Detailed, step-by-step setup instructions for Docker, Backend, and Frontend.
    - Troubleshooting for common errors (Database, JWT, API URL).
- **[User Guide](docs/user-guide.md)**: How to use the mobile app to generate reports.
- **[Admin App Guide](apps/admin_page/README.md)**: Setup and features for the admin dashboard.

### 🧩 Architecture & Data Flow
*Understand the internal plumbing and design decisions.*

- **[Architecture Decisions](docs/architecture-decisions.md)**: The "Why" behind Clean Architecture and Fan-Out.
- **[Data Flow: Reading (Fan-Out)](docs/onboarding-data-flow.md)** (Includes Mermaid Diagram):
    - Explains the API request lifecycle from Flutter -> API -> PDOK/CBS/OSM -> Report.
- **[Data Flow: Writing (Persistence)](docs/onboarding-persistence-flow.md)**:
    - Explains User Registration and database transactions.
- **[Data Flow: Batch Jobs](docs/onboarding-batch-job-flow.md)**:
    - Explains background processing (City Ingestion).

### ⚙️ Developer Reference
*Technical details for contributors.*

- **[API Reference](docs/api-reference.md)**: Complete list of endpoints, request bodies, and authentication methods.
- **[Developer Guide](docs/developer-guide.md)**: Coding standards, patterns, and testing protocols.

---

*Missing documentation? Open an issue or check the `docs/` folder.*
