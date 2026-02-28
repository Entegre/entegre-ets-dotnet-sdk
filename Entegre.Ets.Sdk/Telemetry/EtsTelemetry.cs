using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Entegre.Ets.Sdk.Telemetry;

/// <summary>
/// OpenTelemetry instrumentation for ETS SDK
/// </summary>
public static class EtsTelemetry
{
    /// <summary>
    /// Activity source name for tracing
    /// </summary>
    public const string ActivitySourceName = "Entegre.Ets.Sdk";

    /// <summary>
    /// Meter name for metrics
    /// </summary>
    public const string MeterName = "Entegre.Ets.Sdk";

    /// <summary>
    /// Activity source for distributed tracing
    /// </summary>
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName, "1.2.0");

    /// <summary>
    /// Meter for metrics
    /// </summary>
    public static readonly Meter Meter = new(MeterName, "1.2.0");

    // Counters
    private static readonly Counter<long> RequestsCounter = Meter.CreateCounter<long>(
        "ets.requests.total",
        "requests",
        "Total number of ETS API requests");

    private static readonly Counter<long> SuccessCounter = Meter.CreateCounter<long>(
        "ets.requests.success",
        "requests",
        "Number of successful ETS API requests");

    private static readonly Counter<long> FailureCounter = Meter.CreateCounter<long>(
        "ets.requests.failure",
        "requests",
        "Number of failed ETS API requests");

    private static readonly Counter<long> InvoicesSentCounter = Meter.CreateCounter<long>(
        "ets.invoices.sent",
        "invoices",
        "Number of invoices sent");

    private static readonly Counter<long> DispatchesSentCounter = Meter.CreateCounter<long>(
        "ets.dispatches.sent",
        "dispatches",
        "Number of dispatches sent");

    private static readonly Counter<long> WebhooksReceivedCounter = Meter.CreateCounter<long>(
        "ets.webhooks.received",
        "webhooks",
        "Number of webhooks received");

    // Histograms
    private static readonly Histogram<double> RequestDurationHistogram = Meter.CreateHistogram<double>(
        "ets.requests.duration",
        "ms",
        "Duration of ETS API requests in milliseconds");

    // Gauges
    private static readonly UpDownCounter<long> ActiveRequestsGauge = Meter.CreateUpDownCounter<long>(
        "ets.requests.active",
        "requests",
        "Number of active ETS API requests");

    /// <summary>
    /// Starts a new activity for an ETS operation
    /// </summary>
    public static Activity? StartActivity(string operationName, ActivityKind kind = ActivityKind.Client)
    {
        return ActivitySource.StartActivity(operationName, kind);
    }

    /// <summary>
    /// Records a request
    /// </summary>
    public static void RecordRequest(string endpoint, string method)
    {
        RequestsCounter.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("method", method));
        ActiveRequestsGauge.Add(1);
    }

    /// <summary>
    /// Records a successful request
    /// </summary>
    public static void RecordSuccess(string endpoint, double durationMs)
    {
        SuccessCounter.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint));
        RequestDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status", "success"));
        ActiveRequestsGauge.Add(-1);
    }

    /// <summary>
    /// Records a failed request
    /// </summary>
    public static void RecordFailure(string endpoint, double durationMs, string? errorType = null)
    {
        FailureCounter.Add(1, new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("error_type", errorType ?? "unknown"));
        RequestDurationHistogram.Record(durationMs, new KeyValuePair<string, object?>("endpoint", endpoint),
            new KeyValuePair<string, object?>("status", "failure"));
        ActiveRequestsGauge.Add(-1);
    }

    /// <summary>
    /// Records an invoice sent
    /// </summary>
    public static void RecordInvoiceSent(string invoiceType, string documentType)
    {
        InvoicesSentCounter.Add(1,
            new KeyValuePair<string, object?>("invoice_type", invoiceType),
            new KeyValuePair<string, object?>("document_type", documentType));
    }

    /// <summary>
    /// Records a dispatch sent
    /// </summary>
    public static void RecordDispatchSent(string dispatchType)
    {
        DispatchesSentCounter.Add(1, new KeyValuePair<string, object?>("dispatch_type", dispatchType));
    }

    /// <summary>
    /// Records a webhook received
    /// </summary>
    public static void RecordWebhookReceived(string eventType)
    {
        WebhooksReceivedCounter.Add(1, new KeyValuePair<string, object?>("event_type", eventType));
    }
}

/// <summary>
/// Extension methods for adding telemetry tags to activities
/// </summary>
public static class ActivityExtensions
{
    /// <summary>
    /// Adds ETS-specific tags to an activity
    /// </summary>
    public static Activity? SetEtsTags(this Activity? activity, string endpoint, string method)
    {
        activity?.SetTag("ets.endpoint", endpoint);
        activity?.SetTag("ets.method", method);
        activity?.SetTag("http.method", method);
        activity?.SetTag("http.url", endpoint);
        return activity;
    }

    /// <summary>
    /// Adds invoice-specific tags to an activity
    /// </summary>
    public static Activity? SetInvoiceTags(this Activity? activity, string? uuid, string? invoiceNumber, string? invoiceType)
    {
        activity?.SetTag("ets.invoice.uuid", uuid);
        activity?.SetTag("ets.invoice.number", invoiceNumber);
        activity?.SetTag("ets.invoice.type", invoiceType);
        return activity;
    }

    /// <summary>
    /// Adds dispatch-specific tags to an activity
    /// </summary>
    public static Activity? SetDispatchTags(this Activity? activity, string? uuid, string? dispatchType)
    {
        activity?.SetTag("ets.dispatch.uuid", uuid);
        activity?.SetTag("ets.dispatch.type", dispatchType);
        return activity;
    }

    /// <summary>
    /// Sets the activity status to error
    /// </summary>
    public static Activity? SetError(this Activity? activity, Exception ex)
    {
        activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity?.SetTag("error", true);
        activity?.SetTag("error.type", ex.GetType().Name);
        activity?.SetTag("error.message", ex.Message);
        return activity;
    }

    /// <summary>
    /// Sets the activity status to OK
    /// </summary>
    public static Activity? SetSuccess(this Activity? activity)
    {
        activity?.SetStatus(ActivityStatusCode.Ok);
        return activity;
    }
}

/// <summary>
/// Semantic convention keys for ETS telemetry
/// </summary>
public static class EtsSemanticConventions
{
    // Service
    public const string ServiceName = "service.name";
    public const string ServiceVersion = "service.version";

    // ETS-specific
    public const string EtsEndpoint = "ets.endpoint";
    public const string EtsMethod = "ets.method";
    public const string EtsInvoiceUuid = "ets.invoice.uuid";
    public const string EtsInvoiceNumber = "ets.invoice.number";
    public const string EtsInvoiceType = "ets.invoice.type";
    public const string EtsDispatchUuid = "ets.dispatch.uuid";
    public const string EtsDispatchType = "ets.dispatch.type";
    public const string EtsWebhookEventType = "ets.webhook.event_type";

    // HTTP
    public const string HttpMethod = "http.method";
    public const string HttpUrl = "http.url";
    public const string HttpStatusCode = "http.status_code";
    public const string HttpResponseContentLength = "http.response_content_length";

    // Error
    public const string Error = "error";
    public const string ErrorType = "error.type";
    public const string ErrorMessage = "error.message";
}
