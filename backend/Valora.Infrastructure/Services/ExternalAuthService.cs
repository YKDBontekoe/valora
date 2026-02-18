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
            catch (Exception ex)
            {
                throw new ValidationException($"Failed to verify Google token: {ex.Message}");
            }
        }

        throw new ValidationException($"Provider '{provider}' is not supported.");
    }
}
