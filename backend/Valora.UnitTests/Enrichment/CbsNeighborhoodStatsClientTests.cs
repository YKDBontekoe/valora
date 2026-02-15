using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using System.Net;
using Valora.Application.Common.Interfaces;
using Valora.Application.DTOs;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Enrichment;

public class CbsNeighborhoodStatsClientTests
{
    private readonly Mock<IContextCacheRepository> _dbCache;
    private readonly IMemoryCache _memCache;
    private readonly Mock<ILogger<CbsNeighborhoodStatsClient>> _logger;
    private readonly IOptions<ContextEnrichmentOptions> _options;

    public CbsNeighborhoodStatsClientTests()
    {
        _dbCache = new Mock<IContextCacheRepository>();
        _memCache = new MemoryCache(new MemoryCacheOptions());
        _logger = new Mock<ILogger<CbsNeighborhoodStatsClient>>();
        _options = Options.Create(new ContextEnrichmentOptions { CbsBaseUrl = "https://cbs.local" });
    }

    [Fact]
    public async Task GetStatsAsync_WhenMemCacheMiss_ChecksDbCache()
    {
        var location = new ResolvedLocationDto("q", "A", 0, 0, null, null, null, null, null, null, "BU01", null, null);
        var dbStats = new CbsNeighborhoodStats { RegionCode = "BU01", Residents = 123, ExpiresAtUtc = DateTimeOffset.UtcNow.AddHours(1) };

        _dbCache.Setup(x => x.GetNeighborhoodStatsAsync("BU01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(dbStats);

        var client = new CbsNeighborhoodStatsClient(new HttpClient(), _memCache, _dbCache.Object, _options, _logger.Object);
        var result = await client.GetStatsAsync(location);

        Assert.NotNull(result);
        Assert.Equal(123, result.Residents);
        _dbCache.Verify(x => x.GetNeighborhoodStatsAsync("BU01", It.IsAny<CancellationToken>()), Times.Once);
    }
}
