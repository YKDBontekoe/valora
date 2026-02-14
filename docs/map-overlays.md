# Map Overlay Metrics Contract

## Endpoint

`GET /api/map/overlays?minLat={...}&minLon={...}&maxLat={...}&maxLon={...}&metric={MapOverlayMetric}`

## PricePerSquareMeter behavior

When `metric=PricePerSquareMeter`, Valora computes values per neighborhood overlay (not a single bbox-wide average):

1. Overlay polygons are fetched from CBS/PDOK via `ICbsGeoClient`.
2. Listings inside the requested bbox are matched to neighborhoods using this fallback chain:
   - **Preferred:** listing CBS neighborhood code in listing feature metadata (`buurtcode`, `neighborhoodCode`, `neighbourhoodCode`, `cbsBuurtCode`).
   - **Fallback:** point-in-polygon matching from listing `(longitude, latitude)` into overlay geometry.
3. Per neighborhood, both central-tendency metrics are calculated from listing €/m² samples:
   - **Primary:** median €/m² (`MetricValue`)
   - **Secondary:** mean €/m² (`SecondaryMetricValue`)

## Sparse data handling

Price overlays are explicit about sample quality:

- `SampleSize`: number of matched listings in the neighborhood.
- `HasSufficientData`: `true` when sample size is at least 3 listings.
- `DisplayValue`:
  - `Median: € ... / m² (N listings)` for sufficient samples.
  - `Low confidence (N listings): median € ... / m²` when fewer than 3 listings are available.
  - `Insufficient data` when no listings can be mapped.

No synthetic zero-value display is used for no-data neighborhoods.

## Response fields (MapOverlayDto)

- `id`: neighborhood code.
- `name`: neighborhood name.
- `metricName`: metric identifier.
- `metricValue`: primary numeric value (median for `PricePerSquareMeter`).
- `displayValue`: primary user-facing string including confidence text.
- `geoJson`: raw feature geometry/payload.
- `secondaryMetricValue` *(optional)*: backup/secondary numeric value (mean for `PricePerSquareMeter`).
- `secondaryDisplayValue` *(optional)*: user-facing text for secondary metric.
- `sampleSize` *(optional)*: matched listing count.
- `hasSufficientData`: sample confidence boolean.
