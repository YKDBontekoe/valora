using Valora.Application.DTOs;

namespace Valora.Infrastructure.Enrichment.Builders;

public class CrimeMetricBuilder
{
    public List<ContextMetricDto> Build(CrimeStatsDto? crime, List<string> warnings)
    {
        if (crime is null)
        {
            warnings.Add("Crime statistics were unavailable; safety score is partial.");
            return [];
        }

        var totalScore = ScoreTotalCrime(crime.TotalCrimesPer1000);
        var burglaryScore = ScoreBurglary(crime.BurglaryPer1000);
        var violentScore = ScoreViolentCrime(crime.ViolentCrimePer1000);

        return
        [
            new("total_crimes", "Total Crimes", crime.TotalCrimesPer1000, "per 1000", totalScore, "CBS StatLine 47018NED"),
            new("burglary", "Burglary Rate", crime.BurglaryPer1000, "per 1000", burglaryScore, "CBS StatLine 47018NED"),
            new("violent_crime", "Violent Crime", crime.ViolentCrimePer1000, "per 1000", violentScore, "CBS StatLine 47018NED"),
            new("theft", "Theft Rate", crime.TheftPer1000, "per 1000", null, "CBS StatLine 47018NED"),
            new("vandalism", "Vandalism Rate", crime.VandalismPer1000, "per 1000", null, "CBS StatLine 47018NED")
        ];
    }

    private static double? ScoreTotalCrime(int? crimesPer1000)
    {
        if (!crimesPer1000.HasValue) return null;

        // Lower crime is better - Dutch average is around 50 per 1000
        return crimesPer1000.Value switch
        {
            <= 20 => 100,
            <= 35 => 85,
            <= 50 => 70,
            <= 75 => 50,
            <= 100 => 30,
            _ => 15
        };
    }

    private static double? ScoreBurglary(int? burglaryPer1000)
    {
        if (!burglaryPer1000.HasValue) return null;

        return burglaryPer1000.Value switch
        {
            <= 2 => 100,
            <= 5 => 80,
            <= 10 => 60,
            <= 15 => 40,
            _ => 20
        };
    }

    private static double? ScoreViolentCrime(int? violentPer1000)
    {
        if (!violentPer1000.HasValue) return null;

        return violentPer1000.Value switch
        {
            <= 2 => 100,
            <= 5 => 75,
            <= 10 => 50,
            _ => 25
        };
    }
}
