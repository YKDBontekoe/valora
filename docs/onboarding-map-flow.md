# Data Flow: Interactive Map Overlays

This guide details the flow of data when a user interacts with the Map tab in the Valora App (e.g., viewing population density heatmaps or amenity clusters).

## High-Level Sequence: Map Overlay Tiles

When a user selects a map metric (like `PopulationDensity`), the map needs to render a colored overlay. Instead of sending raw, complex GeoJSON polygons to the Flutter client (which would cause lag), the server rasterizes the data into lightweight tiles.

```mermaid
sequenceDiagram
    participant App as Flutter App
    participant API as MapEndpoints
    participant Service as MapService
    participant Cache as MemoryCache
    participant External as CBS / PDOK
    participant Rasterizer as OverlayRasterizer

    App->>API: GET /api/map/overlay-tiles { bbox, zoom, metric }
    API->>Service: GetMapOverlayTilesAsync

    %% Cache Strategy
    Service->>Service: Snap BBox to Grid Cells (e.g., 0.01 deg)
    Service->>Cache: Check "MapOverlayTiles_{min}_{max}_{zoom}_{metric}"

    alt Cache Hit
        Cache-->>Service: Cached Tiles
        Service-->>API: Tile Array
        API-->>App: 200 OK (Tiles)
    else Cache Miss
        %% Fetching external data
        Service->>External: GetNeighborhoodOverlaysAsync (WFS GeoJSON)
        External-->>Service: List of Polygons + Metrics

        %% Computation heavy step
        Service->>Rasterizer: RasterizeOverlays(GeoJSON)
        Rasterizer->>Rasterizer: Build Spatial Index
        Rasterizer->>Rasterizer: Point-in-Polygon Check per Cell
        Rasterizer-->>Service: List of Grid Tiles

        Service->>Cache: Set Cache (10 minutes)
        Service-->>API: Tile Array
        API-->>App: 200 OK (Tiles)
    end
```

### Why Rasterize Server-Side?
Rendering complex polygons (thousands of vector points) natively in a Flutter map using `PolygonLayer` severely drops frames. By creating a grid of simple, square "tiles" (points with a size) on the backend, the Flutter app only renders a few hundred low-fidelity squares via a heatmap layer, guaranteeing a smooth 60fps experience on mobile devices.

---

## High-Level Sequence: Amenity Clusters

Similar to overlays, rendering every individual tree, bench, or café from OpenStreetMap can crash the app. Amenities are requested via a bounding box and clustered before reaching the client.

```mermaid
sequenceDiagram
    participant App as Flutter App
    participant API as MapEndpoints
    participant Service as MapService
    participant Overpass as OSM / Overpass API
    participant Clusterer as AmenityClusterer

    App->>API: GET /api/map/amenity-clusters { bbox, zoom, types }
    API->>Service: GetMapAmenityClustersAsync

    Service->>Overpass: Get Amenities in BBox (via OQL query)
    Overpass-->>Service: List of Raw Amenities (Nodes)

    Service->>Clusterer: ClusterAmenities(Amenities, CellSize)
    Clusterer->>Clusterer: Group by Grid Coordinate (O(N))
    Clusterer-->>Service: List of Clusters (Lat/Lon + Count + Types)

    Service-->>API: MapAmenityClusterDto Array
    API-->>App: 200 OK
```

### Why Server-Side Grid Clustering?
Standard distance-based clustering algorithms (like DBSCAN) have O(N^2) complexity, making them too slow for real-time requests with thousands of items.
By using a simple grid mapping approach (where coordinates are bucketed via integer division of the cell size), the algorithm processes in strict O(N) time. The tradeoff is that clusters snap to grid centers rather than true geometric centroids, but the performance gain is worth it.
