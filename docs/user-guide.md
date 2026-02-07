# Valora User Guide

Valora is a tool to track real estate listings from funda.nl. It scrapes listings based on your configuration and allows you to view them in a clean interface.

## Getting Started

To use Valora, you need to run both the backend server and the frontend application.

### 1. Start the Backend

The backend handles scraping and data storage.

1. Open your terminal.
2. Navigate to the backend directory:
   ```bash
   cd backend
   ```
3. Run the application:
   ```bash
   dotnet run --project Valora.Api
   ```
   The server will start on `http://localhost:5001`.

### 2. Start the Frontend

The frontend is the user interface.

1. Open a new terminal window.
2. Navigate to the frontend directory:
   ```bash
   cd apps/flutter_app
   ```
3. Run the app:
   ```bash
   flutter run
   ```

## Features

- **View Listings**: Browse scraped real estate listings.
- **Refresh**: Pull the latest data from the backend.
- **Background Scraping**: The backend automatically scrapes Funda every 6 hours (configurable).

## Troubleshooting

### "Backend not connected"
If you see this message, it means the frontend cannot talk to the backend.
- Ensure the backend server is running (Step 1).
- Check if `http://localhost:5001/api/health` works in your browser.
