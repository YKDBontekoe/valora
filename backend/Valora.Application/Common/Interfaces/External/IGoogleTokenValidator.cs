using Google.Apis.Auth;

namespace Valora.Application.Common.Interfaces.External;

public interface IGoogleTokenValidator
{
    Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken);
}
