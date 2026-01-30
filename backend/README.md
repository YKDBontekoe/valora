# Valora Backend

The backend service for Valora, built with .NET 10.

## Getting Started

1.  Ensure you have the .NET 10 SDK installed.
2.  Ensure Docker is running (required for integration tests).

## Building

```bash
dotnet build
```

## Testing

The solution includes integration tests that run against a real PostgreSQL database using Testcontainers.

```bash
dotnet test
```

## Project Structure

- **Valora.Api**: The entry point and API layer.
- **Valora.Application**: Business logic and use cases.
- **Valora.Domain**: Domain entities and logic.
- **Valora.Infrastructure**: Database, external services, and implementation details.
- **Valora.IntegrationTests**: Integration tests.
