using System.Net;
using Xunit;

namespace Valora.IntegrationTests;

public class HealthCheckTests : BaseIntegrationTest
{
    public HealthCheckTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Get_Health_ReturnsOk()
    {
        var response = await Client.GetAsync("/api/health");
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
