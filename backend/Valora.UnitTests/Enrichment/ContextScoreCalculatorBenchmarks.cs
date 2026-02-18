using System.Diagnostics;
using Valora.Domain.Models;
using Valora.Domain.Services;
using Xunit;


namespace Valora.UnitTests.Enrichment;

public class ContextScoreCalculatorBenchmarks
{
    [Fact]
    public void Compare_AverageScore_Performance()
    {
        var metrics = new List<ContextMetricModel>();
        for (int i = 0; i < 1000; i++)
        {
            metrics.Add(new ContextMetricModel(
                "key" + i,
                "label" + i,
                (double)i,
                "unit",
                i % 2 == 0 ? (double)i : null,
                "source"
            ));
        }

        // Create input for the calculator
        var input = new CategoryMetricsInput(
            metrics, // Social
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>(),
            new List<ContextMetricModel>()
        );

        const int iterations = 10000;

        // Warm up
        for (int i = 0; i < 100; i++)
        {
            ContextScoreCalculator.ComputeCategoryScores(input);
        }

        // Measure Current Implementation
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            ContextScoreCalculator.ComputeCategoryScores(input);
        }
        sw.Stop();

        Console.WriteLine($"Current Implementation: {sw.Elapsed.TotalMilliseconds:F2}ms");
    }
}
