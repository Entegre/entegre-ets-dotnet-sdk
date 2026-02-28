using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Entegre.Ets.Sdk.HealthChecks;

/// <summary>
/// Health check for ETS API connectivity
/// </summary>
public class EtsHealthCheck : IHealthCheck
{
    private readonly IEtsClient _client;
    private readonly EtsHealthCheckOptions _options;

    /// <summary>
    /// Creates a new ETS health check
    /// </summary>
    public EtsHealthCheck(IEtsClient client, EtsHealthCheckOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? new EtsHealthCheckOptions();
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to check a known tax ID to verify API connectivity
            var result = await _client.CheckEInvoiceUserAsync(
                _options.TestTaxId,
                cancellationToken);

            if (result.Success)
            {
                return HealthCheckResult.Healthy("ETS API is reachable", new Dictionary<string, object>
                {
                    ["endpoint"] = "CheckEInvoiceUser",
                    ["testTaxId"] = _options.TestTaxId,
                    ["responseTime"] = DateTime.UtcNow
                });
            }

            return HealthCheckResult.Degraded(
                $"ETS API returned error: {result.Message}",
                data: new Dictionary<string, object>
                {
                    ["errorMessage"] = result.Message ?? "Unknown error"
                });
        }
        catch (HttpRequestException ex)
        {
            return HealthCheckResult.Unhealthy(
                "ETS API is unreachable",
                ex,
                new Dictionary<string, object>
                {
                    ["exception"] = ex.Message
                });
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            return HealthCheckResult.Unhealthy(
                "ETS API request timed out",
                ex,
                new Dictionary<string, object>
                {
                    ["timeout"] = true
                });
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "ETS API health check failed",
                ex,
                new Dictionary<string, object>
                {
                    ["exception"] = ex.Message
                });
        }
    }
}

/// <summary>
/// Options for ETS health check
/// </summary>
public class EtsHealthCheckOptions
{
    /// <summary>
    /// Tax ID to use for testing (default: known GIB test number)
    /// </summary>
    public string TestTaxId { get; set; } = "1234567890";

    /// <summary>
    /// Health check name
    /// </summary>
    public string Name { get; set; } = "ets-api";

    /// <summary>
    /// Failure status (default: Unhealthy)
    /// </summary>
    public HealthStatus FailureStatus { get; set; } = HealthStatus.Unhealthy;

    /// <summary>
    /// Tags for filtering
    /// </summary>
    public IEnumerable<string>? Tags { get; set; }

    /// <summary>
    /// Timeout for the health check
    /// </summary>
    public TimeSpan? Timeout { get; set; }
}
