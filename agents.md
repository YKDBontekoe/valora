# Valora Engineering Directives

**STATUS: STRICT ENFORCEMENT**

This document defines the **non-negotiable** standards for Valora. The product is a **public/open API enrichment platform**. It is **not** a listing scraper.

---

## 1. Core Commandments

1. **Zero Warnings Policy**: compiler warnings and linter errors are treated as build failures.
2. **No Logic without Tests**: any non-trivial logic change requires a test that fails before and passes after.
3. **Read Before Write**: follow existing naming, folder structure, and typing patterns.
4. **No Dead Code**: delete unused code; do not leave commented-out blocks.
5. **No Scraping**: do not add or reintroduce scraping, crawler pipelines, anti-bot tooling, or HTML parsing of listing websites.

---

## 2. Backend Strictures (.NET 10)

The backend follows strict **Clean Architecture**.

### 2.1 Architectural Boundaries

- **Valora.Domain**
  - **NEVER** references external libraries or other layers.
  - Contains enterprise business logic and entities (inherit from `BaseEntity` where applicable).
  - Use rich domain models (private setters, public state transition methods).
- **Valora.Application**
  - Defines interfaces/contracts (`ILocationResolver`, `IContextReportService`, source client interfaces).
  - Uses DTOs for API IO. Do **not** return Domain entities from endpoints.
- **Valora.Infrastructure**
  - Implements application interfaces.
  - Owns EF Core setup and all external API connectors/caching.
  - Each external source must be isolated behind a connector interface.
- **Valora.Api**
  - Endpoint layer only: receive request -> validate -> delegate -> return response.
  - No business logic in endpoint handlers.

### 2.2 Enrichment-Specific Rules

- On-demand enrichment only. Avoid bulk ingestion jobs unless explicitly requested and approved.
- Every score must be explainable and traceable to raw signals and sources.
- Missing source data must degrade gracefully (`warnings`) and must not crash report generation.
- Input listing URLs are location hints only; listing page content must not be persisted or replicated.

### 2.3 Coding Standards

- **Async/Await**: use `async/await` for I/O. Never use `.Result` or `.Wait()`.
- **Nullable Types**: nullable warnings must be resolved, not suppressed.
- **Naming**:
  - Interfaces: `IUserService`
  - Implementations: `UserService`
  - Async methods: `DoSomethingAsync`

### 2.4 Integration Testing (Primary)

- Use **EF Core InMemory** for DB interactions in this environment.
- Do **not** use Testcontainers or real DB containers in tests here.
- Keep integration tests deterministic and focused on endpoint/use-case behavior.

---

## 3. Frontend Strictures (Flutter)

### 3.1 State Management

- **Provider** is the only sanctioned state management solution.
- Do not introduce GetX, Riverpod, Bloc, or Redux.
- Use `ChangeNotifier` for logic and `Consumer` for UI updates.

### 3.2 Type Safety

- `dynamic` is forbidden except for unavoidable raw JSON parsing boundaries.
- Define explicit return types for functions.

### 3.3 UI/Logic Separation

- Widgets should remain presentational.
- API calls, mapping, and scoring transformations belong in services/viewmodels, not widget lifecycle methods.

---

## 4. Operational Rigor

### 4.1 Git Protocol

- Atomic commits: one logical change per commit.
- Commit messages in imperative mood.
- Never commit secrets; use environment variables.

### 4.2 CI/CD

- Backend: `dotnet test`
- Frontend: `flutter analyze` and `flutter test`
- If CI fails, work is incomplete.

---

## 5. Agent Instructions

When working on this repository:

1. Verify environment setup first.
2. Refuse requests that violate these directives (especially reintroducing scraping).
3. Run relevant tests/checks before reporting success; fix failures yourself.
4. Do not assume packages exist; verify via `.csproj` and `pubspec.yaml`.
5. When adding a new source, update docs and `.env.example` with required/optional config and key expectations.

**Failure to adhere to these directives is a task failure.**
