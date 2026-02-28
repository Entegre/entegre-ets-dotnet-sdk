using Microsoft.Extensions.Caching.Memory;
using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Invoice;
using Entegre.Ets.Sdk.Models.Dispatch;
using Entegre.Ets.Sdk.Models.ProducerReceipt;
using Entegre.Ets.Sdk.Models.Incoming;
using Entegre.Ets.Sdk.ExchangeRate;

namespace Entegre.Ets.Sdk.Caching;

/// <summary>
/// Cache options for ETS client
/// </summary>
public class EtsCacheOptions
{
    /// <summary>
    /// Enable caching for E-Invoice user checks (default: true)
    /// </summary>
    public bool CacheUserChecks { get; set; } = true;

    /// <summary>
    /// Cache duration for E-Invoice user checks (default: 1 hour)
    /// </summary>
    public TimeSpan UserCheckCacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Enable caching for status queries (default: true)
    /// </summary>
    public bool CacheStatusQueries { get; set; } = true;

    /// <summary>
    /// Cache duration for status queries (default: 30 seconds)
    /// </summary>
    public TimeSpan StatusCacheDuration { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Cache duration for incoming invoice list (default: 1 minute)
    /// </summary>
    public TimeSpan IncomingListCacheDuration { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Enable sliding expiration (default: true)
    /// </summary>
    public bool UseSlidingExpiration { get; set; } = true;
}

/// <summary>
/// ETS client wrapper with caching support
/// </summary>
public class CachingEtsClient : IEtsClient
{
    private readonly IEtsClient _inner;
    private readonly IMemoryCache _cache;
    private readonly EtsCacheOptions _options;

    private const string UserCheckPrefix = "ets_user_";
    private const string InvoiceStatusPrefix = "ets_inv_status_";
    private const string DispatchStatusPrefix = "ets_dsp_status_";
    private const string ReceiptStatusPrefix = "ets_rcp_status_";

    /// <summary>
    /// Creates a new caching ETS client
    /// </summary>
    public CachingEtsClient(IEtsClient inner, IMemoryCache cache, EtsCacheOptions? options = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options ?? new EtsCacheOptions();
    }

    /// <inheritdoc />
    public Task<ApiResponse<InvoiceResult>> SendInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
        => _inner.SendInvoiceAsync(invoice, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<List<InvoiceResult>>> SendInvoicesAsync(
        IEnumerable<InvoiceRequest> invoices,
        CancellationToken cancellationToken = default)
        => _inner.SendInvoicesAsync(invoices, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<InvoiceResult>> SendDraftInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
        => _inner.SendDraftInvoiceAsync(invoice, cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceStatusResult>> GetInvoiceStatusAsync(
        InvoiceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_options.CacheStatusQueries || string.IsNullOrEmpty(request.Uuid))
        {
            return await _inner.GetInvoiceStatusAsync(request, cancellationToken);
        }

        var cacheKey = $"{InvoiceStatusPrefix}{request.Uuid}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ConfigureCacheEntry(entry, _options.StatusCacheDuration);
            return await _inner.GetInvoiceStatusAsync(request, cancellationToken);
        }) ?? await _inner.GetInvoiceStatusAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<ApiResponse<EInvoiceUserResult>> CheckEInvoiceUserAsync(
        string taxId,
        CancellationToken cancellationToken = default)
    {
        if (!_options.CacheUserChecks)
        {
            return await _inner.CheckEInvoiceUserAsync(taxId, cancellationToken);
        }

        var cacheKey = $"{UserCheckPrefix}{taxId}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ConfigureCacheEntry(entry, _options.UserCheckCacheDuration);
            return await _inner.CheckEInvoiceUserAsync(taxId, cancellationToken);
        }) ?? await _inner.CheckEInvoiceUserAsync(taxId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<DispatchResult>> SendDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
        => _inner.SendDispatchAsync(dispatch, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<List<DispatchResult>>> SendDispatchesAsync(
        IEnumerable<DispatchRequest> dispatches,
        CancellationToken cancellationToken = default)
        => _inner.SendDispatchesAsync(dispatches, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<DispatchResult>> SendDraftDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
        => _inner.SendDraftDispatchAsync(dispatch, cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchStatusResult>> GetDispatchStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        if (!_options.CacheStatusQueries)
        {
            return await _inner.GetDispatchStatusAsync(uuid, cancellationToken);
        }

        var cacheKey = $"{DispatchStatusPrefix}{uuid}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ConfigureCacheEntry(entry, _options.StatusCacheDuration);
            return await _inner.GetDispatchStatusAsync(uuid, cancellationToken);
        }) ?? await _inner.GetDispatchStatusAsync(uuid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<ProducerReceiptResult>> SendProducerReceiptAsync(
        ProducerReceiptRequest receipt,
        CancellationToken cancellationToken = default)
        => _inner.SendProducerReceiptAsync(receipt, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<List<ProducerReceiptResult>>> SendProducerReceiptsAsync(
        IEnumerable<ProducerReceiptRequest> receipts,
        CancellationToken cancellationToken = default)
        => _inner.SendProducerReceiptsAsync(receipts, cancellationToken);

    /// <inheritdoc />
    public async Task<ApiResponse<ProducerReceiptStatusResult>> GetProducerReceiptStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        if (!_options.CacheStatusQueries)
        {
            return await _inner.GetProducerReceiptStatusAsync(uuid, cancellationToken);
        }

        var cacheKey = $"{ReceiptStatusPrefix}{uuid}";

        return await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            ConfigureCacheEntry(entry, _options.StatusCacheDuration);
            return await _inner.GetProducerReceiptStatusAsync(uuid, cancellationToken);
        }) ?? await _inner.GetProducerReceiptStatusAsync(uuid, cancellationToken);
    }

    /// <inheritdoc />
    public Task<ApiResponse<IncomingInvoiceListResponse>> GetIncomingInvoicesAsync(
        IncomingInvoiceListRequest request,
        CancellationToken cancellationToken = default)
        => _inner.GetIncomingInvoicesAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<IncomingInvoice>> GetIncomingInvoiceAsync(
        string uuid,
        CancellationToken cancellationToken = default)
        => _inner.GetIncomingInvoiceAsync(uuid, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<InvoiceResponseResult>> AcceptInvoiceAsync(
        string uuid,
        string? note = null,
        CancellationToken cancellationToken = default)
        => _inner.AcceptInvoiceAsync(uuid, note, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<InvoiceResponseResult>> RejectInvoiceAsync(
        string uuid,
        string reason,
        string? note = null,
        CancellationToken cancellationToken = default)
        => _inner.RejectInvoiceAsync(uuid, reason, note, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<byte[]>> GetIncomingInvoicePdfAsync(
        string uuid,
        CancellationToken cancellationToken = default)
        => _inner.GetIncomingInvoicePdfAsync(uuid, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<string>> GetIncomingInvoiceXmlAsync(
        string uuid,
        CancellationToken cancellationToken = default)
        => _inner.GetIncomingInvoiceXmlAsync(uuid, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<AutoRouteResult>> SendInvoiceAutoAsync(
        InvoiceRequest invoice,
        AutoRouteOptions? options = null,
        CancellationToken cancellationToken = default)
        => _inner.SendInvoiceAutoAsync(invoice, options, cancellationToken);

    /// <inheritdoc />
    public Task<ApiResponse<List<BulkStatusResult>>> GetBulkStatusAsync(
        BulkStatusQuery query,
        BulkStatusOptions? options = null,
        CancellationToken cancellationToken = default)
        => _inner.GetBulkStatusAsync(query, options, cancellationToken);

    /// <summary>
    /// Invalidates cached status for a specific invoice
    /// </summary>
    public void InvalidateInvoiceStatus(string uuid)
    {
        _cache.Remove($"{InvoiceStatusPrefix}{uuid}");
    }

    /// <summary>
    /// Invalidates cached status for a specific dispatch
    /// </summary>
    public void InvalidateDispatchStatus(string uuid)
    {
        _cache.Remove($"{DispatchStatusPrefix}{uuid}");
    }

    /// <summary>
    /// Invalidates cached user check
    /// </summary>
    public void InvalidateUserCheck(string taxId)
    {
        _cache.Remove($"{UserCheckPrefix}{taxId}");
    }

    private void ConfigureCacheEntry(ICacheEntry entry, TimeSpan duration)
    {
        if (_options.UseSlidingExpiration)
        {
            entry.SlidingExpiration = duration;
        }
        else
        {
            entry.AbsoluteExpirationRelativeToNow = duration;
        }
    }
}
