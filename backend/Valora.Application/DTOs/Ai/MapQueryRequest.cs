using System.ComponentModel.DataAnnotations;

namespace Valora.Application.DTOs.Ai;

public class MapQueryRequest
{
    [Required]
    public string Query { get; set; } = string.Empty;

    public double? CenterLat { get; set; }
    public double? CenterLon { get; set; }
    public double? Zoom { get; set; }
}
