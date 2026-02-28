using Microsoft.Extensions.Logging;

namespace Entegre.Ets.Sdk.Logging;

/// <summary>
/// Logging extensions for ETS SDK
/// </summary>
public static class EtsLoggerExtensions
{
    private static readonly Action<ILogger, string, string, Exception?> _requestStarted =
        LoggerMessage.Define<string, string>(
            LogLevel.Debug,
            new EventId(1, "RequestStarted"),
            "ETS API request started: {Method} {Endpoint}");

    private static readonly Action<ILogger, string, string, int, long, Exception?> _requestCompleted =
        LoggerMessage.Define<string, string, int, long>(
            LogLevel.Information,
            new EventId(2, "RequestCompleted"),
            "ETS API request completed: {Method} {Endpoint} - Status: {StatusCode}, Duration: {DurationMs}ms");

    private static readonly Action<ILogger, string, string, string, Exception?> _requestFailed =
        LoggerMessage.Define<string, string, string>(
            LogLevel.Error,
            new EventId(3, "RequestFailed"),
            "ETS API request failed: {Method} {Endpoint} - Error: {ErrorMessage}");

    private static readonly Action<ILogger, string, int, int, Exception?> _retryAttempt =
        LoggerMessage.Define<string, int, int>(
            LogLevel.Warning,
            new EventId(4, "RetryAttempt"),
            "Retrying ETS API request: {Endpoint}, Attempt {Attempt}/{MaxAttempts}");

    private static readonly Action<ILogger, string, string, Exception?> _invoiceSent =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(10, "InvoiceSent"),
            "Invoice sent successfully: UUID={Uuid}, Number={InvoiceNumber}");

    private static readonly Action<ILogger, string, string, Exception?> _invoiceFailed =
        LoggerMessage.Define<string, string>(
            LogLevel.Error,
            new EventId(11, "InvoiceFailed"),
            "Invoice sending failed: UUID={Uuid}, Error={ErrorMessage}");

    private static readonly Action<ILogger, string, string, Exception?> _webhookReceived =
        LoggerMessage.Define<string, string>(
            LogLevel.Information,
            new EventId(20, "WebhookReceived"),
            "Webhook received: Type={EventType}, DocumentUuid={DocumentUuid}");

    private static readonly Action<ILogger, Exception?> _webhookSignatureInvalid =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(21, "WebhookSignatureInvalid"),
            "Webhook signature validation failed");

    /// <summary>
    /// Logs that a request has started
    /// </summary>
    public static void LogRequestStarted(this ILogger logger, string method, string endpoint)
        => _requestStarted(logger, method, endpoint, null);

    /// <summary>
    /// Logs that a request has completed
    /// </summary>
    public static void LogRequestCompleted(this ILogger logger, string method, string endpoint, int statusCode, long durationMs)
        => _requestCompleted(logger, method, endpoint, statusCode, durationMs, null);

    /// <summary>
    /// Logs that a request has failed
    /// </summary>
    public static void LogRequestFailed(this ILogger logger, string method, string endpoint, string errorMessage, Exception? ex = null)
        => _requestFailed(logger, method, endpoint, errorMessage, ex);

    /// <summary>
    /// Logs a retry attempt
    /// </summary>
    public static void LogRetryAttempt(this ILogger logger, string endpoint, int attempt, int maxAttempts)
        => _retryAttempt(logger, endpoint, attempt, maxAttempts, null);

    /// <summary>
    /// Logs that an invoice was sent successfully
    /// </summary>
    public static void LogInvoiceSent(this ILogger logger, string uuid, string? invoiceNumber)
        => _invoiceSent(logger, uuid, invoiceNumber ?? "N/A", null);

    /// <summary>
    /// Logs that an invoice sending failed
    /// </summary>
    public static void LogInvoiceFailed(this ILogger logger, string? uuid, string errorMessage, Exception? ex = null)
        => _invoiceFailed(logger, uuid ?? "N/A", errorMessage, ex);

    /// <summary>
    /// Logs that a webhook was received
    /// </summary>
    public static void LogWebhookReceived(this ILogger logger, string eventType, string documentUuid)
        => _webhookReceived(logger, eventType, documentUuid, null);

    /// <summary>
    /// Logs that a webhook signature was invalid
    /// </summary>
    public static void LogWebhookSignatureInvalid(this ILogger logger)
        => _webhookSignatureInvalid(logger, null);
}

/// <summary>
/// Log categories for ETS SDK
/// </summary>
public static class EtsLogCategories
{
    /// <summary>
    /// ETS Client category
    /// </summary>
    public const string Client = "Entegre.Ets.Sdk.EtsClient";

    /// <summary>
    /// Webhook handler category
    /// </summary>
    public const string Webhook = "Entegre.Ets.Sdk.Webhook";

    /// <summary>
    /// Batch processor category
    /// </summary>
    public const string Batch = "Entegre.Ets.Sdk.Batch";
}
