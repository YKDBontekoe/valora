using System.Diagnostics.CodeAnalysis;
using Google.Apis.Auth;
using Valora.Application.Common.Interfaces.External;
using Valora.Application.DTOs;

namespace Valora.Infrastructure.Services.External;

[ExcludeFromCodeCoverage]
public class GoogleTokenValidator : IGoogleTokenValidator
{
    public async Task<GoogleTokenPayloadDto> ValidateAsync(string idToken)
    {
        var payload = await GoogleJsonWebSignature.ValidateAsync(idToken);

        return new GoogleTokenPayloadDto
        {
            Email = payload.Email,
            Name = payload.Name,
            Picture = payload.Picture,
            Subject = payload.Subject
        };
    }
}
