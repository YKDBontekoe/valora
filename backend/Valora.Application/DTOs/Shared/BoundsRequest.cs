using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs.Shared;

public record class BoundsRequest(
    double MinLat,
    double MinLon,
    double MaxLat,
    double MaxLon
) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (MinLat > MaxLat)
        {
            yield return new ValidationResult("MinLat must be less than MaxLat.", new[] { nameof(MinLat), nameof(MaxLat) });
        }
    }
}
