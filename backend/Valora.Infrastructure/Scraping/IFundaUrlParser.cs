namespace Valora.Infrastructure.Scraping;

public interface IFundaUrlParser
{
    string? ExtractRegionFromUrl(string url);
}
