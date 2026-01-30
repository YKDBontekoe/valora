# Valora

House listing scraper for funda.nl.

## Stack

- **Backend**: .NET 10 (Minimal APIs, EF Core, Clean Architecture)
- **Frontend**: Flutter (Web, iOS, Android, Desktop)
- **Database**: PostgreSQL

## Structure

```
valora/
├── apps/flutter_app/     # Flutter app
├── backend/              # .NET 10 API
└── docker/               # Docker Compose
```

## Getting Started

### Backend

```bash
cd backend
dotnet restore
dotnet build
dotnet run --project Valora.Api
```

### Frontend

```bash
cd apps/flutter_app
flutter pub get
flutter run
```

### Database

```bash
docker-compose -f docker/docker-compose.yml up -d
```

## API

Backend runs on `http://localhost:5000` by default.
