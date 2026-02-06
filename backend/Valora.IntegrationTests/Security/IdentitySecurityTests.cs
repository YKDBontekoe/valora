using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace Valora.IntegrationTests.Security;

[Collection("TestDatabase")]
public class IdentitySecurityTests : BaseIntegrationTest
{
    public IdentitySecurityTests(TestDatabaseFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task Register_WithWeakPassword_ShouldFailComplexityCheck()
    {
        // Arrange
        // Password "simplepassword1" has length > 8 and digits, but NO Uppercase and NO NonAlphanumeric
        // Our policy requires: Digit, Lowercase, Uppercase, NonAlphanumeric, MinLength 8
        var weakPassword = "simplepassword1";

        var request = new
        {
            Email = "weakpw@example.com",
            Password = weakPassword,
            ConfirmPassword = weakPassword
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/auth/register", request);

        // Assert
        // Expecting BadRequest (400) or similar failure
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Read response to verify it's the Identity errors
        var content = await response.Content.ReadAsStringAsync();

        // Identity errors usually contain "Passwords must have at least one non alphanumeric character."
        // or "Passwords must have at least one uppercase ('A'-'Z')."
        Assert.Contains("uppercase", content.ToLowerInvariant());
        Assert.Contains("non alphanumeric", content.ToLowerInvariant());
    }
}
