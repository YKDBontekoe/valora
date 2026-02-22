using System.Text.Json;
using Valora.Application.Common.Utilities;
using Xunit;

namespace Valora.UnitTests.Utilities;

public class GeoUtilsTests
{
    [Theory]
    [InlineData("POINT(4.895 52.370)", 4.895, 52.370)]
    [InlineData("POINT(0 0)", 0, 0)]
    [InlineData("POINT(-10.5 20.1)", -10.5, 20.1)]
    public void TryParseWktPoint_ReturnsCoordinates_ForValidInput(string input, double expectedX, double expectedY)
    {
        var result = GeoUtils.TryParseWktPoint(input);
        Assert.NotNull(result);
        Assert.Equal(expectedX, result.Value.X);
        Assert.Equal(expectedY, result.Value.Y);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INVALID")]
    [InlineData("POINT(1 2")] // Missing closing parenthesis
    [InlineData("POINT 1 2)")] // Missing opening parenthesis
    [InlineData("POINT(1)")] // Missing Y coordinate
    [InlineData("POINT(1 2 3)")] // Too many coordinates
    [InlineData("POINT(foo bar)")] // Non-numeric
    public void TryParseWktPoint_ReturnsNull_ForInvalidInput(string? input)
    {
        var result = GeoUtils.TryParseWktPoint(input);
        Assert.Null(result);
    }

    [Fact]
    public void IsPointInPolygon_ReturnsTrue_ForSimplePolygon()
    {
        // A simple square (0,0) to (1,1)
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 1.0, 0.0 },
                    new[] { 1.0, 1.0 },
                    new[] { 0.0, 1.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        // Test point inside
        Assert.True(GeoUtils.IsPointInPolygon(0.5, 0.5, geoJson));

        // Test point outside (outside bbox)
        Assert.False(GeoUtils.IsPointInPolygon(2.0, 0.5, geoJson));

        // Test point outside (inside bbox, but outside polygon - e.g. diagonal cut would be better for this,
        // but bbox optimization catches the obvious ones)
    }

    [Fact]
    public void IsPointInPolygon_ReturnsTrue_ForMultiPolygon()
    {
        // Two squares: (0,0)-(1,1) and (2,2)-(3,3)
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "MultiPolygon",
            coordinates = new[] {
                new[] { // Poly 1
                    new[] {
                        new[] { 0.0, 0.0 },
                        new[] { 1.0, 0.0 },
                        new[] { 1.0, 1.0 },
                        new[] { 0.0, 1.0 },
                        new[] { 0.0, 0.0 }
                    }
                },
                new[] { // Poly 2
                    new[] {
                        new[] { 2.0, 2.0 },
                        new[] { 3.0, 2.0 },
                        new[] { 3.0, 3.0 },
                        new[] { 2.0, 3.0 },
                        new[] { 2.0, 2.0 }
                    }
                }
            }
        });

        // Inside Poly 1
        Assert.True(GeoUtils.IsPointInPolygon(0.5, 0.5, geoJson));
        // Inside Poly 2
        Assert.True(GeoUtils.IsPointInPolygon(2.5, 2.5, geoJson));
        // Outside
        Assert.False(GeoUtils.IsPointInPolygon(1.5, 1.5, geoJson));
    }

    [Fact]
    public void IsPointInPolygon_ReturnsTrue_ForFeatureWithPolygon()
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
                        new[] { 1.0, 0.0 },
                        new[] { 1.0, 1.0 },
                        new[] { 0.0, 1.0 },
                        new[] { 0.0, 0.0 }
                    }
                }
            },
            properties = new { name = "Test Feature" }
        });

        Assert.True(GeoUtils.IsPointInPolygon(0.5, 0.5, geoJson));
    }

    [Fact]
    public void IsPointInPolygon_ReturnsFalse_ForInvalidJson()
    {
        var invalidJson = JsonSerializer.SerializeToElement(new { notType = "Foo" });
        Assert.False(GeoUtils.IsPointInPolygon(0, 0, invalidJson));

        var featureWithoutGeometry = JsonSerializer.SerializeToElement(new { type = "Feature", properties = new { } });
        Assert.False(GeoUtils.IsPointInPolygon(0, 0, featureWithoutGeometry));

        var wrongType = JsonSerializer.SerializeToElement(new { type = "Point" });
        Assert.False(GeoUtils.IsPointInPolygon(0, 0, wrongType));
    }

    [Fact]
    public void IsPointInPolygon_RespectsHoles()
    {
        // A square (0,0)-(3,3) with a hole (1,1)-(2,2)
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                // Exterior Ring
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 3.0, 0.0 },
                    new[] { 3.0, 3.0 },
                    new[] { 0.0, 3.0 },
                    new[] { 0.0, 0.0 }
                },
                // Interior Ring (Hole) - typically wound opposite, but winding doesn't matter for ray casting
                new[] {
                    new[] { 1.0, 1.0 },
                    new[] { 2.0, 1.0 },
                    new[] { 2.0, 2.0 },
                    new[] { 1.0, 2.0 },
                    new[] { 1.0, 1.0 }
                }
            }
        });

        // Inside polygon, outside hole
        Assert.True(GeoUtils.IsPointInPolygon(0.5, 0.5, geoJson));

        // Inside hole -> Not in polygon
        Assert.False(GeoUtils.IsPointInPolygon(1.5, 1.5, geoJson));
    }

    [Fact]
    public void IsPointInPolygon_HandlesMalformedCoordinates()
    {
        // Polygon with point having < 2 coordinates (should be skipped)
        var geoJson = JsonSerializer.SerializeToElement(new
        {
            type = "Polygon",
            coordinates = new[] {
                new[] {
                    new[] { 0.0, 0.0 },
                    new[] { 1.0 }, // Malformed point
                    new[] { 1.0, 1.0 },
                    new[] { 0.0, 1.0 },
                    new[] { 0.0, 0.0 }
                }
            }
        });

        // The malformed point is skipped, remaining points form a triangle (0,0)-(1,1)-(0,1)-(0,0)
        // Vertices: (0,0), (1,1), (0,1). This forms a triangle in the upper-left of unit square (y >= x).
        // Let's test a point clearly inside the valid part.
        // Point (0.1, 0.9) => lat=0.1 (y), lon=0.9 (x). 0.1 < 0.9 => y < x => Outside!
        // Point (0.9, 0.1) => lat=0.9 (y), lon=0.1 (x). 0.9 > 0.1 => y > x => Inside.
        Assert.True(GeoUtils.IsPointInPolygon(0.9, 0.1, geoJson));
    }
}
