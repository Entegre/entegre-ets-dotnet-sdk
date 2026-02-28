using System.Collections.Concurrent;
using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Invoice;
using Entegre.Ets.Sdk.Models.Dispatch;
using Entegre.Ets.Sdk.Models.ProducerReceipt;
using Entegre.Ets.Sdk.Models.Incoming;
using Entegre.Ets.Sdk.Webhooks;
using Entegre.Ets.Sdk.ExchangeRate;

namespace Entegre.Ets.Sdk.Testing;

/// <summary>
/// Mock client options
/// </summary>
public class MockClientOptions
{
    /// <summary>
    /// Simulated network delay
    /// </summary>
    public TimeSpan SimulatedDelay { get; set; } = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Random failure rate (0.0 to 1.0)
    /// </summary>
    public double FailureRate { get; set; } = 0;

    /// <summary>
    /// Auto-accept invoices after sending
    /// </summary>
    public bool AutoAcceptInvoices { get; set; } = false;
}

/// <summary>
/// Mock ETS client for testing
/// </summary>
public class MockEtsClient : IEtsClient
{
    private readonly MockClientOptions _options;
    private readonly Random _random = new();

    private readonly ConcurrentDictionary<string, EInvoiceUserResult> _eInvoiceUsers = new();
    private readonly ConcurrentDictionary<string, InvoiceResult> _invoices = new();
    private readonly ConcurrentDictionary<string, string> _invoiceStatuses = new();
    private readonly ConcurrentDictionary<string, DispatchResult> _dispatches = new();
    private readonly ConcurrentDictionary<string, ProducerReceiptResult> _receipts = new();

    private readonly List<WebhookEvent> _webhookEvents = new();

    public MockEtsClient(MockClientOptions? options = null)
    {
        _options = options ?? new MockClientOptions();
    }

    #region Setup Methods

    /// <summary>
    /// Adds an E-Invoice user for testing
    /// </summary>
    public void AddEInvoiceUser(string taxId, string title, List<string>? aliases = null)
    {
        _eInvoiceUsers[taxId] = new EInvoiceUserResult
        {
            TaxId = taxId,
            IsEInvoiceUser = true,
            Title = title,
            Aliases = aliases ?? new List<string> { $"urn:mail:defaultpk@{taxId}" },
            RegistrationDate = DateTime.Now.AddYears(-1)
        };
    }

    /// <summary>
    /// Sets invoice status manually
    /// </summary>
    public void SetInvoiceStatus(string uuid, string status)
    {
        _invoiceStatuses[uuid] = status;
    }

    /// <summary>
    /// Gets all webhook events
    /// </summary>
    public IReadOnlyList<WebhookEvent> WebhookEvents => _webhookEvents.AsReadOnly();

    /// <summary>
    /// Simulates a webhook event
    /// </summary>
    public async Task SimulateWebhookAsync(string documentUuid, WebhookEventType eventType)
    {
        var webhookEvent = new WebhookEvent
        {
            Event = eventType.ToString().ToLowerInvariant().Replace("invoice", "invoice."),
            DocumentUuid = documentUuid,
            Timestamp = DateTime.UtcNow
        };

        _webhookEvents.Add(webhookEvent);

        if (eventType == WebhookEventType.InvoiceAccepted)
            _invoiceStatuses[documentUuid] = "ACCEPTED";
        else if (eventType == WebhookEventType.InvoiceRejected)
            _invoiceStatuses[documentUuid] = "REJECTED";

        await Task.CompletedTask;
    }

    /// <summary>
    /// Clears all test data
    /// </summary>
    public void Reset()
    {
        _eInvoiceUsers.Clear();
        _invoices.Clear();
        _invoiceStatuses.Clear();
        _dispatches.Clear();
        _receipts.Clear();
        _webhookEvents.Clear();
    }

    #endregion

    #region IEtsClient Implementation

    public async Task<ApiResponse<InvoiceResult>> SendInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (ShouldFail())
            return CreateErrorResponse<InvoiceResult>("Simulated failure");

        var uuid = invoice.Uuid ?? Guid.NewGuid().ToString();
        var result = new InvoiceResult
        {
            Uuid = uuid,
            InvoiceNumber = invoice.InvoiceNumber ?? GenerateInvoiceNumber(),
            Status = "SENT",
            StatusDescription = "Fatura gönderildi",
            ProcessDate = DateTime.Now
        };

