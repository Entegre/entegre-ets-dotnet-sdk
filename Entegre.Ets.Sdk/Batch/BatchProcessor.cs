using System.Collections.Concurrent;

namespace Entegre.Ets.Sdk.Batch;

/// <summary>
/// Batch processing options
/// </summary>
public class BatchOptions
{
    /// <summary>
    /// Maximum concurrent operations (default: 5)
    /// </summary>
    public int Concurrency { get; set; } = 5;

    /// <summary>
    /// Continue processing on error (default: true)
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Delay between items in milliseconds (default: 0)
    /// </summary>
    public int DelayBetweenMs { get; set; } = 0;

    /// <summary>
    /// Number of retry attempts per item (default: 0)
    /// </summary>
    public int Retries { get; set; } = 0;

    /// <summary>
    /// Delay between retries in milliseconds (default: 1000)
    /// </summary>
    public int RetryDelayMs { get; set; } = 1000;
}

/// <summary>
/// Batch processing result
/// </summary>
public class BatchResult<T>
{
    /// <summary>
    /// Total number of items processed
    /// </summary>
    public int Total { get; init; }

    /// <summary>
    /// Number of successful items
    /// </summary>
    public int Successful { get; init; }

    /// <summary>
    /// Number of failed items
    /// </summary>
    public int Failed { get; init; }

    /// <summary>
    /// Individual results
    /// </summary>
    public IReadOnlyList<BatchItemResult<T>> Results { get; init; } = Array.Empty<BatchItemResult<T>>();

    /// <summary>
    /// Total processing duration
    /// </summary>
    public TimeSpan Duration { get; init; }

    /// <summary>
    /// Success rate as percentage
    /// </summary>
    public double SuccessRate => Total > 0 ? (double)Successful / Total * 100 : 0;
}

/// <summary>
/// Individual batch item result
/// </summary>
public class BatchItemResult<T>
{
    /// <summary>
    /// Item index in the batch
    /// </summary>
    public int Index { get; init; }

    /// <summary>
    /// Whether the item was processed successfully
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Result data (if successful)
    /// </summary>
    public T? Data { get; init; }

    /// <summary>
    /// Error information (if failed)
    /// </summary>
    public Exception? Error { get; init; }

    /// <summary>
    /// Processing duration for this item
    /// </summary>
    public TimeSpan Duration { get; init; }
}

/// <summary>
/// Progress callback delegate
/// </summary>
public delegate void BatchProgressCallback<T>(int completed, int total, BatchItemResult<T> lastResult);

/// <summary>
/// Batch processor with concurrency control
/// </summary>
public static class BatchProcessor
{
    /// <summary>
    /// Process items in batch
    /// </summary>
    public static async Task<BatchResult<TOutput>> ProcessAsync<TInput, TOutput>(
        IReadOnlyList<TInput> items,
        Func<TInput, int, CancellationToken, Task<TOutput>> processor,
        BatchOptions? options = null,
        BatchProgressCallback<TOutput>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new BatchOptions();
        var startTime = DateTime.UtcNow;
        var results = new ConcurrentBag<BatchItemResult<TOutput>>();
        var completed = 0;
        var failed = 0;

        using var semaphore = new SemaphoreSlim(options.Concurrency);

        var tasks = items.Select(async (item, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                if (!options.ContinueOnError && Volatile.Read(ref failed) > 0)
                    return;

                var result = await ProcessItemAsync(item, index, processor, options, cancellationToken);
                results.Add(result);

                Interlocked.Increment(ref completed);
                if (!result.Success)
                    Interlocked.Increment(ref failed);

                onProgress?.Invoke(completed, items.Count, result);

                if (options.DelayBetweenMs > 0)
                    await Task.Delay(options.DelayBetweenMs, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);

        var orderedResults = results.OrderBy(r => r.Index).ToList();
        var duration = DateTime.UtcNow - startTime;

        return new BatchResult<TOutput>
        {
            Total = items.Count,
            Successful = orderedResults.Count(r => r.Success),
            Failed = orderedResults.Count(r => !r.Success),
            Results = orderedResults,
            Duration = duration
        };
    }

    /// <summary>
    /// Process items in batch (simplified signature)
    /// </summary>
    public static Task<BatchResult<TOutput>> ProcessAsync<TInput, TOutput>(
        IReadOnlyList<TInput> items,
        Func<TInput, Task<TOutput>> processor,
        BatchOptions? options = null,
        BatchProgressCallback<TOutput>? onProgress = null,
        CancellationToken cancellationToken = default)
    {
        return ProcessAsync(
            items,
            (item, _, ct) => processor(item),
            options,
            onProgress,
            cancellationToken);
    }

    private static async Task<BatchItemResult<TOutput>> ProcessItemAsync<TInput, TOutput>(
        TInput item,
        int index,
        Func<TInput, int, CancellationToken, Task<TOutput>> processor,
        BatchOptions options,
        CancellationToken cancellationToken)
    {
        var itemStart = DateTime.UtcNow;
        Exception? lastError = null;

        for (var attempt = 0; attempt <= options.Retries; attempt++)
        {
            try
            {
                var data = await processor(item, index, cancellationToken);
                return new BatchItemResult<TOutput>
                {
                    Index = index,
                    Success = true,
                    Data = data,
                    Duration = DateTime.UtcNow - itemStart
                };
            }
            catch (Exception ex)
            {
                lastError = ex;

                if (attempt < options.Retries)
                {
                    var delay = options.RetryDelayMs * (attempt + 1);
                    await Task.Delay(delay, cancellationToken);
                }
            }
        }

        return new BatchItemResult<TOutput>
        {
            Index = index,
            Success = false,
            Error = lastError,
            Duration = DateTime.UtcNow - itemStart
        };
    }
}

/// <summary>
/// Static helper for parallel operations
/// </summary>
public static class Batch
{
    /// <summary>
    /// Parallel execution with limit
    /// </summary>
    public static async Task<TOutput[]> ParallelAsync<TInput, TOutput>(
        IReadOnlyList<TInput> items,
        Func<TInput, Task<TOutput>> processor,
        int maxConcurrency = 5,
        CancellationToken cancellationToken = default)
    {
        var results = new TOutput[items.Count];
        using var semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = items.Select(async (item, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                results[index] = await processor(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }
}
