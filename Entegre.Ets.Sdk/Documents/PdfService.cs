using System.Text.Json.Serialization;

namespace Entegre.Ets.Sdk.Documents;

/// <summary>
/// PDF document result
/// </summary>
public class PdfResult
{
    /// <summary>
    /// Document UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// PDF content as Base64 string
    /// </summary>
    [JsonPropertyName("pdfContent")]
    public string? PdfContent { get; set; }

    /// <summary>
    /// File name
    /// </summary>
    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    /// <summary>
    /// Content type
    /// </summary>
    [JsonPropertyName("contentType")]
    public string ContentType { get; set; } = "application/pdf";
}

/// <summary>
/// PDF service interface
/// </summary>
public interface IPdfService
{
    /// <summary>
    /// Gets invoice PDF
    /// </summary>
    Task<PdfResult?> GetInvoicePdfAsync(string uuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets dispatch PDF
    /// </summary>
    Task<PdfResult?> GetDispatchPdfAsync(string uuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets producer receipt PDF
    /// </summary>
    Task<PdfResult?> GetProducerReceiptPdfAsync(string uuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets PDF as byte array
    /// </summary>
    Task<byte[]?> GetPdfBytesAsync(string uuid, string documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets PDF as stream
    /// </summary>
    Task<Stream?> GetPdfStreamAsync(string uuid, string documentType, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves PDF to file
    /// </summary>
    Task SavePdfAsync(string uuid, string documentType, string filePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// PDF utility extensions
/// </summary>
public static class PdfExtensions
{
    /// <summary>
    /// Converts Base64 PDF content to byte array
    /// </summary>
    public static byte[] ToBytes(this PdfResult pdfResult)
    {
        if (string.IsNullOrEmpty(pdfResult.PdfContent))
            return Array.Empty<byte>();

        return Convert.FromBase64String(pdfResult.PdfContent);
    }

    /// <summary>
    /// Converts Base64 PDF content to stream
    /// </summary>
    public static Stream ToStream(this PdfResult pdfResult)
    {
        return new MemoryStream(pdfResult.ToBytes());
    }

    /// <summary>
    /// Saves PDF content to file
    /// </summary>
    public static async Task SaveToFileAsync(this PdfResult pdfResult, string filePath)
    {
        var bytes = pdfResult.ToBytes();
        await File.WriteAllBytesAsync(filePath, bytes);
    }

    /// <summary>
    /// Gets suggested file name
    /// </summary>
    public static string GetFileName(this PdfResult pdfResult, string defaultPrefix = "document")
    {
        if (!string.IsNullOrEmpty(pdfResult.FileName))
            return pdfResult.FileName;

        return $"{defaultPrefix}_{pdfResult.Uuid}.pdf";
    }
}
