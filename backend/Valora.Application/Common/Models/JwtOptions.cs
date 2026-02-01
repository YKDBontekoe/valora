namespace Valora.Application.Common.Models;

public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public double ExpiryMinutes { get; set; } = 60;
}
