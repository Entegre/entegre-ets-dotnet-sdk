using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Invoice;
using Entegre.Ets.Sdk.Models.Dispatch;
using Entegre.Ets.Sdk.Models.ProducerReceipt;
using Entegre.Ets.Sdk.Models.Incoming;

namespace Entegre.Ets.Sdk.Logging;

/// <summary>
/// ETS client wrapper with logging support
/// </summary>
public class LoggingEtsClient : IEtsClient
{
    private readonly IEtsClient _inner;
    private readonly ILogger<LoggingEtsClient> _logger;

    /// <summary>
    /// Creates a new logging ETS client
    /// </summary>
    public LoggingEtsClient(IEtsClient inner, ILogger<LoggingEtsClient> logger)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceResult>> SendInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogRequestStarted("POST", "invoice/send");

        try
        {
            var result = await _inner.SendInvoiceAsync(invoice, cancellationToken);
            sw.Stop();

            if (result.Success)
            {
                _logger.LogRequestCompleted("POST", "invoice/send", 200, sw.ElapsedMilliseconds);
                _logger.LogInvoiceSent(result.Data?.Uuid ?? invoice.Uuid ?? "N/A", result.Data?.InvoiceNumber);
            }
            else
            {
                _logger.LogRequestFailed("POST", "invoice/send", result.Message ?? "Unknown error");
                _logger.LogInvoiceFailed(invoice.Uuid, result.Message ?? "Unknown error");
            }

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogRequestFailed("POST", "invoice/send", ex.Message, ex);
            _logger.LogInvoiceFailed(invoice.Uuid, ex.Message, ex);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<InvoiceResult>>> SendInvoicesAsync(
        IEnumerable<InvoiceRequest> invoices,
        CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        var count = invoices.Count();
        _logger.LogDebug("Sending {Count} invoices in batch", count);

        try
        {
            var result = await _inner.SendInvoicesAsync(invoices, cancellationToken);
            sw.Stop();

            _logger.LogInformation(
                "Batch invoice send completed: {Successful}/{Total} successful, Duration: {Duration}ms",
                result.Data?.Count ?? 0, count, sw.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Batch invoice send failed after {Duration}ms", sw.ElapsedMilliseconds);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceResult>> SendDraftInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", "invoice/draft",
            () => _inner.SendDraftInvoiceAsync(invoice, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceStatusResult>> GetInvoiceStatusAsync(
        InvoiceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"invoice/status?uuid={request.Uuid}",
            () => _inner.GetInvoiceStatusAsync(request, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<EInvoiceUserResult>> CheckEInvoiceUserAsync(
        string taxId,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"invoice/check-user?taxId={taxId}",
            () => _inner.CheckEInvoiceUserAsync(taxId, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchResult>> SendDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", "dispatch/send",
            () => _inner.SendDispatchAsync(dispatch, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<DispatchResult>>> SendDispatchesAsync(
        IEnumerable<DispatchRequest> dispatches,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", "dispatch/send (batch)",
            () => _inner.SendDispatchesAsync(dispatches, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchResult>> SendDraftDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", "dispatch/draft",
            () => _inner.SendDraftDispatchAsync(dispatch, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<DispatchStatusResult>> GetDispatchStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"dispatch/status?uuid={uuid}",
            () => _inner.GetDispatchStatusAsync(uuid, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<ProducerReceiptResult>> SendProducerReceiptAsync(
        ProducerReceiptRequest receipt,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", "producer-receipt/send",
            () => _inner.SendProducerReceiptAsync(receipt, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<List<ProducerReceiptResult>>> SendProducerReceiptsAsync(
        IEnumerable<ProducerReceiptRequest> receipts,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", "producer-receipt/send (batch)",
            () => _inner.SendProducerReceiptsAsync(receipts, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<ProducerReceiptStatusResult>> GetProducerReceiptStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"producer-receipt/status?uuid={uuid}",
            () => _inner.GetProducerReceiptStatusAsync(uuid, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IncomingInvoiceListResponse>> GetIncomingInvoicesAsync(
        IncomingInvoiceListRequest request,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", "incoming/invoices",
            () => _inner.GetIncomingInvoicesAsync(request, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<IncomingInvoice>> GetIncomingInvoiceAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"incoming/invoice?uuid={uuid}",
            () => _inner.GetIncomingInvoiceAsync(uuid, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceResponseResult>> AcceptInvoiceAsync(
        string uuid,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", $"incoming/respond (accept) uuid={uuid}",
            () => _inner.AcceptInvoiceAsync(uuid, note, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<InvoiceResponseResult>> RejectInvoiceAsync(
        string uuid,
        string reason,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "POST", $"incoming/respond (reject) uuid={uuid}",
            () => _inner.RejectInvoiceAsync(uuid, reason, note, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<byte[]>> GetIncomingInvoicePdfAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"incoming/invoice/pdf?uuid={uuid}",
            () => _inner.GetIncomingInvoicePdfAsync(uuid, cancellationToken));
    }

    /// <inheritdoc />
    public async Task<ApiResponse<string>> GetIncomingInvoiceXmlAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        return await LoggedOperationAsync(
            "GET", $"incoming/invoice/xml?uuid={uuid}",
            () => _inner.GetIncomingInvoiceXmlAsync(uuid, cancellationToken));
    }

    private async Task<ApiResponse<T>> LoggedOperationAsync<T>(
        string method,
        string endpoint,
        Func<Task<ApiResponse<T>>> operation)
    {
        var sw = Stopwatch.StartNew();
        _logger.LogRequestStarted(method, endpoint);

        try
        {
            var result = await operation();
            sw.Stop();

            if (result.Success)
            {
                _logger.LogRequestCompleted(method, endpoint, 200, sw.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogRequestFailed(method, endpoint, result.Message ?? "Unknown error");
            }

            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogRequestFailed(method, endpoint, ex.Message, ex);
            throw;
        }
    }
}
