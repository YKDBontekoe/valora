namespace Valora.Application.Common.Interfaces;

public record ExternalUserDto(string Provider, string ProviderUserId, string Email, string Name);

public interface IExternalAuthService
{
    Task<ExternalUserDto> VerifyTokenAsync(string provider, string token);
}
