using Valora.Application.DTOs;
using Valora.Domain.Services.Scoring;

namespace Valora.Application.Enrichment.Builders;

public static class CrimeMetricBuilder
{
    public static List<ContextMetricDto> Build(CrimeStatsDto? crime, List<string> warnings)
    {
        if (crime is null)
        {
            warnings.Add("CBS crime statistics were unavailable; safety score is partial.");
            return [];
        }

        var totalScore = CrimeScoringRules.ScoreTotalCrime(crime.TotalCrimesPer1000);
        var burglaryScore = CrimeScoringRules.ScoreBurglary(crime.BurglaryPer1000);
        var violentScore = CrimeScoringRules.ScoreViolentCrime(crime.ViolentCrimePer1000);

        return
        [
            new("total_crimes", "Total Crimes", crime.TotalCrimesPer1000, "per 1000", totalScore, "CBS StatLine 47018NED"),
            new("burglary", "Burglary Rate", crime.BurglaryPer1000, "per 1000", burglaryScore, "CBS StatLine 47018NED"),
            new("violent_crime", "Violent Crime", crime.ViolentCrimePer1000, "per 1000", violentScore, "CBS StatLine 47018NED"),
            new("theft", "Theft Rate", crime.TheftPer1000, "per 1000", null, "CBS StatLine 47018NED"),
            new("vandalism", "Vandalism Rate", crime.VandalismPer1000, "per 1000", null, "CBS StatLine 47018NED")
        ];
    }
}
