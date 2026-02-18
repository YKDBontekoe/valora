using Google.Apis.Auth;
using Valora.Application.Common.Interfaces.External;

namespace Valora.Infrastructure.Services.External;

public class GoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken)
    {
        return GoogleJsonWebSignature.ValidateAsync(idToken);
    }
}
