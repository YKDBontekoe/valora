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
    string Type,
    string Target
);
