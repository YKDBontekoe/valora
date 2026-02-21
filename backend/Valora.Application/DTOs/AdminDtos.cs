using System.ComponentModel.DataAnnotations;
using Valora.Domain.Entities;

namespace Valora.Application.DTOs;

public record AdminUserDto(
    string Id,
    string Email,
    IList<string> Roles
);

public record AdminStatsDto(
    int TotalUsers,
    int TotalNotifications
);

public record BatchJobRequest(
    [property: Required] [property: EnumDataType(typeof(BatchJobType))] string Type,
    [property: Required] [property: StringLength(255, MinimumLength = 2)] string Target
);

public record AdminCreateUserDto(
    [property: Required] [property: EmailAddress] string Email,
    [property: Required] [property: MinLength(8)] [property: MaxLength(128)] string Password,
    [property: Required] [property: MinLength(1)] IList<string> Roles
)
{
    public override string ToString()
    {
        return $"AdminCreateUserDto {{ Email = {Email}, Roles = {string.Join(", ", Roles)}, Password = [REDACTED] }}";
    }
}
