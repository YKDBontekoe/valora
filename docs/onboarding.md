# Valora Onboarding Guide

Welcome to Valora! This guide is designed to help you understand the system's data flow and get you productive within 10 minutes.

## Data Flow Walkthrough

Understanding how data moves through the system is key to contributing effectively. We have two main data flows: **Reading Listings** (API) and **Scraping Listings** (Background Job).

### 1. The Read Flow: `GET /api/listings`

When a user opens the app, they request a list of real estate listings.

**Flow:** `Client` -> `API` -> `Repository` -> `Database`

```mermaid
sequenceDiagram
    participant Client as Flutter App
    participant API as Program.cs (Minimal API)
    participant Repo as ListingRepository
    participant DB as PostgreSQL

    Client->>API: GET /api/listings?city=Amsterdam
    Note right of Client: Includes JWT Token

    API->>API: Validate Token & Filter
    API->>Repo: GetAllAsync(filter)

    Repo->>DB: Execute Query (EF Core)
    Note right of Repo: Filters applied in SQL
    DB-->>Repo: Returns Entities

    Repo-->>API: Returns PaginatedList<Listing>
    API-->>Client: 200 OK (JSON)
```

**Key Points:**
- **Entry Point:** Defined in `backend/Valora.Api/Program.cs`.
- **Logic:** The API layer is thin. It validates the request and delegates to `ListingRepository`.
- **Data Access:** `ListingRepository` (in `Infrastructure`) uses Entity Framework Core to query PostgreSQL. We use `IQueryable` to ensure filtering happens in the database, not in memory.

---

### 2. The Scraper Flow: Data Ingestion

Valora populates its database by scraping Funda.nl. This happens in the background.

**Flow:** `Job` -> `Service` -> `Funda` -> `Parser` -> `Database`

```mermaid
sequenceDiagram
    participant Job as FundaScraperJob
    participant Service as FundaScraperService
    participant API as FundaApiClient
    participant Parser as FundaNuxtJsonParser
    participant DB as PostgreSQL

    Job->>Service: ScrapeAndStoreAsync()

    loop For each Search URL
        Service->>API: SearchBuyAsync() (API Search)
        API-->>Service: List<FundaApiListing>

        Service->>DB: GetByFundaIdsAsync() (Batch Check)

        loop For each Listing
            Service->>API: GetListingSummaryAsync()
            Service->>API: GetListingDetailsAsync()

            API->>API: Get HTML
            API->>Parser: Parse(Nuxt JSON)
            Note right of Parser: Uses BFS to find data
            Parser-->>API: Rich Data

            API-->>Service: Enriched Listing

            alt New Listing
                Service->>DB: AddAsync()
            else Existing
                Service->>DB: UpdateAsync()
            end
        end
    end
```

**Key Components:**
- **`FundaScraperJob`**: The Hangfire job that triggers the process.
- **`FundaScraperService`**: The conductor. It coordinates fetching search results, enriching them with details, and saving them.
- **`FundaApiClient`**: Handles all HTTP communication with Funda. It mimics a browser and handles rate limiting.
- **`FundaNuxtJsonParser`**: A robust parser that extracts data from the specific Vue/Nuxt JSON structure embedded in Funda's HTML.

## Where to Start?

1.  **Run the App**: Follow the "Quick Start" in `README.md`.
2.  **Explore the API**: Look at `backend/Valora.Api/Program.cs` to see all available endpoints.
3.  **Debug the Scraper**: Trigger a manual scrape via `POST /api/scraper/trigger` and watch the logs.
