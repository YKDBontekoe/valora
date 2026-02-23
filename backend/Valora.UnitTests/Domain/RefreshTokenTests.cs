using Valora.Domain.Entities;
using Xunit;

namespace Valora.UnitTests.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void ComputeHash_WithNullOrEmpty_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => RefreshToken.ComputeHash(null!));
        Assert.Throws<ArgumentException>(() => RefreshToken.ComputeHash(""));
        Assert.Throws<ArgumentException>(() => RefreshToken.ComputeHash("   "));
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
}
