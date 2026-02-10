using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Valora.Application.Enrichment;
using Valora.Infrastructure.Enrichment;
using Xunit;
using Xunit.Abstractions;

namespace Valora.IntegrationTests.Prototypes;

public class WozScrapingPrototype
{
    private readonly ITestOutputHelper _output;

    public WozScrapingPrototype(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact(Skip = "Returns 400 (WAF/Session) even with valid BAG ID. Requires headless browser.")]
    public async Task Can_Scrape_Woz_Value()
    {
        // 1. Setup Service Dependencies
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        var httpClient = new HttpClient(handler);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new ContextEnrichmentOptions());
        
        // Use a simple action logger to redirect logs to xUnit output
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddProvider(new XUnitLoggerProvider(_output));
            builder.SetMinimumLevel(LogLevel.Debug);
        });
        var logger = loggerFactory.CreateLogger<WozValuationService>();

        var service = new WozValuationService(httpClient, cache, options, logger);

        // 2. Execute Scraper
        // Target: Kerkstraat 1, Amsterdam (Residential)
        string street = "Kerkstraat";
        int number = 1;
        string city = "Amsterdam";
        // Known BAG Nummeraanduiding ID for Kerkstraat 1, 1017GA Amsterdam
        string bagId = "0363200000163643";

        _output.WriteLine($"Attempting to scrape WOZ for: {street} {number}, {city} (BAG ID: {bagId})");
        var result = await service.GetWozValuationAsync(street, number, null, city, bagId);

        // 3. Output Results
        if (result != null)
        {
            _output.WriteLine($"Scraping Successful!");
            _output.WriteLine($"WOZ Value: {result.Value:C0}");
            _output.WriteLine($"Reference Date: {result.ReferenceDate:yyyy-MM-dd}");
            _output.WriteLine($"Source: {result.Source}");
        }
        else
        {
            _output.WriteLine("Scraping Failed or No Result Found.");
        }

        Assert.NotNull(result);
        Assert.True(result.Value > 0);
    }
}

public class XUnitLoggerProvider : ILoggerProvider
{
    private readonly ITestOutputHelper _testOutputHelper;

    public XUnitLoggerProvider(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public ILogger CreateLogger(string categoryName)
        => new XUnitLogger(_testOutputHelper, categoryName);

    public void Dispose() { }
}

public class XUnitLogger : ILogger
{
    private readonly ITestOutputHelper _testOutputHelper;
    private readonly string _categoryName;

    public XUnitLogger(ITestOutputHelper testOutputHelper, string categoryName)
    {
        _testOutputHelper = testOutputHelper;
        _categoryName = categoryName;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _testOutputHelper.WriteLine($"{logLevel} [{_categoryName}] {formatter(state, exception)}");
        if (exception != null)
            _testOutputHelper.WriteLine(exception.ToString());
    }
}