        _invoices[uuid] = result;
        _invoiceStatuses[uuid] = "SENT";

        if (_options.AutoAcceptInvoices)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);
                _invoiceStatuses[uuid] = "ACCEPTED";
            });
        }

        return CreateSuccessResponse(result);
    }

    public async Task<ApiResponse<List<InvoiceResult>>> SendInvoicesAsync(
        IEnumerable<InvoiceRequest> invoices,
        CancellationToken cancellationToken = default)
    {
        var results = new List<InvoiceResult>();
        foreach (var invoice in invoices)
        {
            var result = await SendInvoiceAsync(invoice, cancellationToken);
            if (result.Data != null)
                results.Add(result.Data);
        }
        return CreateSuccessResponse(results);
    }

    public async Task<ApiResponse<InvoiceResult>> SendDraftInvoiceAsync(
        InvoiceRequest invoice,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (ShouldFail())
            return CreateErrorResponse<InvoiceResult>("Simulated failure");

        var uuid = invoice.Uuid ?? Guid.NewGuid().ToString();
        var result = new InvoiceResult
        {
            Uuid = uuid,
            InvoiceNumber = invoice.InvoiceNumber ?? GenerateInvoiceNumber(),
            Status = "DRAFT",
            StatusDescription = "Taslak kaydedildi",
            ProcessDate = DateTime.Now
        };

        _invoices[uuid] = result;
        _invoiceStatuses[uuid] = "DRAFT";

        return CreateSuccessResponse(result);
    }

    public async Task<ApiResponse<InvoiceStatusResult>> GetInvoiceStatusAsync(
        InvoiceStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        var uuid = request.Uuid ?? string.Empty;

        if (!_invoiceStatuses.TryGetValue(uuid, out var status))
            return CreateErrorResponse<InvoiceStatusResult>("Invoice not found");

        _invoices.TryGetValue(uuid, out var invoice);

        return CreateSuccessResponse(new InvoiceStatusResult
        {
            Uuid = uuid,
            InvoiceNumber = invoice?.InvoiceNumber,
            Status = status,
            StatusDescription = GetStatusDescription(status)
        });
    }

    public async Task<ApiResponse<EInvoiceUserResult>> CheckEInvoiceUserAsync(
        string taxId,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (_eInvoiceUsers.TryGetValue(taxId, out var user))
        {
            return CreateSuccessResponse(user);
        }

        return CreateSuccessResponse(new EInvoiceUserResult
        {
            TaxId = taxId,
            IsEInvoiceUser = false
        });
    }

    public async Task<ApiResponse<DispatchResult>> SendDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (ShouldFail())
            return CreateErrorResponse<DispatchResult>("Simulated failure");

        var uuid = dispatch.Uuid ?? Guid.NewGuid().ToString();
        var result = new DispatchResult
        {
            Uuid = uuid,
            DispatchNumber = dispatch.DispatchNumber ?? GenerateDispatchNumber(),
            Status = "SENT",
            StatusDescription = "İrsaliye gönderildi"
        };

        _dispatches[uuid] = result;

        return CreateSuccessResponse(result);
    }

    public async Task<ApiResponse<List<DispatchResult>>> SendDispatchesAsync(
        IEnumerable<DispatchRequest> dispatches,
        CancellationToken cancellationToken = default)
    {
        var results = new List<DispatchResult>();
        foreach (var dispatch in dispatches)
        {
            var result = await SendDispatchAsync(dispatch, cancellationToken);
            if (result.Data != null)
                results.Add(result.Data);
        }
        return CreateSuccessResponse(results);
    }

    public async Task<ApiResponse<DispatchResult>> SendDraftDispatchAsync(
        DispatchRequest dispatch,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        var uuid = dispatch.Uuid ?? Guid.NewGuid().ToString();
        var result = new DispatchResult
        {
            Uuid = uuid,
            DispatchNumber = dispatch.DispatchNumber ?? GenerateDispatchNumber(),
            Status = "DRAFT",
            StatusDescription = "Taslak kaydedildi"
        };

        _dispatches[uuid] = result;

        return CreateSuccessResponse(result);
    }

    public async Task<ApiResponse<DispatchStatusResult>> GetDispatchStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (!_dispatches.TryGetValue(uuid, out var dispatch))
            return CreateErrorResponse<DispatchStatusResult>("Dispatch not found");

        return CreateSuccessResponse(new DispatchStatusResult
        {
            Uuid = uuid,
            DispatchNumber = dispatch.DispatchNumber,
            Status = dispatch.Status ?? "SENT",
            StatusDescription = dispatch.StatusDescription
        });
    }

    public async Task<ApiResponse<ProducerReceiptResult>> SendProducerReceiptAsync(
        ProducerReceiptRequest receipt,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (ShouldFail())
            return CreateErrorResponse<ProducerReceiptResult>("Simulated failure");

        var uuid = receipt.Uuid ?? Guid.NewGuid().ToString();
        var result = new ProducerReceiptResult
        {
            Uuid = uuid,
            ReceiptNumber = receipt.ReceiptNumber ?? GenerateReceiptNumber(),
            Status = "SENT",
            StatusDescription = "Makbuz gönderildi"
        };

        _receipts[uuid] = result;

        return CreateSuccessResponse(result);
    }

    public async Task<ApiResponse<List<ProducerReceiptResult>>> SendProducerReceiptsAsync(
        IEnumerable<ProducerReceiptRequest> receipts,
        CancellationToken cancellationToken = default)
    {
        var results = new List<ProducerReceiptResult>();
        foreach (var receipt in receipts)
        {
            var result = await SendProducerReceiptAsync(receipt, cancellationToken);
            if (result.Data != null)
                results.Add(result.Data);
        }
        return CreateSuccessResponse(results);
    }

    public async Task<ApiResponse<ProducerReceiptStatusResult>> GetProducerReceiptStatusAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (!_receipts.TryGetValue(uuid, out var receipt))
            return CreateErrorResponse<ProducerReceiptStatusResult>("Receipt not found");

        return CreateSuccessResponse(new ProducerReceiptStatusResult
        {
            Uuid = uuid,
            ReceiptNumber = receipt.ReceiptNumber,
            Status = receipt.Status ?? "SENT",
            StatusDescription = receipt.StatusDescription
        });
    }

    #endregion

    #region Incoming Invoice Operations

    private readonly ConcurrentDictionary<string, IncomingInvoice> _incomingInvoices = new();

    /// <summary>
    /// Adds an incoming invoice for testing
    /// </summary>
    public void AddIncomingInvoice(IncomingInvoice invoice)
    {
        _incomingInvoices[invoice.Uuid] = invoice;
    }

    public async Task<ApiResponse<IncomingInvoiceListResponse>> GetIncomingInvoicesAsync(
        IncomingInvoiceListRequest request,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        var invoices = _incomingInvoices.Values.ToList();

        if (request.StartDate.HasValue)
            invoices = invoices.Where(i => i.InvoiceDate >= request.StartDate.Value).ToList();

        if (request.EndDate.HasValue)
            invoices = invoices.Where(i => i.InvoiceDate <= request.EndDate.Value).ToList();

        if (!string.IsNullOrEmpty(request.SenderTaxId))
            invoices = invoices.Where(i => i.Sender.TaxId == request.SenderTaxId).ToList();

        if (!string.IsNullOrEmpty(request.Status))
            invoices = invoices.Where(i => i.Status == request.Status).ToList();

        var total = invoices.Count;
        var paged = invoices
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return CreateSuccessResponse(new IncomingInvoiceListResponse
        {
            Invoices = paged,
            TotalCount = total,
            Page = request.Page,
            PageSize = request.PageSize
        });
    }

    public async Task<ApiResponse<IncomingInvoice>> GetIncomingInvoiceAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (_incomingInvoices.TryGetValue(uuid, out var invoice))
        {
            return CreateSuccessResponse(invoice);
        }

        return CreateErrorResponse<IncomingInvoice>("Incoming invoice not found");
    }

    public async Task<ApiResponse<InvoiceResponseResult>> AcceptInvoiceAsync(
        string uuid,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (_incomingInvoices.TryGetValue(uuid, out var invoice))
        {
            invoice.Status = IncomingInvoiceStatus.Accepted;
            invoice.StatusDate = DateTime.UtcNow;

            return CreateSuccessResponse(new InvoiceResponseResult
            {
                Uuid = uuid,
                ResponseType = "KABUL",
                ResponseDate = DateTime.UtcNow
            });
        }

        return CreateErrorResponse<InvoiceResponseResult>("Incoming invoice not found");
    }

    public async Task<ApiResponse<InvoiceResponseResult>> RejectInvoiceAsync(
        string uuid,
        string reason,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (_incomingInvoices.TryGetValue(uuid, out var invoice))
        {
            invoice.Status = IncomingInvoiceStatus.Rejected;
            invoice.StatusDate = DateTime.UtcNow;

            return CreateSuccessResponse(new InvoiceResponseResult
            {
                Uuid = uuid,
                ResponseType = "RED",
                ResponseDate = DateTime.UtcNow
            });
        }

        return CreateErrorResponse<InvoiceResponseResult>("Incoming invoice not found");
    }

    public async Task<ApiResponse<byte[]>> GetIncomingInvoicePdfAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (_incomingInvoices.ContainsKey(uuid))
        {
            // Return a simple PDF-like byte array for testing
            var pdfBytes = System.Text.Encoding.UTF8.GetBytes("%PDF-1.4 Mock PDF Content");
            return CreateSuccessResponse(pdfBytes);
        }

        return CreateErrorResponse<byte[]>("Incoming invoice not found");
    }

    public async Task<ApiResponse<string>> GetIncomingInvoiceXmlAsync(
        string uuid,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (_incomingInvoices.ContainsKey(uuid))
        {
            // Return a simple XML for testing
            var xml = $"<?xml version=\"1.0\"?><Invoice><UUID>{uuid}</UUID></Invoice>";
            return CreateSuccessResponse(xml);
        }

        return CreateErrorResponse<string>("Incoming invoice not found");
    }

    #endregion

    #region Auto-routing Operations

    public async Task<ApiResponse<AutoRouteResult>> SendInvoiceAutoAsync(
        InvoiceRequest invoice,
        AutoRouteOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        if (ShouldFail())
            return CreateErrorResponse<AutoRouteResult>("Simulated failure");

        var recipientTaxId = invoice.Receiver?.TaxId ?? "";
        var isEInvoiceUser = _eInvoiceUsers.ContainsKey(recipientTaxId);

        var documentType = options?.ForceType ?? (isEInvoiceUser ? RouteDocumentType.EFatura : RouteDocumentType.EArsiv);

        var result = await SendInvoiceAsync(invoice, cancellationToken);

        if (!result.Success || result.Data == null)
            return CreateErrorResponse<AutoRouteResult>(result.Message ?? "Failed");

        return CreateSuccessResponse(new AutoRouteResult
        {
            Uuid = result.Data.Uuid,
            InvoiceNumber = result.Data.InvoiceNumber,
            DocumentType = documentType,
            IsEInvoiceRecipient = isEInvoiceUser,
            Result = result.Data
        });
    }

    public async Task<ApiResponse<List<BulkStatusResult>>> GetBulkStatusAsync(
        BulkStatusQuery query,
        BulkStatusOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        await SimulateDelayAsync();

        var results = new List<BulkStatusResult>();

        foreach (var uuid in query.Uuids)
        {
            if (_invoiceStatuses.TryGetValue(uuid, out var status))
            {
                _invoices.TryGetValue(uuid, out var invoice);
                results.Add(new BulkStatusResult
                {
                    Uuid = uuid,
                    DocumentType = RouteDocumentType.EFatura,
                    Status = new InvoiceStatusResult
                    {
                        Uuid = uuid,
                        InvoiceNumber = invoice?.InvoiceNumber,
                        Status = status,
                        StatusDescription = GetStatusDescription(status)
                    },
                    Success = true
                });
            }
            else
            {
                results.Add(new BulkStatusResult
                {
                    Uuid = uuid,
                    Success = false,
                    Error = "Document not found"
                });
            }
        }

        return CreateSuccessResponse(results);
    }

    #endregion

    #region Helpers

    private async Task SimulateDelayAsync()
    {
        if (_options.SimulatedDelay > TimeSpan.Zero)
            await Task.Delay(_options.SimulatedDelay);
    }

    private bool ShouldFail()
    {
        return _options.FailureRate > 0 && _random.NextDouble() < _options.FailureRate;
    }

    private static ApiResponse<T> CreateSuccessResponse<T>(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data
        };
    }

    private static ApiResponse<T> CreateErrorResponse<T>(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message
        };
    }

    private string GenerateInvoiceNumber()
    {
        return $"ABC{DateTime.Now:yyyy}{_random.Next(100000000, 999999999):D9}";
    }

    private string GenerateDispatchNumber()
    {
        return $"IRS{DateTime.Now:yyyy}{_random.Next(100000000, 999999999):D9}";
    }

    private string GenerateReceiptNumber()
    {
        return $"MUS{DateTime.Now:yyyy}{_random.Next(100000000, 999999999):D9}";
    }

    private static string GetStatusDescription(string status)
    {
        return status switch
        {
            "SENT" => "Gönderildi",
            "DELIVERED" => "İletildi",
            "ACCEPTED" => "Kabul edildi",
            "REJECTED" => "Reddedildi",
            "FAILED" => "Başarısız",
            "DRAFT" => "Taslak",
            _ => status
        };
    }

    #endregion
}

