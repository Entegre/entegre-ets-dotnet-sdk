using System.Text.Json.Serialization;
using Entegre.Ets.Sdk.Models.Common;

namespace Entegre.Ets.Sdk.Models.Dispatch;

/// <summary>
/// Dispatch (E-İrsaliye) request model
/// </summary>
public class DispatchRequest
{
    /// <summary>
    /// Dispatch UUID (auto-generated if empty)
    /// </summary>
    [JsonPropertyName("uuid")]
    public string? Uuid { get; set; }

    /// <summary>
    /// Dispatch number (auto-generated if empty)
    /// </summary>
    [JsonPropertyName("dispatchNumber")]
    public string? DispatchNumber { get; set; }

    /// <summary>
    /// Dispatch date
    /// </summary>
    [JsonPropertyName("issueDate")]
    public DateTime IssueDate { get; set; } = DateTime.Now;

    /// <summary>
    /// Dispatch type
    /// </summary>
    [JsonPropertyName("dispatchType")]
    public DispatchType DispatchType { get; set; } = DispatchType.Sevk;

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
    /// Shipment information
    /// </summary>
    [JsonPropertyName("shipment")]
    public ShipmentInfo? Shipment { get; set; }

    /// <summary>
    /// Dispatch lines
    /// </summary>
    [JsonPropertyName("lines")]
    public List<DispatchLine> Lines { get; set; } = [];

    /// <summary>
    /// Notes
    /// </summary>
    [JsonPropertyName("notes")]
    public List<string>? Notes { get; set; }
}

/// <summary>
/// Dispatch line item
/// </summary>
public class DispatchLine
{
    /// <summary>
    /// Product/item name
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
    public string UnitCode { get; set; } = "C62";

    /// <summary>
    /// Description
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
}

/// <summary>
/// Shipment information
/// </summary>
public class ShipmentInfo
{
    /// <summary>
    /// Shipment date
    /// </summary>
    [JsonPropertyName("shipmentDate")]
    public DateTime? ShipmentDate { get; set; }

    /// <summary>
    /// Carrier information
    /// </summary>
    [JsonPropertyName("carrier")]
    public CarrierInfo? Carrier { get; set; }

    /// <summary>
    /// Delivery address
    /// </summary>
    [JsonPropertyName("deliveryAddress")]
    public Address? DeliveryAddress { get; set; }

    /// <summary>
    /// Driver information
    /// </summary>
    [JsonPropertyName("driver")]
    public DriverInfo? Driver { get; set; }

    /// <summary>
    /// Vehicle plate number
    /// </summary>
    [JsonPropertyName("vehiclePlate")]
    public string? VehiclePlate { get; set; }
}

/// <summary>
/// Carrier information
/// </summary>
public class CarrierInfo
{
    /// <summary>
    /// Carrier tax ID
    /// </summary>
    [JsonPropertyName("taxId")]
    public string? TaxId { get; set; }

    /// <summary>
    /// Carrier name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

/// <summary>
/// Driver information
/// </summary>
public class DriverInfo
{
    /// <summary>
    /// Driver name
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// Driver surname
    /// </summary>
    [JsonPropertyName("surname")]
    public string? Surname { get; set; }

    /// <summary>
    /// Driver TCKN
    /// </summary>
    [JsonPropertyName("tckn")]
    public string? Tckn { get; set; }
}

/// <summary>
/// Dispatch result
/// </summary>
public class DispatchResult
{
    /// <summary>
    /// Dispatch UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Dispatch number
    /// </summary>
    [JsonPropertyName("dispatchNumber")]
    public string? DispatchNumber { get; set; }

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
/// Dispatch status result
/// </summary>
public class DispatchStatusResult
{
    /// <summary>
    /// Dispatch UUID
    /// </summary>
    [JsonPropertyName("uuid")]
    public string Uuid { get; set; } = string.Empty;

    /// <summary>
    /// Dispatch number
    /// </summary>
    [JsonPropertyName("dispatchNumber")]
    public string? DispatchNumber { get; set; }

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

    /// <summary>
    /// Response status (KABUL/RED)
    /// </summary>
    [JsonPropertyName("responseStatus")]
    public string? ResponseStatus { get; set; }
}

/// <summary>
/// Dispatch types
/// </summary>
public enum DispatchType
{
    /// <summary>
    /// Sevk irsaliyesi
    /// </summary>
    Sevk,

    /// <summary>
    /// Satış irsaliyesi
    /// </summary>
    Satis,

    /// <summary>
    /// İade irsaliyesi
    /// </summary>
    Iade
}
