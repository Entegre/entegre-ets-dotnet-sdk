using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Entegre.Ets.Sdk.HealthChecks;

/// <summary>
/// Extension methods for adding ETS health checks
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Adds ETS API health check
    /// </summary>
    /// <param name="builder">Health checks builder</param>
    /// <param name="name">Health check name (default: ets-api)</param>
    /// <param name="failureStatus">Failure status (default: Unhealthy)</param>
    /// <param name="tags">Tags for filtering</param>
    /// <param name="timeout">Timeout for the check</param>
    /// <returns>Health checks builder</returns>
    public static IHealthChecksBuilder AddEtsHealthCheck(
        this IHealthChecksBuilder builder,
        string name = "ets-api",
        HealthStatus? failureStatus = null,
        IEnumerable<string>? tags = null,
        TimeSpan? timeout = null)
    {
        return builder.Add(new HealthCheckRegistration(
            name,
            sp =>
            {
                var client = sp.GetRequiredService<IEtsClient>();
                var options = new EtsHealthCheckOptions
                {
                    Name = name,
                    FailureStatus = failureStatus ?? HealthStatus.Unhealthy,
                    Tags = tags,
                    Timeout = timeout
                };
                return new EtsHealthCheck(client, options);
            },
            failureStatus,
            tags,
            timeout));
    }

    /// <summary>
    /// Adds ETS API health check with custom options
    /// </summary>
    /// <param name="builder">Health checks builder</param>
    /// <param name="configure">Options configuration action</param>
    /// <returns>Health checks builder</returns>
    public static IHealthChecksBuilder AddEtsHealthCheck(
        this IHealthChecksBuilder builder,
        Action<EtsHealthCheckOptions> configure)
    {
        var options = new EtsHealthCheckOptions();
        configure(options);

        return builder.Add(new HealthCheckRegistration(
            options.Name,
            sp =>
            {
                var client = sp.GetRequiredService<IEtsClient>();
                return new EtsHealthCheck(client, options);
            },
            options.FailureStatus,
            options.Tags,
            options.Timeout));
    }
}
