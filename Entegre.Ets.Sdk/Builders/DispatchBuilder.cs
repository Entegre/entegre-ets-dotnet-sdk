using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Dispatch;

namespace Entegre.Ets.Sdk.Builders;

/// <summary>
/// Fluent builder for creating dispatches (E-İrsaliye)
/// </summary>
public class DispatchBuilder
{
    private readonly DispatchRequest _dispatch = new();

    /// <summary>
    /// Creates a new dispatch builder
    /// </summary>
    public static DispatchBuilder Create() => new();

    /// <summary>
    /// Sets the dispatch UUID
    /// </summary>
    public DispatchBuilder WithUuid(string uuid)
    {
        _dispatch.Uuid = uuid;
        return this;
    }

    /// <summary>
    /// Sets the dispatch number
    /// </summary>
    public DispatchBuilder WithNumber(string number)
    {
        _dispatch.DispatchNumber = number;
        return this;
    }

    /// <summary>
    /// Sets the issue date
    /// </summary>
    public DispatchBuilder WithDate(DateTime date)
    {
        _dispatch.IssueDate = date;
        return this;
    }

    /// <summary>
    /// Sets the dispatch type
    /// </summary>
    public DispatchBuilder WithType(DispatchType type)
    {
        _dispatch.DispatchType = type;
        return this;
    }

    /// <summary>
    /// Sets the sender information
    /// </summary>
    public DispatchBuilder WithSender(string taxId, string name, Action<PartyBuilder>? configure = null)
    {
        var builder = new PartyBuilder(taxId, name);
        configure?.Invoke(builder);
        _dispatch.Sender = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the sender from existing party
    /// </summary>
    public DispatchBuilder WithSender(Party sender)
    {
        _dispatch.Sender = sender;
        return this;
    }

    /// <summary>
    /// Sets the receiver information
    /// </summary>
    public DispatchBuilder WithReceiver(string taxId, string name, Action<PartyBuilder>? configure = null)
    {
        var builder = new PartyBuilder(taxId, name);
        configure?.Invoke(builder);
        _dispatch.Receiver = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the receiver from existing party
    /// </summary>
    public DispatchBuilder WithReceiver(Party receiver)
    {
        _dispatch.Receiver = receiver;
        return this;
    }

    /// <summary>
    /// Sets shipment information
    /// </summary>
    public DispatchBuilder WithShipment(Action<ShipmentBuilder> configure)
    {
        var builder = new ShipmentBuilder();
        configure(builder);
        _dispatch.Shipment = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets shipment from existing info
    /// </summary>
    public DispatchBuilder WithShipment(ShipmentInfo shipment)
    {
        _dispatch.Shipment = shipment;
        return this;
    }

    /// <summary>
    /// Adds a dispatch line
    /// </summary>
    public DispatchBuilder AddLine(string name, decimal quantity, string unitCode = "C62", string? description = null)
    {
        _dispatch.Lines.Add(new DispatchLine
        {
            Name = name,
            Quantity = quantity,
            UnitCode = unitCode,
            Description = description
        });
        return this;
    }

    /// <summary>
    /// Adds an existing dispatch line
    /// </summary>
    public DispatchBuilder AddLine(DispatchLine line)
    {
        _dispatch.Lines.Add(line);
        return this;
    }

    /// <summary>
    /// Adds a note
    /// </summary>
    public DispatchBuilder WithNote(string note)
    {
        _dispatch.Notes ??= [];
        _dispatch.Notes.Add(note);
        return this;
    }

    /// <summary>
    /// Builds the dispatch request
    /// </summary>
    public DispatchRequest Build()
    {
        _dispatch.Uuid ??= Guid.NewGuid().ToString();
        return _dispatch;
    }
}

/// <summary>
/// Builder for shipment information
/// </summary>
public class ShipmentBuilder
{
    private readonly ShipmentInfo _shipment = new();

    /// <summary>
    /// Sets the shipment date
    /// </summary>
    public ShipmentBuilder WithDate(DateTime date)
    {
        _shipment.ShipmentDate = date;
        return this;
    }

    /// <summary>
    /// Sets the vehicle plate number
    /// </summary>
    public ShipmentBuilder WithVehicle(string plateNumber)
    {
        _shipment.VehiclePlate = plateNumber;
        return this;
    }

    /// <summary>
    /// Sets the driver information
    /// </summary>
    public ShipmentBuilder WithDriver(string firstName, string lastName, string tckn)
    {
        _shipment.Driver = new DriverInfo
        {
            Name = firstName,
            Surname = lastName,
            Tckn = tckn
        };
        return this;
    }

    /// <summary>
    /// Sets the carrier information
    /// </summary>
    public ShipmentBuilder WithCarrier(string taxId, string name)
    {
        _shipment.Carrier = new CarrierInfo
        {
            TaxId = taxId,
            Name = name
        };
        return this;
    }

    /// <summary>
    /// Sets the delivery address
    /// </summary>
    public ShipmentBuilder WithDeliveryAddress(string city, string? street = null, string? district = null)
    {
        _shipment.DeliveryAddress = new Address
        {
            City = city,
            Street = street,
            District = district
        };
        return this;
    }

    /// <summary>
    /// Builds the shipment info
    /// </summary>
    public ShipmentInfo Build() => _shipment;
}
