namespace Valora.Domain.Services.Scoring;

public static class HousingScoringRules
{
    public static double? ScoreOwnerOccupied(int? value)
    {
        if (!value.HasValue) return null;
        return Math.Clamp(value.Value * 1.25, 0, 100);
    }

    public static double? ScorePrivateRental(int? value)
    {
        if (!value.HasValue) return null;
        return value.Value switch
        {
            <= 10 => 70,
            <= 20 => 85,
            <= 35 => 100,
            <= 50 => 80,
            _ => 60
        };
    }

    public static double? ScoreBuildMix(int? pre2000, int? post2000)
    {
        if (!pre2000.HasValue && !post2000.HasValue) return null;
        if (!pre2000.HasValue || !post2000.HasValue)
        {
            return 70;
        }

        var delta = Math.Abs(pre2000.Value - post2000.Value);
        return Math.Clamp(100 - (delta * 1.2), 40, 100);
    }
}
