using Google.Apis.Auth;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;
using Valora.Application.Common.Interfaces.External;

namespace Valora.Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
    private readonly IGoogleTokenValidator _googleTokenValidator;

    public ExternalAuthService(IGoogleTokenValidator googleTokenValidator)
    {
        _googleTokenValidator = googleTokenValidator;
    }

    public async Task<ExternalUserDto> VerifyTokenAsync(string provider, string token)
    {
        if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var payload = await _googleTokenValidator.ValidateAsync(token);
                return new ExternalUserDto("google", payload.Subject, payload.Email, payload.Name);
            }
            catch (InvalidJwtException)
            {
                throw new ValidationException("Invalid Google token.");
            }
            catch (Exception ex) when (ex is not ValidationException)
            {
                var baseEx = ex.GetBaseException();
                if (baseEx is HttpRequestException ||
                    baseEx is System.Net.Sockets.SocketException ||
                    baseEx is TimeoutException ||
                    baseEx is TaskCanceledException)
                {
                    throw new HttpRequestException("Google authentication service is unavailable.", ex);
                }
                // Do not leak ex.Message in ValidationException to prevent internal details exposure
                throw new ValidationException("Failed to verify Google token.");
            }
        }

        throw new ValidationException($"Provider '{provider}' is not supported.");
    }
}
