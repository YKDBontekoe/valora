using Google.Apis.Auth;
using Valora.Application.Common.Exceptions;
using Valora.Application.Common.Interfaces;

namespace Valora.Infrastructure.Services;

public class ExternalAuthService : IExternalAuthService
{
    public async Task<ExternalUserDto> VerifyTokenAsync(string provider, string token)
    {
        if (string.Equals(provider, "google", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var payload = await GoogleJsonWebSignature.ValidateAsync(token);
                return new ExternalUserDto("google", payload.Subject, payload.Email, payload.Name);
            }
            catch (InvalidJwtException)
            {
                throw new ValidationException("Invalid Google token.");
            }
            catch (Exception ex)
            {
                throw new ValidationException($"Failed to verify Google token: {ex.Message}");
            }
        }

        throw new ValidationException($"Provider '{provider}' is not supported.");
    }
}
