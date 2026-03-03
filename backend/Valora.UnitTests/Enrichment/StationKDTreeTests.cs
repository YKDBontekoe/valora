using System.Collections.Generic;
using Valora.Domain.Common;
using Valora.Infrastructure.Services.AppServices.Utilities;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class StationKDTreeTests
{
    [Fact]
    public void FindNearest_WithEmptyList_ReturnsNull()
    {
        var tree = new StationKDTree(new List<(string, string, double, double)>());
        var result = tree.FindNearest(52.0, 4.0);
        Assert.Null(result);
    }

    [Fact]
    public void FindNearest_ReturnsExactMatch()
    {
        var stations = new List<(string Id, string Name, double Lat, double Lon)>
        {
            ("S1", "Station 1", 52.0, 4.0),
            ("S2", "Station 2", 53.0, 5.0)
        };
        var tree = new StationKDTree(stations);

        var result = tree.FindNearest(52.0, 4.0);

        Assert.NotNull(result);
        Assert.Equal("S1", result!.Value.Id);
        Assert.Equal(0, result.Value.DistanceMeters, 0.001);
    }

    [Fact]
    public void FindNearest_FindsClosestStation()
    {
        var stations = new List<(string Id, string Name, double Lat, double Lon)>
        {
            ("S1", "Amsterdam", 52.3676, 4.9041),
            ("S2", "Rotterdam", 51.9225, 4.47917),
            ("S3", "Utrecht", 52.0907, 5.1214)
        };
        var tree = new StationKDTree(stations);

        // Point closer to Utrecht
        var result = tree.FindNearest(52.1, 5.1);

        Assert.NotNull(result);
        Assert.Equal("S3", result!.Value.Id);
    }
}
