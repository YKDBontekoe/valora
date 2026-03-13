# Detailed Guide: API to DB Data Flow

This detailed onboarding guide illustrates the step-by-step journey of a data creation request (e.g., creating a new Workspace) from the moment the HTTP POST request hits the Valora API until the `Workspace` entity is fully validated, processed, and persisted to the PostgreSQL database by Entity Framework Core.

## The Journey of a Write Request

When a user in the Flutter app or the web admin dashboard wants to create a new Workspace, they submit a JSON payload containing the Workspace details. This payload traverses the strictly separated layers of Valora's Clean Architecture.

```mermaid
sequenceDiagram
    participant Client as Client (Flutter / React)
    participant API as Valora.Api (Endpoints)
    participant Filter as ValidationFilter
    participant App as Valora.Application (Service)
    participant Domain as Valora.Domain (Entity)
    participant Repo as Valora.Infrastructure (Repository)
    participant DB as PostgreSQL (EF Core)

    Note over Client, API: 1. Request Arrives
    Client->>API: POST /api/workspaces { "Name": "My Project" }

    Note over API, Filter: 2. Security & Validation
    API->>Filter: Invoke Endpoint Filter
    Filter->>Filter: Enforce DataAnnotations & Anti-XSS
    alt Invalid Data
        Filter-->>Client: 400 Bad Request
    else Valid Data
        Filter-->>API: Pass Context
    end

    Note over API, App: 3. Business Orchestration
    API->>App: IWorkspaceService.CreateWorkspaceAsync(dto)
    App->>Repo: GetUserOwnedWorkspacesCountAsync()
    Repo-->>App: currentCount

    Note over App, Domain: 4. Domain Logic
    App->>Domain: new Workspace { Name, OwnerId... }
    Domain-->>App: workspaceInstance

    Note over App, Repo: 5. Persistence
    App->>Repo: AddAsync(workspaceInstance)
    App->>Repo: LogActivityEventAsync(workspaceCreated)
    App->>Repo: SaveChangesAsync()

    Repo->>DB: BEGIN TRANSACTION; INSERT INTO Workspaces; COMMIT;
    DB-->>Repo: Acknowledge Transaction

    Note over Repo, Client: 6. Response Mapping
    Repo-->>App: Persistence Complete
    App->>App: Map Workspace to WorkspaceDto
    App-->>API: WorkspaceDto
    API-->>Client: 201 Created
```
