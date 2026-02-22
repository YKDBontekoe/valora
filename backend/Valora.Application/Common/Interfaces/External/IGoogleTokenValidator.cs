using Valora.Application.DTOs;

namespace Valora.Application.Common.Interfaces.External;

public interface IGoogleTokenValidator
{
    Task<GoogleTokenPayloadDto> ValidateAsync(string idToken);
}
