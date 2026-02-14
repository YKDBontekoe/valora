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

## Property Photos

- On property open, the app uses Kadaster/PDOK luchtfoto imagery (`Actueel_orthoHR`) centered on the property coordinates.
- This integration uses only public/open data and requires no API key.
- If coordinates are missing, Valora keeps the listing image placeholders.

## Android Release Signing

To configure release signing for Android:

1. Generate a keystore file.
2. Create a file named `key.properties` in the `apps/flutter_app/android` directory.
3. Add the following properties to `key.properties`:
   ```properties
   storePassword=<your-store-password>
   keyPassword=<your-key-password>
   keyAlias=<your-key-alias>
   storeFile=<location-of-keystore-file>
   ```
   Note: `storeFile` should be a path relative to `apps/flutter_app/android/app`. For example, if the keystore is in `apps/flutter_app/android/app/upload-keystore.jks`, use `storeFile=upload-keystore.jks`.

The `key.properties` file is excluded from version control to keep your credentials secure.
