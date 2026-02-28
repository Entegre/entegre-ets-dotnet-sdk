using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Invoice;
using Entegre.Ets.Sdk.Models.Dispatch;
using Entegre.Ets.Sdk.Models.ProducerReceipt;

namespace Entegre.Ets.Sdk;

/// <summary>
/// Entegre ETS API client
/// </summary>
public class EtsClient : IEtsClient, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly EtsClientOptions _options;
    private readonly JsonSerializerOptions _jsonOptions;
    private bool _disposed;

    /// <summary>
    /// Creates a new ETS client
    /// </summary>
    public EtsClient(EtsClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var handler = options.HttpMessageHandler ?? new HttpClientHandler();
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = options.Timeout
        };

        ConfigureDefaultHeaders();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    /// <summary>
    /// Creates a new ETS client with configuration action
    /// </summary>
    public EtsClient(Action<EtsClientOptions> configure)
    {
        var options = new EtsClientOptions();
        configure(options);
        _options = options;

        var handler = options.HttpMessageHandler ?? new HttpClientHandler();
        _httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl),
            Timeout = options.Timeout
        };

        ConfigureDefaultHeaders();

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    private void ConfigureDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Add("X-Api-Key", _options.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("X-Api-Secret", _options.ApiSecret);
        _httpClient.DefaultRequestHeaders.Add("X-Customer-Id", _options.CustomerId);
        _httpClient.DefaultRequestHeaders.Add("X-Software-Id", _options.SoftwareId);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    #region Invoice Operations

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceResult>> SendInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<InvoiceRequest, InvoiceResult>(
            EtsEndpoints.SendInvoice,
            invoice,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<InvoiceResult>>> SendInvoicesAsync(
        IEnumerable<InvoiceRequest> invoices,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IEnumerable<InvoiceRequest>, List<InvoiceResult>>(
            EtsEndpoints.SendInvoice,
            invoices,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceResult>> SendDraftInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<InvoiceRequest, InvoiceResult>(
            EtsEndpoints.SendDraftInvoice,
            invoice,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceStatusResult>> GetInvoiceStatusAsync(
        InvoiceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<InvoiceStatusRequest, InvoiceStatusResult>(
            EtsEndpoints.GetInvoiceStatus,
            request,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<EInvoiceUserResult>> CheckEInvoiceUserAsync(
        string taxId,
        CancellationToken cancellationToken = default)
    {
        var request = new EInvoiceUserRequest { TaxId = taxId };
        return await PostAsync<EInvoiceUserRequest, EInvoiceUserResult>(
            EtsEndpoints.CheckEInvoiceUser,
            request,
            cancellationToken);
    }

    #endregion

    #region Dispatch Operations

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchResult>> SendDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<DispatchRequest, DispatchResult>(
            EtsEndpoints.SendDispatch,
            dispatch,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<DispatchResult>>> SendDispatchesAsync(
        IEnumerable<DispatchRequest> dispatches,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IEnumerable<DispatchRequest>, List<DispatchResult>>(
            EtsEndpoints.SendDispatch,
            dispatches,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchResult>> SendDraftDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<DispatchRequest, DispatchResult>(
            EtsEndpoints.SendDraftDispatch,
            dispatch,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchStatusResult>> GetDispatchStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<DispatchStatusResult>(
            $"{EtsEndpoints.GetDispatchStatus}?uuid={uuid}",
            cancellationToken);
    }

    #endregion

    #region Producer Receipt Operations

    /// <inheritdoc />
    public async Task<ApiResponse<ProducerReceiptResult>> SendProducerReceiptAsync(
        ProducerReceiptRequest receipt,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<ProducerReceiptRequest, ProducerReceiptResult>(
            EtsEndpoints.SendProducerReceipt,
            receipt,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<ProducerReceiptResult>>> SendProducerReceiptsAsync(
        IEnumerable<ProducerReceiptRequest> receipts,
        CancellationToken cancellationToken = default)
    {
        return await PostAsync<IEnumerable<ProducerReceiptRequest>, List<ProducerReceiptResult>>(
            EtsEndpoints.SendProducerReceipt,
            receipts,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<ProducerReceiptStatusResult>> GetProducerReceiptStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await GetAsync<ProducerReceiptStatusResult>(
            $"{EtsEndpoints.GetProducerReceiptStatus}?uuid={uuid}",
            cancellationToken);
    }

    #endregion

    #region HTTP Helpers

    private async Task<ApiResponse<TResponse>> GetAsync<TResponse>(
        string endpoint,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, cancellationToken);
        });
    }

    private async Task<ApiResponse<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest data,
        CancellationToken cancellationToken)
    {
        return await ExecuteWithRetryAsync(async () =>
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(endpoint, content, cancellationToken);
            return await HandleResponseAsync<TResponse>(response, cancellationToken);
        });
    }

    private async Task<ApiResponse<T>> HandleResponseAsync<T>(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var result = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
                return result ?? new ApiResponse<T> { Success = false, Message = "Empty response" };
            }
            catch (JsonException)
            {
                // Try to deserialize just the data
                var data = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return new ApiResponse<T> { Success = true, Data = data };
            }
        }

        // Handle error response
        try
        {
            var error = JsonSerializer.Deserialize<ApiResponse<T>>(content, _jsonOptions);
            return error ?? new ApiResponse<T>
            {
                Success = false,
                Message = $"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}"
            };
        }
        catch
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = $"HTTP {(int)response.StatusCode}: {content}"
            };
        }
    }

    private async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> action)
    {
        if (!_options.EnableRetry)
            return await action();

        Exception? lastException = null;

        for (var attempt = 0; attempt <= _options.MaxRetries; attempt++)
        {
            try
            {
                return await action();
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                if (attempt < _options.MaxRetries)
                {
                    var delay = _options.RetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay);
                }
            }
            catch (TaskCanceledException ex) when (!ex.CancellationToken.IsCancellationRequested)
            {
                lastException = ex;
                if (attempt < _options.MaxRetries)
                {
                    var delay = _options.RetryDelayMs * (int)Math.Pow(2, attempt);
                    await Task.Delay(delay);
                }
            }
        }

        throw new EtsApiException("All retry attempts failed", lastException!);
    }

    #endregion

    #region IDisposable

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Disposes resources
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
            return;

        if (disposing)
        {
            _httpClient.Dispose();
        }

        _disposed = true;
    }

    #endregion
}

