# Onboarding Guide: Data Flow from API Request to Database Persistence

This guide traces the exact path a write request takes in Valora, starting from an external client (e.g., the Flutter App or Admin Dashboard) hitting an API endpoint, moving through our Clean Architecture layers, and finally being persisted in the PostgreSQL database via Entity Framework Core.

To understand this flow, we will use the creation of a **Workspace** as our example. A Workspace allows users to save and group context reports.

## High-Level Sequence Diagram

The following Mermaid diagram maps out the complete "Write" lifecycle for `POST /api/workspaces`.

```mermaid
sequenceDiagram
    participant Client as Client App (Flutter/Web)
    participant API as Valora.Api (Endpoints)
    participant App as Valora.Application (Service)
    participant Domain as Valora.Domain (Entity)
    participant Repo as Valora.Infrastructure (Repository)
    participant DB as PostgreSQL

    Client->>API: POST /api/workspaces { "name": "My Favorites" }

    %% API Layer Validation
    API->>API: Validate Request (ValidationFilter)

    %% Delegate to Application Layer
    API->>App: IWorkspaceService.CreateWorkspaceAsync

    %% Business Logic
    App->>Domain: Create Workspace Entity (Public Setters)
    Domain-->>App: Workspace Instance

    %% Persistence Call
    App->>Repo: AddAsync(Workspace)
    App->>Repo: SaveChangesAsync()

    %% Database Transaction
    Repo->>DB: INSERT INTO "Workspaces"
    DB-->>Repo: Acknowledge Insert

    %% Completion
    Repo-->>App: Completed
    App-->>API: Map to WorkspaceDto
    API-->>Client: 201 Created (WorkspaceDto)
```

## Step-by-Step Breakdown

### 1. The Request Arrives (API Layer)
* **Location:** `Valora.Api/Endpoints/WorkspaceEndpoints.cs`
* The request is received by a Minimal API endpoint (`MapPost("/")`). Before the core logic is hit, it passes through the `ValidationFilter`.
* The `ValidationFilter` enforces DataAnnotations and checks for XSS strings. If validation fails, it returns a `400 Bad Request`.
* The endpoint extracts the DTO and invokes `IWorkspaceService.CreateWorkspaceAsync`.

### 2. Service Logic (Application Layer)
* **Location:** `Valora.Application/Services/WorkspaceService.cs`
* The application layer service performs necessary business checks (e.g., verifying the user hasn't exceeded the 10-workspace limit).
* The service instantiates a new `Workspace` entity.

### 3. Domain Entity (Domain Layer)
* **Location:** `Valora.Domain/Entities/Workspace.cs` & `Valora.Domain/Common/BaseEntity.cs`
* The `Workspace` entity is initialized using public setters (e.g., `Name`, `Description`, `OwnerId`, `Members`).
* The base properties like `Id` (GUID) and `CreatedAt` (UTC Now) are automatically assigned default values within the `BaseEntity` constructor.

### 4. Database Persistence (Infrastructure Layer)
* **Location:** `Valora.Infrastructure/Persistence/Repositories/WorkspaceRepository.cs`
* The Application layer calls `_repository.AddAsync(workspace)` to track the entity, and then explicitly calls `_repository.SaveChangesAsync()`.
* Entity Framework Core translates the tracked entity state into an SQL `INSERT` statement and executes it against PostgreSQL.

### 5. Returning the Response
* Once `SaveChangesAsync` is complete, the repository returns control to the Application service.
* The Application layer maps the saved Entity into a `WorkspaceDto` to avoid leaking domain objects.
* The API layer returns a `201 Created` response to the client.

## Summary

This architecture guarantees that the database schema is decoupled from the API contract. DTOs are for external communication, Entities are for internal business logic, and EF Core Configurations handle the mapping to database tables.