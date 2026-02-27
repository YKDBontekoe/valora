# Architecture Decisions

This document captures the key architectural decisions (ADRs) that shape Valora's design, explaining the "Why" behind the "What".

## 1. Fan-Out / Fan-In Aggregation (No Scraper Policy)

**Decision:** Valora does not scrape listing websites (like Funda) for data. Instead, it aggregates public data (CBS, PDOK, OSM) in real-time or near real-time.

**Why:**
-   **Legal & Ethical:** Scraping listing data violates terms of service and copyright. Valora adds value through *context* (neighborhood stats), not by duplicating listing content.
-   **Freshness:** Real-time queries to sources like Luchtmeetnet (Air Quality) ensure the data is current.
-   **Scalability:** Storing pre-computed reports for every address in the Netherlands is storage-intensive and difficult to keep consistent. "Fan-Out" allows us to compute only what is requested.

**Trade-offs:**
-   **Latency:** The response time is bound by the slowest external API. We mitigate this with aggressive caching (24h) and timeouts.
-   **Complexity:** Parallel error handling (Fan-In) is more complex than a simple DB lookup.

## 2. Clean Architecture

**Decision:** The solution is structured into four concentric layers: `Domain` (Center), `Application`, `Infrastructure`, and `Api` (Outer).

**Why:**
-   **Dependency Rule:** Dependencies only point inwards. The `Domain` knows nothing about the database or the web.
-   **Testability:** Business logic in `Application` and `Domain` can be unit tested without spinning up a database or web server.
-   **Flexibility:** We can swap the database (e.g., from Postgres to SQL Server) or the API framework (Minimal APIs to Controllers) without touching the core business rules.

## 3. CQRS-Lite (Repository Pattern)

**Decision:** Repositories explicitly separate "Read" and "Write" operations.

**Why:**
-   **Performance (Reads):** Read methods (e.g., `GetContextReport`) use `.AsNoTracking()` and project directly to DTOs using `.Select()`. This bypasses the EF Core Change Tracker, reducing memory overhead and execution time.
-   **Consistency (Writes):** Write methods use standard tracked entities to ensure domain invariants are enforced before `SaveChangesAsync`.

**Trade-offs:**
-   **Boilerplate:** Requires separate DTOs for reading vs. Domain Entities for writing.

## 4. Identity Abstraction

**Decision:** The Application layer depends on `IIdentityService`, not on `Microsoft.AspNetCore.Identity`.

**Why:**
-   **Decoupling:** `Valora.Application` should not depend on ASP.NET Core specific libraries. This keeps the core logic framework-agnostic.
-   **Testing:** We can easily mock `IIdentityService` in unit tests, whereas mocking the concrete `UserManager<T>` class is notoriously difficult.

## 5. Batch Job Processing

**Decision:** Long-running tasks (like City Ingestion) are offloaded to background services (`BatchJobExecutor`) and tracked via a `BatchJob` entity.

**Why:**
-   **Resilience:** HTTP requests should be short. Offloading prevents timeouts for operations that take minutes (e.g., ingesting 500 neighborhoods).
-   **Visibility:** Storing job status in the DB allows admins to track progress and debug failures via the Admin Dashboard.
-   **Isolation:** The `BatchJobExecutor` runs in its own scope, preventing memory leaks or context issues from affecting the main API threads.
