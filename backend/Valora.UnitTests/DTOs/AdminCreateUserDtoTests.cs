using Valora.Application.DTOs;
using Xunit;

namespace Valora.UnitTests.DTOs;

public class AdminCreateUserDtoTests
{
    [Fact]
    public void ToString_RedactsPassword()
    {
        // Arrange
        var dto = new AdminCreateUserDto("test@example.com", "SecretPassword123!", new List<string> { "Admin" });

        // Act
        var result = dto.ToString();

        // Assert
        Assert.Contains("Email = test@example.com", result);
        Assert.Contains("Roles = Admin", result);
        Assert.Contains("Password = [REDACTED]", result);
        Assert.DoesNotContain("SecretPassword123!", result);
    }

    [Fact]
    public void ToString_HandlesMultipleRoles()
    {
        // Arrange
        var dto = new AdminCreateUserDto("test@example.com", "pass", new List<string> { "Admin", "User" });

        // Act
        var result = dto.ToString();

        // Assert
        Assert.Contains("Roles = Admin, User", result);
    }
}
