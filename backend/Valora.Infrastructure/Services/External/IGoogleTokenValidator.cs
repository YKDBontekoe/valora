using Google.Apis.Auth;

namespace Valora.Infrastructure.Services.External;

public interface IGoogleTokenValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
}
