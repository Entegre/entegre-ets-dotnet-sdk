namespace Entegre.Ets.Sdk.Resilience;

/// <summary>
/// Resilience options for ETS client (Polly-compatible)
/// </summary>
public class ResilienceOptions
{
    /// <summary>
    /// Enable retry policy
    /// </summary>
    public bool EnableRetry { get; set; } = true;

    /// <summary>
    /// Maximum retry attempts
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Base delay for retries (exponential backoff)
    /// </summary>
    public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Maximum delay between retries
    /// </summary>
    public TimeSpan RetryMaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Add jitter to retry delays
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Enable circuit breaker
    /// </summary>
    public bool EnableCircuitBreaker { get; set; } = true;

    /// <summary>
    /// Number of failures before opening circuit
    /// </summary>
    public int CircuitBreakerThreshold { get; set; } = 5;

    /// <summary>
    /// Duration to keep circuit open
    /// </summary>
    public TimeSpan CircuitBreakerDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable timeout policy
    /// </summary>
    public bool EnableTimeout { get; set; } = true;

    /// <summary>
    /// Timeout duration
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Enable bulkhead (concurrency limit)
    /// </summary>
    public bool EnableBulkhead { get; set; } = false;

    /// <summary>
    /// Maximum concurrent requests
    /// </summary>
    public int MaxConcurrentRequests { get; set; } = 10;

    /// <summary>
    /// Maximum queue size for bulkhead
    /// </summary>
    public int MaxQueueSize { get; set; } = 100;

    /// <summary>
    /// HTTP status codes that should trigger a retry
    /// </summary>
    public int[] RetryableStatusCodes { get; set; } = [408, 429, 500, 502, 503, 504];

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = false;

    /// <summary>
    /// Requests per second limit
    /// </summary>
    public int RequestsPerSecond { get; set; } = 10;
}

/// <summary>
/// Circuit breaker state
/// </summary>
public enum CircuitState
{
    /// <summary>
    /// Circuit is closed (normal operation)
    /// </summary>
    Closed,

    /// <summary>
    /// Circuit is open (rejecting requests)
    /// </summary>
    Open,

    /// <summary>
    /// Circuit is half-open (testing)
    /// </summary>
    HalfOpen
}

/// <summary>
/// Simple circuit breaker implementation
/// </summary>
public class CircuitBreaker
{
    private readonly int _threshold;
    private readonly TimeSpan _openDuration;
    private readonly object _lock = new();

    private int _failureCount;
    private DateTime _openedAt;
    private CircuitState _state = CircuitState.Closed;

    /// <summary>
    /// Current circuit state
    /// </summary>
    public CircuitState State
    {
        get
        {
            lock (_lock)
            {
                if (_state == CircuitState.Open && DateTime.UtcNow - _openedAt >= _openDuration)
                {
                    _state = CircuitState.HalfOpen;
                }
                return _state;
            }
        }
    }

    /// <summary>
    /// Creates a new circuit breaker
    /// </summary>
    public CircuitBreaker(int threshold = 5, TimeSpan? openDuration = null)
    {
        _threshold = threshold;
        _openDuration = openDuration ?? TimeSpan.FromSeconds(30);
    }

    /// <summary>
    /// Checks if the circuit allows requests
    /// </summary>
    public bool AllowRequest()
    {
        var state = State;
        return state != CircuitState.Open;
    }

    /// <summary>
    /// Records a successful request
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }

    /// <summary>
    /// Records a failed request
    /// </summary>
    public void RecordFailure()
    {
        lock (_lock)
        {
            _failureCount++;
            if (_failureCount >= _threshold)
            {
                _state = CircuitState.Open;
                _openedAt = DateTime.UtcNow;
            }
        }
    }

    /// <summary>
    /// Resets the circuit breaker
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _failureCount = 0;
            _state = CircuitState.Closed;
        }
    }
}

/// <summary>
/// Simple rate limiter implementation
/// </summary>
public class RateLimiter
{
    private readonly int _requestsPerSecond;
    private readonly Queue<DateTime> _requestTimes = new();
    private readonly object _lock = new();

    /// <summary>
    /// Creates a new rate limiter
    /// </summary>
    public RateLimiter(int requestsPerSecond = 10)
    {
        _requestsPerSecond = requestsPerSecond;
    }

    /// <summary>
    /// Acquires a permit to make a request
    /// </summary>
    public async Task<bool> AcquireAsync(CancellationToken cancellationToken = default)
    {
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            lock (_lock)
            {
                var now = DateTime.UtcNow;

                // Remove old entries
                while (_requestTimes.Count > 0 && now - _requestTimes.Peek() > TimeSpan.FromSeconds(1))
                {
                    _requestTimes.Dequeue();
                }

                // Check if we can make a request
                if (_requestTimes.Count < _requestsPerSecond)
                {
                    _requestTimes.Enqueue(now);
                    return true;
                }
            }

            // Wait a bit before trying again
            await Task.Delay(100, cancellationToken);
        }
    }
}

/// <summary>
/// Retry helper with exponential backoff
/// </summary>
public static class RetryHelper
{
    private static readonly Random Jitter = new();

    /// <summary>
    /// Executes an action with retry logic
    /// </summary>
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> action,
        ResilienceOptions options,
        CancellationToken cancellationToken = default)
    {
        Exception? lastException = null;

        for (var attempt = 0; attempt <= options.MaxRetries; attempt++)
        {
            try
            {
                return await action(cancellationToken);
            }
            catch (HttpRequestException ex) when (attempt < options.MaxRetries)
            {
                lastException = ex;
                await WaitBeforeRetry(attempt, options, cancellationToken);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested && attempt < options.MaxRetries)
            {
                lastException = ex;
                await WaitBeforeRetry(attempt, options, cancellationToken);
            }
        }

        throw new EtsApiException("All retry attempts failed", lastException!);
    }

    private static async Task WaitBeforeRetry(int attempt, ResilienceOptions options, CancellationToken ct)
    {
        var delay = CalculateDelay(attempt, options);
        await Task.Delay(delay, ct);
    }

    private static TimeSpan CalculateDelay(int attempt, ResilienceOptions options)
    {
        // Exponential backoff
        var exponentialDelay = TimeSpan.FromMilliseconds(
            options.RetryBaseDelay.TotalMilliseconds * Math.Pow(2, attempt));

        // Cap at max delay
        if (exponentialDelay > options.RetryMaxDelay)
        {
            exponentialDelay = options.RetryMaxDelay;
        }

        // Add jitter
        if (options.UseJitter)
        {
            var jitterMs = Jitter.Next(0, (int)(exponentialDelay.TotalMilliseconds * 0.2));
            exponentialDelay = exponentialDelay.Add(TimeSpan.FromMilliseconds(jitterMs));
        }

        return exponentialDelay;
    }
}
