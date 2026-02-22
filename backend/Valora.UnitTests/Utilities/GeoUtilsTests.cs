using System.Text.Json;
using Valora.Application.Common.Utilities;
using Xunit;

namespace Valora.UnitTests.Utilities;

public class GeoUtilsTests
{
    [Fact]
    public void ParseGeometry_ShouldParsePolygonCorrectly()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 5.0, 52.0 },
                    new[] { 5.1, 52.0 },
                    new[] { 5.1, 52.1 },
                    new[] { 5.0, 52.1 },
                    new[] { 5.0, 52.0 }
                }
            }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        Assert.Single(parsed.Polygons);
        Assert.Single(parsed.Polygons[0]); // One ring (exterior)
        Assert.Equal(5, parsed.Polygons[0][0].Count); // 5 points

        // Check BoundingBox
        Assert.Equal(52.0, parsed.BBox.MinLat);
        Assert.Equal(52.1, parsed.BBox.MaxLat);
        Assert.Equal(5.0, parsed.BBox.MinLon);
        Assert.Equal(5.1, parsed.BBox.MaxLon);
    }

    [Fact]
    public void ParseGeometry_ShouldParseMultiPolygonCorrectly()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "MultiPolygon",
            coordinates = new[] {
                new[] { // Polygon 1
                    new[] {
                        new[] { 5.0, 52.0 },
                        new[] { 5.1, 52.0 },
                        new[] { 5.1, 52.1 },
                        new[] { 5.0, 52.1 },
                        new[] { 5.0, 52.0 }
                    }
                },
                new[] { // Polygon 2
                    new[] {
                        new[] { 5.2, 52.2 },
                        new[] { 5.3, 52.2 },
                        new[] { 5.3, 52.3 },
                        new[] { 5.2, 52.3 },
                        new[] { 5.2, 52.2 }
                    }
                }
            }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        Assert.Equal(2, parsed.Polygons.Count);

        // Check BoundingBox (union of both polygons)
        Assert.Equal(52.0, parsed.BBox.MinLat);
        Assert.Equal(52.3, parsed.BBox.MaxLat);
        Assert.Equal(5.0, parsed.BBox.MinLon);
        Assert.Equal(5.3, parsed.BBox.MaxLon);
    }

    [Fact]
    public void ParseGeometry_ShouldHandleFeatureObject()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Feature",
            geometry = new
            {
                type = "Polygon",
                coordinates = new[] {
                    new[] {
                        new[] { 5.0, 52.0 },
                        new[] { 5.1, 52.0 },
                        new[] { 5.1, 52.1 },
                        new[] { 5.0, 52.1 },
                        new[] { 5.0, 52.0 }
                    }
                }
            },
            properties = new { }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        Assert.Single(parsed.Polygons);
        Assert.Equal(52.0, parsed.BBox.MinLat);
    }

    [Fact]
    public void ParseGeometry_ShouldReturnEmpty_ForInvalidJson()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Point", // Not supported by ParseGeometry currently as we focus on Polygons/MultiPolygons
            coordinates = new[] { 5.0, 52.0 }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        Assert.Empty(parsed.Polygons);
        Assert.Equal(double.MaxValue, parsed.BBox.MinLat); // Default/invalid bbox
    }

    [Fact]
    public void ParseGeometry_ShouldReturnEmpty_ForNullOrNonObject()
    {
        var geoJson = JsonSerializer.SerializeToElement("invalid"); // String instead of object
        var parsed = GeoUtils.ParseGeometry(geoJson);
        Assert.Empty(parsed.Polygons);
    }

    [Fact]
    public void IsPointInPolygon_ParsedGeometry_ShouldReturnTrue_WhenInside()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 10.0, 0.0 },
                    new[] { 10.0, 10.0 },
                    new[] { 0.0, 10.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        Assert.True(GeoUtils.IsPointInPolygon(5.0, 5.0, parsed));
    }

    [Fact]
    public void IsPointInPolygon_ParsedGeometry_ShouldReturnFalse_WhenOutsideBBox()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 10.0, 0.0 },
                    new[] { 10.0, 10.0 },
                    new[] { 0.0, 10.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        // Outside BBox optimization check
        Assert.False(GeoUtils.IsPointInPolygon(15.0, 15.0, parsed));
    }

    [Fact]
    public void IsPointInPolygon_ParsedGeometry_ShouldReturnFalse_WhenInsideBBoxButOutsidePolygon()
    {
        // Triangle inside a square bbox
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 10.0, 0.0 },
                    new[] { 5.0, 10.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        // Point (9.0, 9.0) -> inside bbox, outside triangle
        Assert.False(GeoUtils.IsPointInPolygon(9.0, 9.0, parsed));
    }

    [Fact]
    public void IsPointInPolygon_ParsedGeometry_ShouldReturnFalse_WhenInHole()
    {
        // Square with a hole in the middle
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] { // Exterior
                    new[] { 0.0, 0.0 },
                    new[] { 10.0, 0.0 },
                    new[] { 10.0, 10.0 },
                    new[] { 0.0, 10.0 },
                    new[] { 0.0, 0.0 }
                },
                new[] { // Hole
                    new[] { 4.0, 4.0 },
                    new[] { 6.0, 4.0 },
                    new[] { 6.0, 6.0 },
                    new[] { 4.0, 6.0 },
                    new[] { 4.0, 4.0 }
                }
            }
        });

        var parsed = GeoUtils.ParseGeometry(geoJson);

        Assert.True(GeoUtils.IsPointInPolygon(2.0, 2.0, parsed)); // In exterior, not in hole
        Assert.False(GeoUtils.IsPointInPolygon(5.0, 5.0, parsed)); // In hole
    }

    [Fact]
    public void IsPointInPolygon_JsonElement_ShouldReturnTrue_WhenInside()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 10.0, 0.0 },
                    new[] { 10.0, 10.0 },
                    new[] { 0.0, 10.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        Assert.True(GeoUtils.IsPointInPolygon(5.0, 5.0, geoJson));
    }

    [Fact]
    public void IsPointInPolygon_JsonElement_ShouldReturnFalse_WhenOutside()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 10.0, 0.0 },
                    new[] { 10.0, 10.0 },
                    new[] { 0.0, 10.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        Assert.False(GeoUtils.IsPointInPolygon(15.0, 15.0, geoJson));
    }

    [Fact]
    public void IsPointInPolygon_JsonElement_ShouldHandleFeatureObject()
    {
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Feature",
            geometry = new
            {
                type = "Polygon",
                coordinates = new[] {
                    new[] {
                        new[] { 0.0, 0.0 },
                        new[] { 10.0, 0.0 },
                        new[] { 10.0, 10.0 },
                        new[] { 0.0, 10.0 },
                        new[] { 0.0, 0.0 }
                    }
                }
            },
            properties = new { }
        });

        Assert.True(GeoUtils.IsPointInPolygon(5.0, 5.0, geoJson));
    }
}
