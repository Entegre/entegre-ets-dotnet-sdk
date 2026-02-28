using System.Text.Json.Serialization;

namespace Entegre.Ets.Sdk.Models.Common;

/// <summary>
/// Generic API response wrapper
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class ApiResponse<T>
{
    /// <summary>
    /// Whether the request was successful
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Response message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Response data
    /// </summary>
    [JsonPropertyName("data")]
    public T? Data { get; set; }

    /// <summary>
    /// Error details (if any)
    /// </summary>
    [JsonPropertyName("errors")]
    public List<ApiError>? Errors { get; set; }
}

/// <summary>
/// API error detail
/// </summary>
public class ApiError
{
    /// <summary>
    /// Error code
    /// </summary>
    [JsonPropertyName("code")]
    public string? Code { get; set; }

    /// <summary>
    /// Error message
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Field that caused the error
    /// </summary>
    [JsonPropertyName("field")]
    public string? Field { get; set; }
}

/// <summary>
/// Party information (sender/receiver)
/// </summary>
public class Party
{
    /// <summary>
    /// Tax/Identity number
    /// </summary>
    [JsonPropertyName("taxId")]
    public string TaxId { get; set; } = string.Empty;

    /// <summary>
    /// Party name/title
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Tax office
    /// </summary>
    [JsonPropertyName("taxOffice")]
    public string? TaxOffice { get; set; }

    /// <summary>
    /// Address
    /// </summary>
    [JsonPropertyName("address")]
    public Address? Address { get; set; }

    /// <summary>
    /// Contact information
    /// </summary>
    [JsonPropertyName("contact")]
    public Contact? Contact { get; set; }
}

/// <summary>
/// Address information
/// </summary>
public class Address
{
    /// <summary>
    /// Street address
    /// </summary>
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    /// <summary>
    /// Building number
    /// </summary>
    [JsonPropertyName("buildingNumber")]
    public string? BuildingNumber { get; set; }

    /// <summary>
    /// District
    /// </summary>
    [JsonPropertyName("district")]
    public string? District { get; set; }

    /// <summary>
    /// City
    /// </summary>
    [JsonPropertyName("city")]
    public string City { get; set; } = string.Empty;

    /// <summary>
    /// Country (default: Türkiye)
    /// </summary>
    [JsonPropertyName("country")]
    public string Country { get; set; } = "Türkiye";

    /// <summary>
    /// Postal code
    /// </summary>
    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }
}

/// <summary>
/// Contact information
/// </summary>
public class Contact
{
    /// <summary>
    /// Phone number
    /// </summary>
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    /// <summary>
    /// Fax number
    /// </summary>
    [JsonPropertyName("fax")]
    public string? Fax { get; set; }

    /// <summary>
    /// Email address
    /// </summary>
    [JsonPropertyName("email")]
    public string? Email { get; set; }

    /// <summary>
    /// Website
    /// </summary>
    [JsonPropertyName("website")]
    public string? Website { get; set; }
}
