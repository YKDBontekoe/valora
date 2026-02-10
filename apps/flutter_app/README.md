# Valora Flutter App

Flutter client for Valora context reports.

## Setup

```bash
cd apps/flutter_app
flutter pub get
flutter run
```

## API Configuration

Create `.env` from `.env.example` and set:

- `API_URL=http://localhost:5001/api` (or your deployment URL)

## Main Screens

- `Report`: generate context reports from link/address
- `Search`: existing listing dataset browsing (legacy support)
- `Saved`: local favorites
- `Settings`
