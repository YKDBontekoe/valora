# Onboarding Guide: Frontend Architecture

This guide explains the frontend architecture of the Valora Flutter application and the Admin Dashboard, including how data flows from the UI to the backend API.

## High-Level Sequence Diagram

The following Mermaid diagram maps out the complete flow of data from the Flutter app to the API and back.

```mermaid
sequenceDiagram
    participant User
    participant FlutterApp as Flutter App (Provider)
    participant ApiClient as ApiClient (Dart)
    participant ValoraApi as Valora API (.NET)

    User->>FlutterApp: Enters location & taps "Get Report"
    FlutterApp->>FlutterApp: Validates input
    FlutterApp->>ApiClient: requestReport("Damrak 1")
    ApiClient->>ValoraApi: POST /api/context/report

    note over ValoraApi: Fan-out fetching occurs here

    ValoraApi-->>ApiClient: Returns ContextReportDto
    ApiClient-->>FlutterApp: Parsed Report Model
    FlutterApp->>FlutterApp: Updates State (notifyListeners)
    FlutterApp-->>User: Renders Report UI
```

## Flutter State Management

We use `Provider` and `ChangeNotifier` to manage state in the Flutter app.

*   **View Models**: Controllers that fetch data from the `ApiClient` and hold the state (loading, error, success).
*   **Widgets**: UI components that observe the state and rebuild when `notifyListeners` is called.

### Example: The Report Screen

```mermaid
graph TD
    A[ReportScreen] -->|observes| B(ReportViewModel)
    B -->|calls| C{ApiClient}
    C -->|fetches from| D((Valora API))
    D -->|returns JSON| C
    C -->|returns Model| B
    B -->|notifyListeners| A
```

## Admin Dashboard

The Admin Dashboard is built with React and Vite. It uses standard React hooks for state and Axios for making API requests to the Valora backend.

## Best Practices

*   **UI/UX Polish**: Always ensure micro-interactions, hover/tap feedback, and smooth transitions are present.
*   **Performance**: Use `Selector` instead of `Consumer` in Flutter to avoid unnecessary widget rebuilds.
*   **Error Handling**: Always display a user-friendly error state when an API call fails.
