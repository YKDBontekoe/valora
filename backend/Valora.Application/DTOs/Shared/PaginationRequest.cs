using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs.Shared;

public record class PaginationRequest(
    [property: Range(1, PaginationRequest.MaxPage)] int Page = 1,
    [property: Range(1, 100)] int PageSize = 10
)
{
    public const int MaxPage = 10_000;
}
