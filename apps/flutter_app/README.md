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

The app reads API configuration from environment files:

1. Create `.env` from `.env.example`.
2. Set `API_URL` to your backend base URL (for example: `http://localhost:5000/api`).

If `API_URL` is missing, the app falls back to `http://localhost:5000/api` and shows a startup warning banner.