/// <summary>
/// Test fixtures and generators
/// </summary>
public static class TestFixtures
{
    private static readonly Random Random = new();

    /// <summary>
    /// Sample supplier
    /// </summary>
    public static Party Supplier => new()
    {
        TaxId = "1234567890",
        Name = "Test Satıcı Firma A.Ş.",
        TaxOffice = "Test VD",
        Address = new Address { City = "İstanbul", District = "Kadıköy" }
    };

    /// <summary>
    /// Sample customer
    /// </summary>
    public static Party Customer => new()
    {
        TaxId = "9876543210",
        Name = "Test Alıcı Firma Ltd.",
        TaxOffice = "Test VD",
        Address = new Address { City = "Ankara", District = "Çankaya" }
    };

    /// <summary>
    /// Sample invoice lines
    /// </summary>
    public static List<InvoiceLine> InvoiceLines =>
    [
        new() { Name = "Test Ürün 1", Quantity = 1, UnitPrice = 100, VatRate = 20 },
        new() { Name = "Test Ürün 2", Quantity = 2, UnitPrice = 50, VatRate = 20 }
    ];
}

/// <summary>
/// Test data generators
/// </summary>
public static class TestGenerators
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generates a random VKN
    /// </summary>
    public static string RandomVkn()
    {
        var digits = new int[10];
        for (var i = 0; i < 9; i++)
            digits[i] = Random.Next(0, 10);

        // Calculate checksum
        var sum = 0;
        for (var i = 0; i < 9; i++)
        {
            var tmp = (digits[i] + (9 - i)) % 10;
            sum += (tmp * (int)Math.Pow(2, 9 - i)) % 9;
            if (tmp != 0 && (tmp * (int)Math.Pow(2, 9 - i)) % 9 == 0)
                sum += 9;
        }
        digits[9] = (10 - (sum % 10)) % 10;

        return string.Concat(digits);
    }

    /// <summary>
    /// Generates a random TCKN
    /// </summary>
    public static string RandomTckn()
    {
        var digits = new int[11];
        digits[0] = Random.Next(1, 10); // Cannot start with 0

        for (var i = 1; i < 9; i++)
            digits[i] = Random.Next(0, 10);

        // Calculate checksums
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        digits[9] = ((oddSum * 7) - evenSum) % 10;
        if (digits[9] < 0) digits[9] += 10;

        digits[10] = digits.Take(10).Sum() % 10;

        return string.Concat(digits);
    }

    /// <summary>
    /// Generates a random Turkish IBAN
    /// </summary>
    public static string RandomIban()
    {
        var bankCode = Random.Next(0, 100).ToString("D5");
        var accountNumber = Random.Next(0, 999999999).ToString("D16");

        // Calculate check digits (simplified)
        var checkDigits = Random.Next(10, 99).ToString("D2");

        return $"TR{checkDigits}{bankCode}{accountNumber}";
    }

    /// <summary>
    /// Generates a random amount
    /// </summary>
    public static decimal RandomAmount(decimal min = 1, decimal max = 10000)
    {
        var range = (double)(max - min);
        return min + (decimal)(Random.NextDouble() * range);
    }

    /// <summary>
    /// Generates a random recent date
    /// </summary>
    public static DateTime RandomRecentDate(int daysBack = 30)
    {
        return DateTime.Now.AddDays(-Random.Next(0, daysBack));
    }
}