/// <summary>
/// ETS client interface
/// </summary>
public interface IEtsClient
{
    /// <summary>
    /// Sends an invoice
    /// </summary>
    Task<ApiResponse<InvoiceResult>> SendInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple invoices
    /// </summary>
    Task<ApiResponse<List<InvoiceResult>>> SendInvoicesAsync(
        IEnumerable<InvoiceRequest> invoices,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a draft invoice
    /// </summary>
    Task<ApiResponse<InvoiceResult>> SendDraftInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets invoice status
    /// </summary>
    Task<ApiResponse<InvoiceStatusResult>> GetInvoiceStatusAsync(
        InvoiceStatusRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a tax ID is registered for E-Invoice
    /// </summary>
    Task<ApiResponse<EInvoiceUserResult>> CheckEInvoiceUserAsync(
        string taxId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a dispatch
    /// </summary>
    Task<ApiResponse<DispatchResult>> SendDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple dispatches
    /// </summary>
    Task<ApiResponse<List<DispatchResult>>> SendDispatchesAsync(
        IEnumerable<DispatchRequest> dispatches,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a draft dispatch
    /// </summary>
    Task<ApiResponse<DispatchResult>> SendDraftDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dispatch status
    /// </summary>
    Task<ApiResponse<DispatchStatusResult>> GetDispatchStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a producer receipt
    /// </summary>
    Task<ApiResponse<ProducerReceiptResult>> SendProducerReceiptAsync(
        ProducerReceiptRequest receipt,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends multiple producer receipts
    /// </summary>
    Task<ApiResponse<List<ProducerReceiptResult>>> SendProducerReceiptsAsync(
        IEnumerable<ProducerReceiptRequest> receipts,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets producer receipt status
    /// </summary>
    Task<ApiResponse<ProducerReceiptStatusResult>> GetProducerReceiptStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// ETS API exception
/// </summary>
public class EtsApiException : Exception
{
    public EtsApiException(string message) : base(message) { }
    public EtsApiException(string message, Exception innerException) : base(message, innerException) { }
}
