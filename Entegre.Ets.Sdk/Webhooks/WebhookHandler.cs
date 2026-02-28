using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Entegre.Ets.Sdk.Webhooks;

/// <summary>
/// Webhook event types
/// </summary>
public enum WebhookEventType
{
    /// <summary>Invoice sent</summary>
    InvoiceSent,
    /// <summary>Invoice delivered</summary>
    InvoiceDelivered,
    /// <summary>Invoice accepted</summary>
    InvoiceAccepted,
    /// <summary>Invoice rejected</summary>
    InvoiceRejected,
    /// <summary>Invoice failed</summary>
    InvoiceFailed,
    /// <summary>Archive sent</summary>
    ArchiveSent,
    /// <summary>Archive cancelled</summary>
    ArchiveCancelled,
    /// <summary>Dispatch sent</summary>
    DispatchSent,
    /// <summary>Dispatch delivered</summary>
    DispatchDelivered,
    /// <summary>Producer receipt sent</summary>
    ProducerReceiptSent,
    /// <summary>Unknown event</summary>
    Unknown
}

/// <summary>
/// Webhook event payload
/// </summary>
public class WebhookEvent
{
    /// <summary>
    /// Event type
    /// </summary>
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    /// <summary>
    /// Parsed event type
    /// </summary>
    [JsonIgnore]
    public WebhookEventType EventType => ParseEventType(Event);

    /// <summary>
    /// Document UUID
    /// </summary>
    [JsonPropertyName("documentUuid")]
    public string DocumentUuid { get; set; } = string.Empty;

    /// <summary>
    /// Document number
    /// </summary>
    [JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get; set; }

    /// <summary>
    /// Document type (INVOICE, DISPATCH, PRODUCER_RECEIPT)
    /// </summary>
    [JsonPropertyName("documentType")]
    public string? DocumentType { get; set; }

    /// <summary>
    /// Status code
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status description
    /// </summary>
    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Error message (if failed)
    /// </summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Error code (if failed)
    /// </summary>
    [JsonPropertyName("errorCode")]
    public string? ErrorCode { get; set; }

    /// <summary>
    /// Event timestamp
    /// </summary>
    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Customer ID
    /// </summary>
    [JsonPropertyName("customerId")]
    public string? CustomerId { get; set; }

    private static WebhookEventType ParseEventType(string eventName)
    {
        return eventName.ToLowerInvariant() switch
        {
            "invoice.sent" => WebhookEventType.InvoiceSent,
            "invoice.delivered" => WebhookEventType.InvoiceDelivered,
            "invoice.accepted" => WebhookEventType.InvoiceAccepted,
            "invoice.rejected" => WebhookEventType.InvoiceRejected,
            "invoice.failed" => WebhookEventType.InvoiceFailed,
            "archive.sent" => WebhookEventType.ArchiveSent,
            "archive.cancelled" => WebhookEventType.ArchiveCancelled,
            "dispatch.sent" => WebhookEventType.DispatchSent,
            "dispatch.delivered" => WebhookEventType.DispatchDelivered,
            "producerreceipt.sent" => WebhookEventType.ProducerReceiptSent,
            _ => WebhookEventType.Unknown
        };
    }
}

/// <summary>
/// Webhook handler options
/// </summary>
public class WebhookOptions
{
    /// <summary>
    /// Secret key for signature verification
    /// </summary>
    public string Secret { get; set; } = string.Empty;

    /// <summary>
    /// Maximum time difference allowed between request timestamp and server time
    /// </summary>
    public TimeSpan TimestampTolerance { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Whether to verify signature (default: true)
    /// </summary>
    public bool VerifySignature { get; set; } = true;
}

/// <summary>
/// Interface for webhook handler
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Processes a webhook payload
    /// </summary>
    Task<WebhookEvent> ProcessAsync(string payload, string? signature, string? timestamp);

    /// <summary>
    /// Verifies webhook signature
    /// </summary>
    bool VerifySignature(string payload, string signature, string timestamp);
}

/// <summary>
/// Webhook handler with signature verification
/// </summary>
public class WebhookHandler : IWebhookHandler
{
    private readonly WebhookOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;

    public WebhookHandler(WebhookOptions options)
    {
        _options = options;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    /// <inheritdoc />
    public async Task<WebhookEvent> ProcessAsync(string payload, string? signature, string? timestamp)
    {
        if (_options.VerifySignature)
        {
            if (string.IsNullOrEmpty(signature))
                throw new WebhookSignatureException("Missing signature header");

            if (string.IsNullOrEmpty(timestamp))
                throw new WebhookSignatureException("Missing timestamp header");

            if (!VerifySignature(payload, signature, timestamp))
                throw new WebhookSignatureException("Invalid signature");

            // Verify timestamp
            if (long.TryParse(timestamp, out var ts))
            {
                var eventTime = DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;
                var timeDiff = DateTime.UtcNow - eventTime;

                if (Math.Abs(timeDiff.TotalSeconds) > _options.TimestampTolerance.TotalSeconds)
                    throw new WebhookSignatureException("Timestamp out of tolerance");
            }
        }

        var webhookEvent = JsonSerializer.Deserialize<WebhookEvent>(payload, _jsonOptions)
            ?? throw new WebhookException("Failed to parse webhook payload");

        return await Task.FromResult(webhookEvent);
    }

    /// <inheritdoc />
    public bool VerifySignature(string payload, string signature, string timestamp)
    {
        var signedPayload = $"{timestamp}.{payload}";
        var expectedSignature = ComputeHmacSha256(signedPayload, _options.Secret);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signature),
            Encoding.UTF8.GetBytes(expectedSignature));
    }

    private static string ComputeHmacSha256(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

/// <summary>
/// Webhook signature exception
/// </summary>
public class WebhookSignatureException : Exception
{
    public WebhookSignatureException(string message) : base(message) { }
}

/// <summary>
/// General webhook exception
/// </summary>
public class WebhookException : Exception
{
    public WebhookException(string message) : base(message) { }
}

/// <summary>
/// Webhook event handler delegate
/// </summary>
public delegate Task WebhookEventHandler(WebhookEvent webhookEvent);

/// <summary>
/// Webhook router for handling different event types
/// </summary>
public class WebhookRouter
{
    private readonly Dictionary<WebhookEventType, List<WebhookEventHandler>> _handlers = new();
    private readonly List<WebhookEventHandler> _allHandlers = new();

    /// <summary>
    /// Registers a handler for a specific event type
    /// </summary>
    public WebhookRouter On(WebhookEventType eventType, WebhookEventHandler handler)
    {
        if (!_handlers.ContainsKey(eventType))
            _handlers[eventType] = new List<WebhookEventHandler>();

        _handlers[eventType].Add(handler);
        return this;
    }

    /// <summary>
    /// Registers a handler for all events
    /// </summary>
    public WebhookRouter OnAll(WebhookEventHandler handler)
    {
        _allHandlers.Add(handler);
        return this;
    }

    /// <summary>
    /// Routes the event to registered handlers
    /// </summary>
    public async Task RouteAsync(WebhookEvent webhookEvent)
    {
        // Call all handlers
        foreach (var handler in _allHandlers)
        {
            await handler(webhookEvent);
        }

        // Call specific handlers
        if (_handlers.TryGetValue(webhookEvent.EventType, out var handlers))
        {
            foreach (var handler in handlers)
            {
                await handler(webhookEvent);
            }
        }
    }
}
