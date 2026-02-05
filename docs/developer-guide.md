# Valora Developer Guide

This guide explains the technical architecture and implementation details of Valora.

## Architecture Overview

Valora follows a **Clean Architecture** approach. This ensures separation of concerns, testability, and independence from external frameworks.

### Layers

1.  **Valora.Domain**
    *   **Responsibility**: Defines the core business entities and logic.
    *   **Dependencies**: None.
    *   **Key Classes**: `Listing`, `PriceHistory`, `ApplicationUser`.

2.  **Valora.Application**
    *   **Responsibility**: Defines application use cases, interfaces, and DTOs.
    *   **Dependencies**: `Valora.Domain`.
    *   **Key Interfaces**: `IListingRepository`, `IFundaScraperService`, `IAuthService`.

3.  **Valora.Infrastructure**
    *   **Responsibility**: Implements interfaces defined in the Application layer. interacting with external systems (Database, Funda.nl, Hangfire).
    *   **Dependencies**: `Valora.Application`, `Valora.Domain`.
    *   **Key Components**: `ValoraDbContext` (EF Core), `FundaScraperService`, `TokenService`.

4.  **Valora.Api**
    *   **Responsibility**: The entry point of the application. Handles HTTP requests, dependency injection, and configuration.
    *   **Dependencies**: `Valora.Application`, `Valora.Infrastructure`.
    *   **Key Components**: `Program.cs` (Minimal APIs), `AuthEndpoints`.

### Class Diagram

```mermaid
classDiagram
    class Listing {
        +Guid Id
        +string FundaId
        +string Address
        +decimal? Price
        +List~PriceHistory~ PriceHistories
    }

    class IListingRepository {
        <<interface>>
        +GetByIdAsync(id)
        +GetAllAsync(filter)
    }

    class ListingRepository {
        -ValoraDbContext _context
        +GetByIdAsync(id)
        +GetAllAsync(filter)
    }

    class FundaScraperService {
        -IListingRepository _repo
        -HttpClient _http
        +ScrapeAndStoreAsync()
    }

    ListingRepository ..|> IListingRepository
    FundaScraperService --> IListingRepository
    ListingRepository --> Listing
```

## Onboarding: Data Flow

> **Note:** The detailed data flow guide has been moved to the [Onboarding Guide](onboarding.md).

## API Documentation

The backend exposes a REST API via Minimal APIs in `Valora.Api`.

### Authentication

*   **Register**
    *   `POST /api/auth/register`
    *   Body: `{ "email": "user@example.com", "password": "Password123!" }`
    *   Response: `200 OK`

*   **Login**
    *   `POST /api/auth/login`
    *   Body: `{ "email": "user@example.com", "password": "Password123!" }`
    *   Response: `{ "accessToken": "...", "refreshToken": "...", "expiresIn": 3600 }`

*   **Refresh Token**
    *   `POST /api/auth/refresh`
    *   Body: `{ "refreshToken": "..." }`
    *   Response: `{ "accessToken": "...", "refreshToken": "..." }`

### Listings

*   `GET /api/listings`
    *   Headers: `Authorization: Bearer <token>`
    *   Query Params: `pageIndex`, `pageSize`, `minPrice`, `maxPrice`, `city`
    *   Response: Paged list of listings.

*   `GET /api/listings/{id}`
    *   Headers: `Authorization: Bearer <token>`
    *   Response: Detailed listing DTO.

### Scraper

*   `POST /api/scraper/trigger`
    *   Headers: `Authorization: Bearer <token>`
    *   Triggers the `FundaScraperJob` immediately via Hangfire.

## Configuration

Configuration is managed entirely via **Environment Variables**. There is no `appsettings.json`.

| Variable | Description | Example |
|----------|-------------|---------|
| `DATABASE_URL` | PostgreSQL connection string | `Host=localhost;Database=valora;Username=postgres;Password=postgres` |
| `JWT_SECRET` | Secret for signing tokens | `SuperSecretKeyForDevelopmentOnly123!` |
| `SCRAPER_SEARCH_URLS` | Semicolon-separated Funda URLs | `https://www.funda.nl/koop/amsterdam/` |
| `HANGFIRE_ENABLED` | Enable background jobs | `true` |

## Testing

### Backend

Uses xUnit and Testcontainers for integration testing.

```bash
cd backend
dotnet test
```

**Note**: Docker must be running for integration tests to pass.

### Manual Verification

To manually verify the scraper:
1.  Ensure Backend and Database are running.
2.  Login via Postman or the Frontend to get a token.
3.  Call `POST /api/scraper/trigger` with the token.
4.  Check the logs or `http://localhost:5000/hangfire` to see the job processing.
