using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth;

namespace Valora.Infrastructure.Services.External;

[ExcludeFromCodeCoverage]
public class GoogleTokenValidator : IGoogleTokenValidator
{
    public Task<GoogleJsonWebSignature.Payload> ValidateAsync(string idToken)
    {
        return GoogleJsonWebSignature.ValidateAsync(idToken);
    }
}
