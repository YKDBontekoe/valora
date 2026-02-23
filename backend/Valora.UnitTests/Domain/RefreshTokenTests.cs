using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Domain;

public class RefreshTokenTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ComputeHash_WithNullOrEmpty_ThrowsArgumentException(string? token)
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.ComputeHash(token!));
    }

    [Fact]
    public void ComputeHash_WithValidToken_ReturnsHash()
    {
        var token = "valid_token";
        var hash = RefreshToken.ComputeHash(token);
        Assert.NotNull(hash);
        Assert.NotEmpty(hash);
        Assert.NotEqual(token, hash);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidUserId_ThrowsArgumentException(string? userId)
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.Create(userId!, TimeProvider.System));
    }

    [Fact]
    public void Create_WithNullTimeProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => RefreshToken.Create("valid_user", null!));
    }
}
