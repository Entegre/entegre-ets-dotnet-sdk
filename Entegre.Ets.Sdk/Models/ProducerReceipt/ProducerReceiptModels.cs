using System.Text.Json.Serialization;
using Entegre.Ets.Sdk.Models.Common;

namespace Entegre.Ets.Sdk.Models.ProducerReceipt;

/// <summary>
/// Producer Receipt (E-Müstahsil Makbuzu) request model
/// </summary>
public class ProducerReceiptRequest
{
    /// <summary>
    /// Receipt UUID (auto-generated if empty)
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// Receipt number (auto-generated if empty)
    /// </summary>
    [JsonPropertyName("receiptNumber")]
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Issue date
    /// </summary>
    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Currency code (default: TRY)
    /// </summary>
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "TRY";

    /// <summary>
    /// Buyer (mükelef) information
    /// </summary>
    [JsonPropertyName("buyer")]
    public Party Buyer { get; set; } = new();

    /// <summary>
    /// Producer (müstahsil) information
    /// </summary>
    [JsonPropertyName("producer")]
    public ProducerInfo Producer { get; set; } = new();

    /// <summary>
    /// Receipt lines
    /// </summary>
    [JsonPropertyName("lines")]
    public List<ProducerReceiptLine> Lines { get; set; } = [];

    /// <summary>
    /// Notes
    /// </summary>
    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }
}

/// <summary>
/// Producer information
/// </summary>
public class ProducerInfo
{
    /// <summary>
    /// Producer TCKN
    /// </summary>
    [JsonPropertyName("tckn")]
    public string Tckn { get; set; } = string.Empty;

    /// <summary>
    /// First name
    /// </summary>
    [JsonPropertyName("firstName")]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// Last name
    /// </summary>
    [JsonPropertyName("lastName")]
    public string LastName { get; set; } = string.Empty;

    /// <summary>
    /// Address
    /// </summary>
    [JsonPropertyName("address")]
    public Address? Address { get; set; }
}

/// <summary>
/// Producer receipt line item
/// </summary>
public class ProducerReceiptLine
{
    /// <summary>
    /// Product name
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Quantity
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit code
    /// </summary>
    [JsonPropertyName("unitCode")]
    public string UnitCode { get; set; } = "KGM";

    /// <summary>
    /// Unit price
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Stopaj (withholding) rate
    /// </summary>
    [JsonPropertyName("stopajRate")]
    public decimal StopajRate { get; set; } = 2;
}

/// <summary>
/// Producer receipt result
/// </summary>
public class ProducerReceiptResult
{
    /// <summary>
    /// Receipt UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Receipt number
    /// </summary>
    [JsonPropertyName("receiptNumber")]
    public string? ReceiptNumber { get; set; }

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
}

/// <summary>
/// Producer receipt status result
/// </summary>
public class ProducerReceiptStatusResult
{
    /// <summary>
    /// Receipt UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Receipt number
    /// </summary>
    [JsonPropertyName("receiptNumber")]
    public string? ReceiptNumber { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Status description
    /// </summary>
    [JsonPropertyName("statusDescription")]
    public string? StatusDescription { get; set; }
}
