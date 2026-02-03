namespace Valora.Application.Scraping;

public class ScraperOptions
{
    public const string SectionName = "Scraper";

    public List<string> SearchUrls { get; set; } = [];
    public int DelayBetweenRequestsMs { get; set; } = 2000;
    public int MaxRetries { get; set; } = 3;
    public string CronExpression { get; set; } = "0 */6 * * *"; // Every 6 hours
}
