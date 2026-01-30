using Microsoft.AspNetCore.SignalR;
using Valora.Api.Hubs;
using Valora.Application.Common.Interfaces;

namespace Valora.Api.Services;

public class SignalRNotificationService : IScraperNotificationService
{
    private readonly IHubContext<ScraperHub> _hubContext;

    public SignalRNotificationService(IHubContext<ScraperHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyProgressAsync(string message)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveProgress", message);
    }

    public async Task NotifyListingFoundAsync(string address)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveListing", address);
    }

    public async Task NotifyCompleteAsync()
    {
        await _hubContext.Clients.All.SendAsync("ReceiveComplete");
    }

    public async Task NotifyErrorAsync(string error)
    {
        await _hubContext.Clients.All.SendAsync("ReceiveError", error);
    }
}
