# Valora Developer Guide

This guide explains the technical architecture and implementation details of Valora.

## Architecture Overview

Valora follows a **Clean Architecture** approach with the following layers:

- **Valora.Domain**: Enterprise business logic and entities. No external dependencies.
- **Valora.Application**: Application logic, interfaces, and DTOs.
- **Valora.Infrastructure**: Implementation of interfaces (EF Core, Hangfire, Scraping).
- **Valora.Api**: Entry point, Minimal APIs, and Dependency Injection configuration.

## API Documentation

The backend exposes a REST API via Minimal APIs in `Valora.Api`.

### Endpoints

- `GET /api/health`
  - Returns the health status of the API.
  - Response: `{ "status": "healthy", "timestamp": "..." }`

- `GET /api/listings`
  - Returns a list of scraped listings.
  - Response: `ListingDto[]`

- `GET /api/listings/{id}`
  - Returns details for a specific listing.
  - Response: `ListingDto`

- `POST /api/scraper/trigger`
  - Manually triggers the scraping job.
  - Response: `{ "message": "Scraper job queued" }`

- `GET /hangfire`
  - Hangfire Dashboard for monitoring background jobs.

## Scraping Logic

The scraping logic is implemented in `Valora.Infrastructure.Scraping.FundaScraperService`.

### Key Components

- **FundaScraperService**: The main service that coordinates scraping. It iterates through configured search URLs, parses results, and updates the database.
- **FundaHtmlParser**: Parses HTML content to extract listing details.
- **Retry Policy**: Uses **Polly** to handle transient failures (HTTP errors) with exponential backoff.
- **Hangfire**: Schedules and executes the `FundaScraperJob` in the background.

### Configuration (`appsettings.json`)

The scraper is configured via the `Scraper` section in `appsettings.json`:

```json
"Scraper": {
  "SearchUrls": [
    "https://www.funda.nl/zoeken/koop?selected_area=%5B%22amsterdam%22%5D"
  ],
  "DelayBetweenRequestsMs": 2000,
  "MaxRetries": 3,
  "CronExpression": "0 */6 * * *"
}
```

- `SearchUrls`: List of Funda search result URLs to scrape.
- `DelayBetweenRequestsMs`: Delay between HTTP requests to avoid rate limiting.
- `MaxRetries`: Number of retries for failed requests.
- `CronExpression`: Schedule for the background job (Default: every 6 hours).

## Testing

### Backend

Uses xUnit and Testcontainers for integration testing.

```bash
cd backend
dotnet test
```

**Note**: Docker must be running for integration tests to pass.

### Frontend

Uses Flutter test.

```bash
cd apps/flutter_app
flutter test
```
