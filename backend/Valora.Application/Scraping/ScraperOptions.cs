namespace Valora.Application.Scraping;

public class ScraperOptions
{
    public const string SectionName = "Scraper";
    
    public List<string> SearchUrls { get; set; } = [];
    public int DelayBetweenRequestsMs { get; set; } = 2000;
    public int MaxRetries { get; set; } = 3;
    public string CronExpression { get; set; } = "*/15 * * * *"; // Every 15 minutes
    public int MaxApiCallsPerMinute { get; set; } = 10;
    public int MaxApiCallsPerRun { get; set; } = 20;
    public int RecentPagesPerRegion { get; set; } = 1;
    public int MaxBackfillPagesPerRun { get; set; } = 5;
    public bool FocusOnNewConstruction { get; set; } = true;
}
