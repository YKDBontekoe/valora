# Data Flow: Listing Lifecycle

This guide explains the complete lifecycle of a listing in Valora, from initial discovery via PDOK to detailed context enrichment and database persistence.

## 1. Creation & Upsert (The "Discovery" Phase)

When a user or admin requests a listing by its external ID (e.g., PDOK ID or Funda URL), the system ensures it exists in our database.

### The Flow
1.  **Request**: `GET /api/listings/pdok/{id}`
2.  **External Fetch**: `PdokListingService` queries the PDOK Locatieserver for raw details.
3.  **Upsert Logic**: `ListingService` checks if a listing with this `FundaId` (or external ID) already exists.
    -   **If New**: Creates a new `Listing` entity.
    -   **If Existing**: Updates the existing entity (last seen date, price, etc.).
4.  **Persistence**: Saves to `Listings` table.

### Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant API as Valora.Api
    participant Service as ListingService
    participant Pdok as PdokListingService
    participant Repo as ListingRepository
    participant DB as PostgreSQL

    Client->>API: GET /api/listings/pdok/{id}
    API->>Service: GetPdokListingAsync(id)
    Service->>Pdok: GetListingDetailsAsync(id)
    Pdok-->>Service: ListingDto (Raw Details)

    Service->>Repo: GetByFundaIdAsync(dto.FundaId)
    Repo->>DB: SELECT ... WHERE FundaId = @id
    DB-->>Repo: Existing Entity (or null)

    alt Is New
        Service->>Repo: AddAsync(New Entity)
        Repo->>DB: INSERT INTO Listings ...
    else Is Existing
        Service->>Service: Map Updates (Price, etc.)
        Service->>Repo: UpdateAsync(Existing Entity)
        Repo->>DB: UPDATE Listings ...
    end

    Service-->>API: ListingDto
    API-->>Client: 200 OK
```

## 2. Enrichment (The "Intelligence" Phase)

Once a listing exists, it can be "enriched" with Valora's context data (Social, Safety, Amenities). This is often triggered explicitly or lazily.

### The Flow
1.  **Request**: `POST /api/listings/{id}/enrich`
2.  **Retrieval**: `ListingService` fetches the entity from DB.
3.  **Fan-Out**: `ContextReportService` generates a report for the listing's address (see [Report Data Flow](onboarding-data-flow.md)).
4.  **Mapping**: The complex `ContextReportDto` is mapped to a JSON-serializable `ContextReportModel`.
5.  **Indexing**: Key scores (Composite, Safety) are copied to indexed columns for fast filtering.
6.  **Persistence**: The entity is updated with the JSON blob and scores.

### Sequence Diagram

```mermaid
sequenceDiagram
    participant Client
    participant API as Valora.Api
    participant Service as ListingService
    participant Context as ContextReportService
    participant Repo as ListingRepository
    participant DB as PostgreSQL

    Client->>API: POST /api/listings/{id}/enrich
    API->>Service: EnrichListingAsync(id)

    Service->>Repo: GetByIdAsync(id)
    Repo-->>Service: Listing Entity

    Service->>Context: BuildAsync(Entity.Address)
    note right of Context: Fan-Out to CBS, OSM, PDOK...
    Context-->>Service: ContextReportDto (Full Data)

    Service->>Service: MapToDomain(Report) -> JSON
    Service->>Service: Update Entity Columns (CompositeScore, etc.)

    Service->>Repo: UpdateAsync(Entity)
    Repo->>DB: UPDATE Listings SET ContextReport = JSON, ...
    DB-->>Repo: Success

    Service-->>API: New Composite Score
    API-->>Client: 200 OK
```

## Key Decisions & "Why"

-   **Upsert on Read**: We update the listing on every fetch (`GetPdokListingAsync`) to ensure our price and status data is as fresh as possible without running background scrapers.
-   **JSONB Storage**: The full context report is stored as JSONB. This allows us to evolve the report structure (add new metrics) without painful schema migrations for every single data point.
-   **Indexed Columns**: While we store the full report in JSON, we duplicate the most important scores (Composite, Safety, Social) into standard columns. This enables SQL-native sorting and filtering (`WHERE ContextSafetyScore > 80`) which would be slow or complex with JSON queries.
