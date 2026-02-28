using System.Text.Json.Serialization;
using Entegre.Ets.Sdk.Models.Invoice;

namespace Entegre.Ets.Sdk.Models.Common;

/// <summary>
/// Bulk status query
/// </summary>
public class BulkStatusQuery
{
    /// <summary>
    /// List of UUIDs to query
    /// </summary>
    public List<string> Uuids { get; set; } = new();

    /// <summary>
    /// Include E-Archive search (default: true)
    /// </summary>
    public bool IncludeEArchive { get; set; } = true;
}

/// <summary>
/// Bulk status query options
/// </summary>
public class BulkStatusOptions
{
    /// <summary>
    /// Maximum concurrent requests (default: 5)
    /// </summary>
    public int Concurrency { get; set; } = 5;

    /// <summary>
    /// Continue on error (default: true)
    /// </summary>
    public bool ContinueOnError { get; set; } = true;

    /// <summary>
    /// Number of retries per request (default: 1)
    /// </summary>
    public int Retries { get; set; } = 1;
}

/// <summary>
/// Single document status result
/// </summary>
public class BulkStatusResult
{
    /// <summary>
    /// Document UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Document type (where it was found)
    /// </summary>
    [JsonPropertyName("documentType")]
    public RouteDocumentType? DocumentType { get; set; }

    /// <summary>
    /// Status information
    /// </summary>
    [JsonPropertyName("status")]
    public InvoiceStatusResult? Status { get; set; }

    /// <summary>
    /// Whether the query was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Error message if failed
    /// </summary>
    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
