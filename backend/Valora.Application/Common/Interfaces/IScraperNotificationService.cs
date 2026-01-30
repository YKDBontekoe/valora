namespace Valora.Application.Common.Interfaces;

public interface IScraperNotificationService
{
    Task NotifyProgressAsync(string message);
    Task NotifyListingFoundAsync(string address);
    Task NotifyCompleteAsync();
    Task NotifyErrorAsync(string error);
}
