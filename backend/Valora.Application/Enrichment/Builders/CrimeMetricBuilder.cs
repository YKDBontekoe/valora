using Valora.Application.DTOs;
using Valora.Domain.Common;
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
            new("total_crimes", "Total Crimes", crime.TotalCrimesPer1000, "per 1000", totalScore, DataSources.CbsCrimeStatLine),
            new("burglary", "Burglary Rate", crime.BurglaryPer1000, "per 1000", burglaryScore, DataSources.CbsCrimeStatLine),
            new("violent_crime", "Violent Crime", crime.ViolentCrimePer1000, "per 1000", violentScore, DataSources.CbsCrimeStatLine),
            new("theft", "Theft Rate", crime.TheftPer1000, "per 1000", null, DataSources.CbsCrimeStatLine),
            new("vandalism", "Vandalism Rate", crime.VandalismPer1000, "per 1000", null, DataSources.CbsCrimeStatLine)
        ];
    }
}
