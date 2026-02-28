using System.Text.Json.Serialization;
using Entegre.Ets.Sdk.Models.Common;

namespace Entegre.Ets.Sdk.Models.Invoice;

/// <summary>
/// Invoice request model
/// </summary>
public class InvoiceRequest
{
    /// <summary>
    /// Invoice UUID (auto-generated if empty)
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// Invoice number (auto-generated if empty)
    /// </summary>
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Invoice date
    /// </summary>
    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Invoice type
    /// </summary>
    [JsonPropertyName("invoiceType")]
    public InvoiceType InvoiceType { get; set; } = InvoiceType.Satis;

    /// <summary>
    /// Document type (EFATURA, EARŞIV, etc.)
    /// </summary>
    [JsonPropertyName("documentType")]
    public DocumentType DocumentType { get; set; } = DocumentType.EFatura;

    /// <summary>
    /// Currency code (default: TRY)
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Exchange rate (for foreign currency)
    /// </summary>
    [JsonPropertyName("exchangeRate")]
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Sender information
    /// </summary>
    [JsonPropertyName("sender")]
    public Party Sender { get; set; } = new();

    /// <summary>
    /// Receiver information
    /// </summary>
    [JsonPropertyName("receiver")]
    public Party Receiver { get; set; } = new();

    /// <summary>
    /// Invoice lines
    /// </summary>
    [JsonPropertyName("lines")]
    public List<InvoiceLine> Lines { get; set; } = [];

    /// <summary>
    /// Notes
    /// </summary>
    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }

    /// <summary>
    /// Payment terms
    /// </summary>
    [JsonPropertyName("paymentTerms")]
    public PaymentTerms? PaymentTerms { get; set; }

    /// <summary>
    /// Withholding information
    /// </summary>
    [JsonPropertyName("withholding")]
    public WithholdingInfo? Withholding { get; set; }
}

/// <summary>
/// Invoice line item
/// </summary>
public class InvoiceLine
{
    /// <summary>
    /// Product/service name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit code (C62=Adet, KGM=Kilogram, etc.)
    /// </summary>
    [JsonPropertyName("unitCode")]
    public string UnitCode { get; set; } = "C62";

    /// <summary>
    /// Unit price
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// VAT rate (KDV oranı)
    /// </summary>
    [JsonPropertyName("vatRate")]
    public decimal VatRate { get; set; } = 20;

    /// <summary>
    /// Discount amount
    /// </summary>
    [JsonPropertyName("discountAmount")]
    public decimal? DiscountAmount { get; set; }

    /// <summary>
    /// Discount rate (percentage)
    /// </summary>
    [JsonPropertyName("discountRate")]
    public decimal? DiscountRate { get; set; }

    /// <summary>
    /// Product description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Payment terms
/// </summary>
public class PaymentTerms
{
    /// <summary>
    /// Payment due date
    /// </summary>
    [JsonPropertyName("dueDate")]
    public DateTime? DueDate { get; set; }

    /// <summary>
    /// Payment note
    /// </summary>
    [JsonPropertyName("note")]
    public string? Note { get; set; }

    /// <summary>
    /// IBAN
    /// </summary>
    [JsonPropertyName("iban")]
    public string? Iban { get; set; }
}

/// <summary>
/// Withholding tax information
/// </summary>
public class WithholdingInfo
{
    /// <summary>
    /// Withholding rate (e.g., 0.20 for 2/10)
    /// </summary>
    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    /// <summary>
    /// Withholding reason code
    /// </summary>
    [JsonPropertyName("reasonCode")]
    public string? ReasonCode { get; set; }

    /// <summary>
    /// Withholding reason description
    /// </summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; set; }
}

/// <summary>
/// Invoice result
/// </summary>
public class InvoiceResult
{
    /// <summary>
    /// Invoice UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Invoice number
    /// </summary>
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    /// <summary>
    /// Status description
    /// </summary>
    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// Processing date
    /// </summary>
    [JsonPropertyName("processDate")]
    public DateTime? ProcessDate { get; set; }
}

/// <summary>
/// Invoice status request
/// </summary>
public class InvoiceStatusRequest
{
    /// <summary>
    /// Invoice UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// Invoice number
    /// </summary>
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }
}

/// <summary>
/// Invoice status result
/// </summary>
public class InvoiceStatusResult
{
    /// <summary>
    /// Invoice UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Invoice number
    /// </summary>
    [JsonPropertyName("invoiceNumber")]
    public string? InvoiceNumber { get; set; }

    /// <summary>
    /// Status code
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status description
    /// </summary>
    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; }

    /// <summary>
    /// GIB status code
    /// </summary>
    [JsonPropertyName("gibStatusCode")]
    public string? GibStatusCode { get; set; }

    /// <summary>
    /// GIB status description
    /// </summary>
    [JsonPropertyName("gibStatusDescription")]
    public string? GibStatusDescription { get; set; }
}

/// <summary>
/// E-Invoice user check request
/// </summary>
public class EInvoiceUserRequest
{
    /// <summary>
    /// Tax/Identity number to check
    /// </summary>
    [JsonPropertyName("taxId")]
    public string TaxId { get; set; } = string.Empty;
}

/// <summary>
/// E-Invoice user check result
/// </summary>
public class EInvoiceUserResult
{
    /// <summary>
    /// Tax/Identity number
    /// </summary>
    [JsonPropertyName("taxId")]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Whether the user is registered for E-Invoice
    /// </summary>
    [JsonPropertyName("isEInvoiceUser")]
    public bool IsEInvoiceUser { get; set; }

    /// <summary>
    /// User title
    /// </summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>
    /// Aliases/Posta kutulari
    /// </summary>
    [JsonPropertyName("aliases")]
    public List<string>? Aliases { get; set; }

    /// <summary>
    /// Registration date
    /// </summary>
    [JsonPropertyName("registrationDate")]
    public DateTime? RegistrationDate { get; set; }
}

/// <summary>
/// Invoice types
/// </summary>
public enum InvoiceType
{
    /// <summary>
    /// Satış faturası
    /// </summary>
    Satis,

    /// <summary>
    /// İade faturası
    /// </summary>
    Iade,

    /// <summary>
    /// Tevkifat faturası
    /// </summary>
    Tevkifat,

    /// <summary>
    /// İstisna faturası
    /// </summary>
    Istisna,

    /// <summary>
    /// İhraç kayıtlı fatura
    /// </summary>
    IhracKayitli,

    /// <summary>
    /// Özel matrah faturası
    /// </summary>
    OzelMatrah,

    /// <summary>
    /// SGK faturası
    /// </summary>
    SGK
}

/// <summary>
/// Document types
/// </summary>
public enum DocumentType
{
    /// <summary>
    /// E-Fatura
    /// </summary>
    EFatura,

    /// <summary>
    /// E-Arşiv
    /// </summary>
    EArsiv
}
