using Microsoft.AspNetCore.SignalR;
using Moq;
using Valora.Api.Hubs;
using Valora.Api.Services;

namespace Valora.UnitTests.Services;

public class SignalRNotificationServiceTests
{
    private readonly Mock<IHubContext<ScraperHub>> _hubContextMock;
    private readonly Mock<IHubClients> _clientsMock;
    private readonly Mock<IClientProxy> _clientProxyMock;
    private readonly SignalRNotificationService _service;

    public SignalRNotificationServiceTests()
    {
        _hubContextMock = new Mock<IHubContext<ScraperHub>>();
        _clientsMock = new Mock<IHubClients>();
        _clientProxyMock = new Mock<IClientProxy>();

        _hubContextMock.Setup(x => x.Clients).Returns(_clientsMock.Object);
        _clientsMock.Setup(x => x.All).Returns(_clientProxyMock.Object);

        _service = new SignalRNotificationService(_hubContextMock.Object);
    }

    [Fact]
    public async Task NotifyProgressAsync_ShouldSendProgress()
    {
        await _service.NotifyProgressAsync("test");
        VerifySend("ReceiveProgress", "test");
    }

    [Fact]
    public async Task NotifyListingFoundAsync_ShouldSendListing()
    {
        await _service.NotifyListingFoundAsync("address");
        VerifySend("ReceiveListing", "address");
    }

    [Fact]
    public async Task NotifyCompleteAsync_ShouldSendComplete()
    {
        await _service.NotifyCompleteAsync();
        VerifySend("ReceiveComplete");
    }

    [Fact]
    public async Task NotifyErrorAsync_ShouldSendError()
    {
        await _service.NotifyErrorAsync("error");
        VerifySend("ReceiveError", "error");
    }

    private void VerifySend(string method, params object[] expectedArgs)
    {
        _clientProxyMock.Verify(
            x => x.SendCoreAsync(
                method,
                It.Is<object[]>(args => VerifyArgs(args, expectedArgs)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private bool VerifyArgs(object[] actual, object[] expected)
    {
        if (expected.Length == 0) return true;
        if (actual == null || actual.Length != expected.Length) return false;

        for (int i = 0; i < actual.Length; i++)
        {
            if (!actual[i].Equals(expected[i])) return false;
        }
        return true;
    }
}
