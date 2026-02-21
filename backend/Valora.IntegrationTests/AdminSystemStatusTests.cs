using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class AdminSystemStatusTests : BaseIntegrationTest
{
    public AdminSystemStatusTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task GetSystemStatus_ReturnsOk_WhenAdmin()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/system-status");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var content = await response.Content.ReadFromJsonAsync<SystemStatusDto>();
        content.ShouldNotBeNull();
        content.DbConnectivity.ShouldBe("Connected");
        content.QueueDepth.ShouldBeGreaterThanOrEqualTo(0);
    }
}
