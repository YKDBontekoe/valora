import re

with open('backend/Valora.Api/Program.cs', 'r') as f:
    content = f.read()

new_implementation = """
/// <summary>
/// Health check endpoint. Used by Docker Compose and load balancers.
/// </summary>
api.MapGet("/health", async (ValoraDbContext db, Valora.Api.Services.IRequestMetricsService metricsService, CancellationToken ct) =>
{
    try
    {
        var canConnect = await db.Database.CanConnectAsync(ct);

        int activeJobs = 0;
        int queuedJobs = 0;
        int failedJobs = 0;
        DateTime? lastPipelineSuccess = null;

        if (canConnect)
        {
            var jobStats = await db.BatchJobs
                .GroupBy(j => j.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Status, x => x.Count, ct);

            activeJobs = jobStats.TryGetValue(BatchJobStatus.Processing, out var processing) ? processing : 0;
            queuedJobs = jobStats.TryGetValue(BatchJobStatus.Pending, out var pending) ? pending : 0;
            failedJobs = jobStats.TryGetValue(BatchJobStatus.Failed, out var failed) ? failed : 0;

            lastPipelineSuccess = await db.BatchJobs
                .Where(j => j.Status == BatchJobStatus.Completed)
                .OrderByDescending(j => j.CompletedAt)
                .Select(j => j.CompletedAt)
                .FirstOrDefaultAsync(ct);
        }

        var p50 = metricsService.GetPercentile(50);
        var p95 = metricsService.GetPercentile(95);
        var p99 = metricsService.GetPercentile(99);

        var response = new
        {
            status = canConnect ? "Healthy" : "Unhealthy",
            database = canConnect,
            apiLatency = (int)p50,
            apiLatencyP50 = (int)p50,
            apiLatencyP95 = (int)p95,
            apiLatencyP99 = (int)p99,
            activeJobs = activeJobs,
            queuedJobs = queuedJobs,
            failedJobs = failedJobs,
            lastPipelineSuccess = lastPipelineSuccess,
            timestamp = DateTime.UtcNow
        };

        if (canConnect)
        {
            return Results.Ok(response);
        }

        return Results.Json(response, statusCode: 503);
    }
    catch (Exception)
    {
        return Results.Json(new
        {
            status = "Unhealthy",
            database = false,
            apiLatency = 0,
            apiLatencyP50 = 0,
            apiLatencyP95 = 0,
            apiLatencyP99 = 0,
            activeJobs = 0,
            queuedJobs = 0,
            failedJobs = 0,
            lastPipelineSuccess = (DateTime?)null,
            timestamp = DateTime.UtcNow,
            error = "Critical system failure"
        }, statusCode: 503);
    }
})
.DisableRateLimiting();
"""

# Regex to match the existing MapGet("/health", ...) block
# We assume it starts with api.MapGet("/health" and ends with .DisableRateLimiting();
# Since it spans multiple lines, we use dotall or manually handle it.
# The existing block ends with .DisableRateLimiting(); which is unique enough in this section hopefully.

pattern = r'api\.MapGet\("/health",.*?\)\s*\.DisableRateLimiting\(\);'
match = re.search(pattern, content, re.DOTALL)

if match:
    new_content = content[:match.start()] + new_implementation.strip() + content[match.end():]
    with open('backend/Valora.Api/Program.cs', 'w') as f:
        f.write(new_content)
    print("Successfully updated Program.cs")
else:
    print("Could not find health endpoint block")
