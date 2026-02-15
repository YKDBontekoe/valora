using Valora.Application.Common.Mappings;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Mappings;

public class ContextMapperTests
{
    [Fact]
    public void NeighborhoodStats_Mapping_ShouldWork()
    {
        var entity = new CbsNeighborhoodStats
        {
            RegionCode = "R1",
            Residents = 100,
            RetrievedAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();

        Assert.Equal("R1", dto.RegionCode);
        Assert.Equal(100, dto.Residents);

        var backToEntity = dto.ToEntity("D1", DateTimeOffset.UtcNow.AddDays(1));
        Assert.Equal("R1", backToEntity.RegionCode);
        Assert.Equal("D1", backToEntity.DatasetId);
    }

    [Fact]
    public void CrimeStats_Mapping_ShouldWork()
    {
        var entity = new CbsCrimeStats
        {
            RegionCode = "R1",
            TotalCrimesPer1000 = 50,
            RetrievedAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();
        Assert.Equal(50, dto.TotalCrimesPer1000);

        var backToEntity = dto.ToEntity("D1", DateTimeOffset.UtcNow.AddDays(1), "R1");
        Assert.Equal(50, backToEntity.TotalCrimesPer1000);
    }

    [Fact]
    public void AirQuality_Mapping_ShouldWork()
    {
        var entity = new AirQualitySnapshot
        {
            StationId = "S1",
            Pm25 = 10.5,
            RetrievedAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();
        Assert.Equal(10.5, dto.Pm25);

        var backToEntity = dto.ToEntity(DateTimeOffset.UtcNow.AddDays(1));
        Assert.Equal(10.5, backToEntity.Pm25);
    }

    [Fact]
    public void AmenityCache_Mapping_ShouldWork()
    {
        var entity = new AmenityCache
        {
            LocationKey = "K1",
            SchoolCount = 3,
            RetrievedAtUtc = DateTimeOffset.UtcNow
        };

        var dto = entity.ToDto();
        Assert.Equal(3, dto.SchoolCount);

        var backToEntity = dto.ToEntity("K1", 52, 4, 1000, DateTimeOffset.UtcNow.AddDays(1));
        Assert.Equal(3, backToEntity.SchoolCount);
    }
}
