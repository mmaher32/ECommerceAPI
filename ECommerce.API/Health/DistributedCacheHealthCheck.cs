using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ECommerce.API.Health;

public static class HealthCheckTags
{
    /// <summary>Dependencies the API needs before it should receive traffic.</summary>
    public const string Ready = "ready";
}

/// <summary>
/// Probes Redis through the same IDistributedCache the basket store uses, so a
/// passing check means the path baskets actually take is working, without pulling in
/// a separate Redis client just for health checks.
/// </summary>
public sealed class DistributedCacheHealthCheck(IDistributedCache cache) : IHealthCheck
{
    private const string ProbeKey = "health:probe";

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // A read is enough: a missing key still needs a round trip to the server.
            await cache.GetAsync(ProbeKey, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return new HealthCheckResult(
                context.Registration.FailureStatus,
                "Distributed cache is unreachable.",
                ex);
        }
    }
}
