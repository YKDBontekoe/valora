using System.Net;
using System.Net.Http.Json;
using Shouldly;
using Valora.Application.DTOs;
using Valora.Domain.Entities;
using Xunit;

namespace Valora.IntegrationTests;

[Collection("TestDatabase")]
public class AdminSecurityTests : BaseIntegrationTest
{
    public AdminSecurityTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreateJob_WithInvalidType_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new BatchJobRequest(
            Type: "InvalidType",
            Target: "Amsterdam"
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/jobs", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateJob_WithInvalidTarget_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new BatchJobRequest(
            Type: "CityIngestion",
            Target: "A" // Too short
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/jobs", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        // Too long
        var longTarget = new string('a', 256);
        request = request with { Target = longTarget };
        response = await Client.PostAsJsonAsync("/api/admin/jobs", request);
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateJob_WithEmptyTarget_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();
        var request = new BatchJobRequest(
            Type: "CityIngestion",
            Target: ""
        );

        // Act
        var response = await Client.PostAsJsonAsync("/api/admin/jobs", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_WithInvalidPage_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/users?page=0");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetUsers_WithInvalidPageSize_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/users?pageSize=101");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        response = await Client.GetAsync("/api/admin/users?pageSize=0");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetJobs_WithInvalidLimit_ReturnsBadRequest()
    {
        // Arrange
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.GetAsync("/api/admin/jobs?limit=101");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);

        response = await Client.GetAsync("/api/admin/jobs?limit=0");
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

}
