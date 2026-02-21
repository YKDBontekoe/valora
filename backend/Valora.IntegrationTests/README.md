# Valora Integration Tests

Integration tests for the backend API.

## Notes

- Tests boot the API via `WebApplicationFactory`.
- Database tests run with EF Core InMemory in this environment.
- Focus includes auth, health, map endpoints, and context report endpoint behavior.

## Run

```bash
cd backend
dotnet test Valora.IntegrationTests
```
