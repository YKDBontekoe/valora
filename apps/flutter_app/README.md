# Valora Flutter App

Multi-platform client for valora (iOS, Android, Web, Desktop).

## Setup

1. **Install Flutter**: https://docs.flutter.dev/get-started/install

2. **Initialize the project**:
   ```bash
   cd apps/flutter_app
   flutter create --project-name valora_app --org nl.valora --platforms=ios,android,web,macos .
   ```

3. **Add dependencies** (after flutter create):
   ```bash
   flutter pub add http provider
   ```

4. **Run**:
   ```bash
   flutter run
   ```

## API Configuration

The backend API defaults to `http://localhost:5000/api`.

You can configure the API URL in two ways:

1.  **Using a `.env` file (Recommended for local dev):**
    Create a `.env` file in the root of the flutter app (copy from `.env.example`) and set your URL:
    ```
    API_BASE_URL=https://valora-ylpr.onrender.com/api
    ```

2.  **Using `--dart-define` (Recommended for CI/CD):**
    ```bash
    flutter run --dart-define=API_BASE_URL=https://valora-ylpr.onrender.com/api
    ```
