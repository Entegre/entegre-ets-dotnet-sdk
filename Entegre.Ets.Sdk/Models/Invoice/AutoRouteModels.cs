using System.Text.Json.Serialization;

namespace Entegre.Ets.Sdk.Models.Invoice;

/// <summary>
/// Document type for routing
/// </summary>
public enum RouteDocumentType
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

/// <summary>
/// Auto-routing result
/// </summary>
public class AutoRouteResult
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
    /// Document type used
    /// </summary>
    [JsonPropertyName("documentType")]
    public RouteDocumentType DocumentType { get; set; }

    /// <summary>
    /// Whether recipient is an E-Invoice user
    /// </summary>
    [JsonPropertyName("isEInvoiceRecipient")]
    public bool IsEInvoiceRecipient { get; set; }

    /// <summary>
    /// Invoice send result
    /// </summary>
    [JsonPropertyName("result")]
    public InvoiceResult Result { get; set; } = new();
}

/// <summary>
/// Auto-routing options
/// </summary>
public class AutoRouteOptions
{
    /// <summary>
    /// Skip cache and re-check E-Invoice status
    /// </summary>
    public bool SkipCache { get; set; }

    /// <summary>
    /// Force document type (disables auto-routing)
    /// </summary>
    public RouteDocumentType? ForceType { get; set; }

    /// <summary>
    /// E-Archive sending type (default: Elektronik)
    /// </summary>
    public ArchiveSendingType ArchiveSendingType { get; set; } = ArchiveSendingType.Elektronik;

    /// <summary>
    /// Is this an internet sale?
    /// </summary>
    public bool IsInternetSales { get; set; }
}

/// <summary>
/// E-Archive sending type
/// </summary>
public enum ArchiveSendingType
{
    /// <summary>
    /// Electronic delivery
    /// </summary>
    Elektronik,

    /// <summary>
    /// Paper delivery
    /// </summary>
    Kagit
}
