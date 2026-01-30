using Microsoft.AspNetCore.SignalR;

namespace Valora.Api.Hubs;

public class ScraperHub : Hub
{
    // Clients will listen to methods like "ReceiveProgress", "ReceiveListing", "ReceiveComplete", "ReceiveError"
}
