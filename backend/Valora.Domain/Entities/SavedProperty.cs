using System;
using Valora.Domain.Common;

namespace Valora.Domain.Entities;

public class SavedProperty : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? CachedScore { get; set; }
}
