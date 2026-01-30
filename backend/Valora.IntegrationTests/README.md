# Valora Integration Tests

This project contains integration tests for the Valora backend API.

## Requirements

- **Docker**: These tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a PostgreSQL database in a Docker container. Ensure Docker is installed and running on your machine.

## Running Tests

Run the tests using the .NET CLI:

```bash
dotnet test
```

## Structure

- **IntegrationTestWebAppFactory**: bootstraps the API in-memory and configures the Testcontainer.
- **BaseIntegrationTest**: handles the test lifecycle (creating/deleting the DB).
- **ListingTests**: contains tests for the listing endpoints.
