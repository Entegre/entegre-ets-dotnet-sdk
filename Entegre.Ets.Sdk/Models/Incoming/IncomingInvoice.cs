namespace Entegre.Ets.Sdk.Models.Incoming;

/// <summary>
/// Incoming invoice model
/// </summary>
public class IncomingInvoice
{
    /// <summary>
    /// Invoice UUID
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Invoice number
    /// </summary>
    public string InvoiceNumber { get; set; } = string.Empty;

    /// <summary>
    /// Invoice date
    /// </summary>
    public DateTime InvoiceDate { get; set; }

    /// <summary>
    /// Invoice type (SATIS, IADE, TEVKIFAT, etc.)
    /// </summary>
    public string InvoiceType { get; set; } = string.Empty;

    /// <summary>
    /// Document type (EFATURA, EARSIV)
    /// </summary>
    public string DocumentType { get; set; } = string.Empty;

    /// <summary>
    /// Sender information
    /// </summary>
    public IncomingParty Sender { get; set; } = new();

    /// <summary>
    /// Currency code
    /// </summary>
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Subtotal amount (excluding VAT)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Total VAT amount
    /// </summary>
    public decimal TotalVat { get; set; }

    /// <summary>
    /// Total payable amount
    /// </summary>
    public decimal PayableAmount { get; set; }

    /// <summary>
    /// Invoice status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status date
    /// </summary>
    public DateTime? StatusDate { get; set; }

    /// <summary>
    /// Response deadline
    /// </summary>
    public DateTime? ResponseDeadline { get; set; }

    /// <summary>
    /// Invoice lines
    /// </summary>
    public List<IncomingInvoiceLine> Lines { get; set; } = new();

    /// <summary>
    /// Notes
    /// </summary>
    public List<string> Notes { get; set; } = new();

    /// <summary>
    /// XML content (Base64 encoded)
    /// </summary>
    public string? XmlContent { get; set; }

    /// <summary>
    /// PDF content (Base64 encoded)
    /// </summary>
    public string? PdfContent { get; set; }

    /// <summary>
    /// Received date
    /// </summary>
    public DateTime ReceivedDate { get; set; }
}

/// <summary>
/// Incoming invoice party information
/// </summary>
public class IncomingParty
{
    /// <summary>
    /// Tax ID (VKN or TCKN)
    /// </summary>
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Party name/title
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tax office
    /// </summary>
    public string? TaxOffice { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    public string? Address { get; set; }

    /// <summary>
    /// City
    /// </summary>
    public string? City { get; set; }

    /// <summary>
    /// District
    /// </summary>
    public string? District { get; set; }

    /// <summary>
    /// Email
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Phone
    /// </summary>
    public string? Phone { get; set; }
}

/// <summary>
/// Incoming invoice line
/// </summary>
public class IncomingInvoiceLine
{
    /// <summary>
    /// Line number
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Product/service name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Quantity
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Unit code (C62, KGM, LTR, etc.)
    /// </summary>
    public string UnitCode { get; set; } = "C62";

    /// <summary>
    /// Unit price
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// VAT rate (percentage)
    /// </summary>
    public decimal VatRate { get; set; }

    /// <summary>
    /// VAT amount
    /// </summary>
    public decimal VatAmount { get; set; }

    /// <summary>
    /// Line total (excluding VAT)
    /// </summary>
    public decimal LineTotal { get; set; }

    /// <summary>
    /// Discount amount
    /// </summary>
    public decimal? DiscountAmount { get; set; }
}

/// <summary>
/// Request for listing incoming invoices
/// </summary>
public class IncomingInvoiceListRequest
{
    /// <summary>
    /// Start date filter
    /// </summary>
    public DateTime? StartDate { get; set; }

    /// <summary>
    /// End date filter
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Sender tax ID filter
    /// </summary>
    public string? SenderTaxId { get; set; }

    /// <summary>
    /// Status filter (WAITING, ACCEPTED, REJECTED)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Page number (1-based)
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Page size (default: 50, max: 100)
    /// </summary>
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Response for incoming invoice list
/// </summary>
public class IncomingInvoiceListResponse
{
    /// <summary>
    /// List of invoices
    /// </summary>
    public List<IncomingInvoice> Invoices { get; set; } = new();

    /// <summary>
    /// Total count
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Current page
    /// </summary>
    public int Page { get; set; }

    /// <summary>
    /// Page size
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Total pages
    /// </summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

/// <summary>
/// Request for accepting/rejecting an invoice
/// </summary>
public class InvoiceResponseRequest
{
    /// <summary>
    /// Invoice UUID
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Response type (KABUL or RED)
    /// </summary>
    public InvoiceResponseType ResponseType { get; set; }

    /// <summary>
    /// Rejection reason (required for RED)
    /// </summary>
    public string? Reason { get; set; }

    /// <summary>
    /// Additional notes
    /// </summary>
    public string? Note { get; set; }
}

/// <summary>
/// Invoice response type
/// </summary>
public enum InvoiceResponseType
{
    /// <summary>
    /// Accept invoice
    /// </summary>
    Kabul,

    /// <summary>
    /// Reject invoice
    /// </summary>
    Red
}

/// <summary>
/// Response for invoice accept/reject operation
/// </summary>
public class InvoiceResponseResult
{
    /// <summary>
    /// Invoice UUID
    /// </summary>
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Response type applied
    /// </summary>
    public string ResponseType { get; set; } = string.Empty;

    /// <summary>
    /// Response date
    /// </summary>
    public DateTime ResponseDate { get; set; }

    /// <summary>
    /// Envelope UUID (for tracking)
    /// </summary>
    public string? EnvelopeUuid { get; set; }
}

/// <summary>
/// Incoming invoice status constants
/// </summary>
public static class IncomingInvoiceStatus
{
    /// <summary>
    /// Waiting for response
    /// </summary>
    public const string Waiting = "WAITING";

    /// <summary>
    /// Accepted
    /// </summary>
    public const string Accepted = "ACCEPTED";

    /// <summary>
    /// Rejected
    /// </summary>
    public const string Rejected = "REJECTED";

    /// <summary>
    /// Auto-accepted (deadline passed)
    /// </summary>
    public const string AutoAccepted = "AUTO_ACCEPTED";
}
