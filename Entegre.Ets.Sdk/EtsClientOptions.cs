namespace Entegre.Ets.Sdk;

/// <summary>
/// Configuration options for EtsClient
/// </summary>
public class EtsClientOptions
{
    /// <summary>
    /// Production API base URL
    /// </summary>
    public const string ProductionUrl = "https://ets.bulutix.com";

    /// <summary>
    /// Test/Sandbox API base URL
    /// </summary>
    public const string TestUrl = "https://ets-test.bulutix.com";

    /// <summary>
    /// API base URL (default: Production)
    /// </summary>
    public string BaseUrl { get; set; } = ProductionUrl;

    /// <summary>
    /// API key for authentication
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// API secret for authentication
    /// </summary>
    public string ApiSecret { get; set; } = string.Empty;

    /// <summary>
    /// Customer ID
    /// </summary>
    public string CustomerId { get; set; } = string.Empty;

    /// <summary>
    /// Software ID
    /// </summary>
    public string SoftwareId { get; set; } = string.Empty;

    /// <summary>
    /// Request timeout
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable retry on transient errors
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Retry delay in milliseconds
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;

    /// <summary>
    /// Enable debug logging
    /// </summary>
    public bool EnableDebugLogging { get; set; }

    /// <summary>
    /// Custom HTTP message handler (for testing)
    /// </summary>
    public HttpMessageHandler? HttpMessageHandler { get; set; }

    /// <summary>
    /// Use test/sandbox environment
    /// </summary>
    public EtsClientOptions UseTestEnvironment()
    {
        BaseUrl = TestUrl;
        return this;
    }

    /// <summary>
    /// Use production environment
    /// </summary>
    public EtsClientOptions UseProductionEnvironment()
    {
        BaseUrl = ProductionUrl;
        return this;
    }
}

/// <summary>
/// ETS API endpoints
/// </summary>
internal static class EtsEndpoints
{
    public const string SendInvoice = "/api/v1/invoice/send";
    public const string SendDraftInvoice = "/api/v1/invoice/draft";
    public const string GetInvoiceStatus = "/api/v1/invoice/status";
    public const string CancelInvoice = "/api/v1/invoice/cancel";
    public const string CheckEInvoiceUser = "/api/v1/invoice/check-user";

    public const string SendDispatch = "/api/v1/dispatch/send";
    public const string SendDraftDispatch = "/api/v1/dispatch/draft";
    public const string GetDispatchStatus = "/api/v1/dispatch/status";
    public const string CheckDispatchUser = "/api/v1/dispatch/check-user";

    public const string SendProducerReceipt = "/api/v1/producer-receipt/send";
    public const string GetProducerReceiptStatus = "/api/v1/producer-receipt/status";
}
