namespace Entegre.Ets.Sdk.RealTime;

/// <summary>
/// Event arguments for document status changes
/// </summary>
public class DocumentStatusChangedEventArgs : EventArgs
{
    /// <summary>
    /// Document UUID
    /// </summary>
    public string Uuid { get; init; } = string.Empty;

    /// <summary>
    /// Document type (INVOICE, DISPATCH, PRODUCER_RECEIPT)
    /// </summary>
    public string DocumentType { get; init; } = string.Empty;

    /// <summary>
    /// New status
    /// </summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>
    /// Previous status
    /// </summary>
    public string? PreviousStatus { get; init; }

    /// <summary>
    /// Status change timestamp
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    /// <summary>
    /// Additional message
    /// </summary>
    public string? Message { get; init; }
}

/// <summary>
/// Event arguments for connection state changes
/// </summary>
public class ConnectionStateChangedEventArgs : EventArgs
{
    /// <summary>
    /// Current connection state
    /// </summary>
    public RealTimeConnectionState State { get; init; }

    /// <summary>
    /// Error if disconnected due to error
    /// </summary>
    public Exception? Error { get; init; }
}

/// <summary>
/// Real-time connection state
/// </summary>
public enum RealTimeConnectionState
{
    /// <summary>
    /// Disconnected
    /// </summary>
    Disconnected,

    /// <summary>
    /// Connecting
    /// </summary>
    Connecting,

    /// <summary>
    /// Connected
    /// </summary>
    Connected,

    /// <summary>
    /// Reconnecting
    /// </summary>
    Reconnecting
}

/// <summary>
/// Interface for real-time ETS updates
/// </summary>
public interface IEtsRealTimeClient : IAsyncDisposable
{
    /// <summary>
    /// Current connection state
    /// </summary>
    RealTimeConnectionState State { get; }

    /// <summary>
    /// Event raised when a document status changes
    /// </summary>
    event EventHandler<DocumentStatusChangedEventArgs>? OnDocumentStatusChanged;

    /// <summary>
    /// Event raised when connection state changes
    /// </summary>
    event EventHandler<ConnectionStateChangedEventArgs>? OnConnectionStateChanged;

    /// <summary>
    /// Connects to the real-time service
    /// </summary>
    Task ConnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Disconnects from the real-time service
    /// </summary>
    Task DisconnectAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to status updates for a specific document
    /// </summary>
    Task SubscribeToDocumentAsync(string uuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribes from status updates for a specific document
    /// </summary>
    Task UnsubscribeFromDocumentAsync(string uuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to all invoice status updates
    /// </summary>
    Task SubscribeToInvoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribes to all dispatch status updates
    /// </summary>
    Task SubscribeToDispatchesAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Real-time client options
/// </summary>
public class RealTimeOptions
{
    /// <summary>
    /// Real-time hub URL
    /// </summary>
    public string HubUrl { get; set; } = string.Empty;

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
    /// Enable automatic reconnection
    /// </summary>
    public bool AutoReconnect { get; set; } = true;

    /// <summary>
    /// Maximum reconnection attempts
    /// </summary>
    public int MaxReconnectAttempts { get; set; } = 10;

    /// <summary>
    /// Reconnection delay
    /// </summary>
    public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Real-time client implementation using polling (SignalR-compatible interface)
/// </summary>
public class PollingRealTimeClient : IEtsRealTimeClient
{
    private readonly IEtsClient _client;
    private readonly RealTimeOptions _options;
    private readonly Dictionary<string, string> _trackedDocuments = new();
    private readonly CancellationTokenSource _pollingCts = new();
    private Task? _pollingTask;

    /// <inheritdoc />
    public RealTimeConnectionState State { get; private set; } = RealTimeConnectionState.Disconnected;

    /// <inheritdoc />
    public event EventHandler<DocumentStatusChangedEventArgs>? OnDocumentStatusChanged;

    /// <inheritdoc />
    public event EventHandler<ConnectionStateChangedEventArgs>? OnConnectionStateChanged;

    /// <summary>
    /// Polling interval
    /// </summary>
    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Creates a new polling real-time client
    /// </summary>
    public PollingRealTimeClient(IEtsClient client, RealTimeOptions? options = null)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _options = options ?? new RealTimeOptions();
    }

    /// <inheritdoc />
    public Task ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (State == RealTimeConnectionState.Connected)
            return Task.CompletedTask;

        State = RealTimeConnectionState.Connecting;
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { State = State });

        _pollingTask = PollForUpdatesAsync(_pollingCts.Token);

        State = RealTimeConnectionState.Connected;
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { State = State });

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task DisconnectAsync(CancellationToken cancellationToken = default)
    {
        _pollingCts.Cancel();

        if (_pollingTask != null)
        {
            try
            {
                await _pollingTask;
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }

        State = RealTimeConnectionState.Disconnected;
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs { State = State });
    }

    /// <inheritdoc />
    public Task SubscribeToDocumentAsync(string uuid, CancellationToken cancellationToken = default)
    {
        lock (_trackedDocuments)
        {
            if (!_trackedDocuments.ContainsKey(uuid))
            {
                _trackedDocuments[uuid] = string.Empty; // Unknown initial status
            }
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task UnsubscribeFromDocumentAsync(string uuid, CancellationToken cancellationToken = default)
    {
        lock (_trackedDocuments)
        {
            _trackedDocuments.Remove(uuid);
        }
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SubscribeToInvoicesAsync(CancellationToken cancellationToken = default)
    {
        // In polling mode, we track individual documents
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SubscribeToDispatchesAsync(CancellationToken cancellationToken = default)
    {
        // In polling mode, we track individual documents
        return Task.CompletedTask;
    }

    private async Task PollForUpdatesAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(PollingInterval, cancellationToken);

                List<string> uuidsToCheck;
                lock (_trackedDocuments)
                {
                    uuidsToCheck = _trackedDocuments.Keys.ToList();
                }

                foreach (var uuid in uuidsToCheck)
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    try
                    {
                        var result = await _client.GetInvoiceStatusAsync(
                            new Models.Invoice.InvoiceStatusRequest { Uuid = uuid },
                            cancellationToken);

                        if (result.Success && result.Data != null)
                        {
                            string? previousStatus;
                            lock (_trackedDocuments)
                            {
                                _trackedDocuments.TryGetValue(uuid, out previousStatus);
                            }

                            if (previousStatus != result.Data.Status)
                            {
                                lock (_trackedDocuments)
                                {
                                    _trackedDocuments[uuid] = result.Data.Status;
                                }

                                OnDocumentStatusChanged?.Invoke(this, new DocumentStatusChangedEventArgs
                                {
                                    Uuid = uuid,
                                    DocumentType = "INVOICE",
                                    Status = result.Data.Status,
                                    PreviousStatus = string.IsNullOrEmpty(previousStatus) ? null : previousStatus,
                                    Message = result.Data.StatusDescription
                                });
                            }
                        }
                    }
                    catch (Exception)
                    {
                        // Continue polling other documents
                    }
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Continue polling
            }
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        _pollingCts.Dispose();
    }
}
