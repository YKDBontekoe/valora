# Valora User Guide

## What Valora Does

Valora helps you evaluate a location by generating a context report from public data.

You can paste:

- an address, or
- a location link

Valora resolves the location and returns metrics for neighborhood context.

## Main Flow

1. Sign in.
2. Open `Report` tab.
3. Enter an address or location URL.
4. Select radius.
5. Tap `Generate Report`.

## What You Get

- Location details (resolved address/area)
- Social indicators
- Amenities summary
- Environment signals
- Composite score
- Data source attribution
- Warnings when a source is temporarily unavailable

## Troubleshooting

### Backend not connected

- Ensure backend is running.
- Check: `http://localhost:5001/api/health`

### Report fails

- Confirm you are logged in.
- Try a clear NL address format.
- If partial data appears, check report warnings.
