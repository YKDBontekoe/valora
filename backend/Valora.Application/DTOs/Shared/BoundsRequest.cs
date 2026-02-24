using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs.Shared;

public record class BoundsRequest(
    [property: Range(-90, 90)] double MinLat,
    [property: Range(-180, 180)] double MinLon,
    [property: Range(-90, 90)] double MaxLat,
    [property: Range(-180, 180)] double MaxLon
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
