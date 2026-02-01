# Valora Engineering Directives

**STATUS: STRICT ENFORCEMENT**

This document defines the **non-negotiable** standards for the Valora repository. Agents and contributors must adhere to these rules without exception. Violations will result in rejected changes or immediate reverts.

---

## 1. Core Commandments

1.  **Zero Warnings Policy**: The codebase must be free of compiler warnings and linter errors. Treat every warning as a build failure.
2.  **No Logic without Tests**: If you write code, you **MUST** write a test that fails without it and passes with it.
3.  **Read Before Write**: You **MUST** analyze existing patterns (naming, folder structure, typing) before creating new files. Consistency > Creativity.
4.  **No Dead Code**: Do not comment out code. Delete it. Git history is for recovery, not the codebase.

---

## 2. Backend Strictures (.NET 10)

The backend adheres to a strict **Clean Architecture**.

### 2.1 Architectural Boundaries

*   **Valora.Domain**:
    *   **NEVER** reference external libraries or other layers.
    *   **MUST** contain all enterprise business logic.
    *   **MUST** inherit entities from `BaseEntity`.
    *   **MUST** use rich domain models (private setters, public methods for state mutation).
*   **Valora.Application**:
    *   **MUST** define interfaces (`IListingRepository`) but **NEVER** implement data access.
    *   **MUST** rely entirely on DTOs for input/output. **NEVER** return Domain Entities directly from the API.
*   **Valora.Infrastructure**:
    *   **MUST** implement interfaces defined in Application.
    *   **MUST** own all EF Core / Hangfire / External Service configurations.
*   **Valora.Api**:
    *   **MUST** remain "dumb". No business logic in Controllers/Endpoints.
    *   **Function**: Receive Request -> Validate -> Delegate to Application -> Return Response.

### 2.2 Coding Standards

*   **Async/Await**: **ALWAYS** use `async/await` for I/O. **NEVER** use `.Result` or `.Wait()`.
*   **Nullable Types**: Nullable Reference Types are enabled. **NEVER** ignore nullability warnings (CS8600, etc.).
*   **Naming**:
    *   Interfaces: `IUserService`
    *   Implementations: `UserService`
    *   Async Methods: `DoSomethingAsync`

### 2.3 Integration Testing (Primary)

*   **Mechanism**: **InMemory** (EF Core InMemory) is the **ONLY** acceptable way to test database interactions in this environment.
*   **Constraint**: **NEVER** use real database containers or `Testcontainers` due to environment limitations.
*   **Fixture**: Configure `TestDatabaseFixture` to use `UseInMemoryDatabase`.

---

## 3. Frontend Strictures (Flutter)

### 3.1 State Management

*   **Standard**: **Provider** is the **ONLY** sanctioned state management solution.
*   **Constraint**: **NEVER** introduce GetX, Riverpod, Bloc, or Redux.
*   **Pattern**: Use `ChangeNotifier` for logic and `Consumer` for UI updates.

### 3.2 Type Safety

*   **Forbidden**: The `dynamic` type is strictly **FORBIDDEN** except when absolutely unavoidable (e.g., raw JSON parsing).
*   **Strictness**: `explicit-function-return-types` is implied. Define return types for all functions.

### 3.3 UI & Logic Separation

*   **Widgets**: Must be purely presentational.
*   **Logic**: Complex logic (API calls, data transformation) **MUST** move to a Service or ViewModel (ChangeNotifier). **NEVER** perform HTTP calls directly inside a Widget's `build` method or `initState`.

---

## 4. Operational Rigor

### 4.1 Git Protocol

*   **Atomic Commits**: One logical change per commit.
*   **Message Format**: Imperative mood.
    *   *Good*: "Add integration test for ListingService"
    *   *Bad*: "Added tests" or "Fixing bug"
*   **Secrets**: **NEVER** commit API keys, connection strings, or secrets. Use Environment Variables.

### 4.2 CI/CD

*   **Backend**: `dotnet test` (Validation against Docker is mandatory).
*   **Frontend**: `flutter analyze` and `flutter test`.
*   **Rule**: If CI fails, the work is incomplete.

---

## 5. Agent Instructions

When you are working on this repo:

1.  **Verify Environment**: ensure that the environment is set up correctly.
2.  **Strict Compliance**: If a user asks for a quick hack that violates these rules, **REFUSE** and explain why.
3.  **Self-Correction**: Run tests *before* reporting success. If tests fail, fix them. Do not ask the user to fix your broken code.
4.  **No Assumptions**: Do not assume a library exists. Check `.csproj` or `pubspec.yaml` first.

**Failure to adhere to these directives is a failure of the task.**