using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.ProducerReceipt;

namespace Entegre.Ets.Sdk.Builders;

/// <summary>
/// Fluent builder for creating producer receipts (E-Müstahsil Makbuzu)
/// </summary>
public class ProducerReceiptBuilder
{
    private readonly ProducerReceiptRequest _receipt = new();

    /// <summary>
    /// Creates a new producer receipt builder
    /// </summary>
    public static ProducerReceiptBuilder Create() => new();

    /// <summary>
    /// Sets the receipt UUID
    /// </summary>
    public ProducerReceiptBuilder WithUuid(string uuid)
    {
        _receipt.Uuid = uuid;
        return this;
    }

    /// <summary>
    /// Sets the receipt number
    /// </summary>
    public ProducerReceiptBuilder WithNumber(string number)
    {
        _receipt.ReceiptNumber = number;
        return this;
    }

    /// <summary>
    /// Sets the issue date
    /// </summary>
    public ProducerReceiptBuilder WithDate(DateTime date)
    {
        _receipt.IssueDate = date;
        return this;
    }

    /// <summary>
    /// Sets the currency
    /// </summary>
    public ProducerReceiptBuilder WithCurrency(string currency)
    {
        _receipt.Currency = currency;
        return this;
    }

    /// <summary>
    /// Sets the buyer (mükelef) information
    /// </summary>
    public ProducerReceiptBuilder WithBuyer(string taxId, string name, Action<PartyBuilder>? configure = null)
    {
        var builder = new PartyBuilder(taxId, name);
        configure?.Invoke(builder);
        _receipt.Buyer = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the buyer from existing party
    /// </summary>
    public ProducerReceiptBuilder WithBuyer(Party buyer)
    {
        _receipt.Buyer = buyer;
        return this;
    }

    /// <summary>
    /// Sets the producer (müstahsil) information
    /// </summary>
    public ProducerReceiptBuilder WithProducer(string tckn, string firstName, string lastName, Action<ProducerInfoBuilder>? configure = null)
    {
        var builder = new ProducerInfoBuilder(tckn, firstName, lastName);
        configure?.Invoke(builder);
        _receipt.Producer = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the producer from existing info
    /// </summary>
    public ProducerReceiptBuilder WithProducer(ProducerInfo producer)
    {
        _receipt.Producer = producer;
        return this;
    }

    /// <summary>
    /// Adds a receipt line
    /// </summary>
    public ProducerReceiptBuilder AddLine(
        string name,
        decimal quantity,
        decimal unitPrice,
        string unitCode = "KGM",
        decimal stopajRate = 2)
    {
        _receipt.Lines.Add(new ProducerReceiptLine
        {
            Name = name,
            Quantity = quantity,
            UnitPrice = unitPrice,
            UnitCode = unitCode,
            StopajRate = stopajRate
        });
        return this;
    }

    /// <summary>
    /// Adds an existing receipt line
    /// </summary>
    public ProducerReceiptBuilder AddLine(ProducerReceiptLine line)
    {
        _receipt.Lines.Add(line);
        return this;
    }

    /// <summary>
    /// Adds a note
    /// </summary>
    public ProducerReceiptBuilder WithNote(string note)
    {
        _receipt.Notes ??= [];
        _receipt.Notes.Add(note);
        return this;
    }

    /// <summary>
    /// Builds the receipt request
    /// </summary>
    public ProducerReceiptRequest Build()
    {
        _receipt.Uuid ??= Guid.NewGuid().ToString();
        return _receipt;
    }

    /// <summary>
    /// Builds and calculates totals
    /// </summary>
    public (ProducerReceiptRequest Receipt, ProducerReceiptTotals Totals) BuildWithTotals()
    {
        var receipt = Build();
        var totals = CalculateTotals();
        return (receipt, totals);
    }

    /// <summary>
    /// Calculates receipt totals
    /// </summary>
    public ProducerReceiptTotals CalculateTotals()
    {
        decimal grossTotal = 0;
        decimal totalStopaj = 0;

        foreach (var line in _receipt.Lines)
        {
            var lineTotal = line.Quantity * line.UnitPrice;
            var stopaj = lineTotal * line.StopajRate / 100;

            grossTotal += lineTotal;
            totalStopaj += stopaj;
        }

        var netTotal = grossTotal - totalStopaj;

        return new ProducerReceiptTotals
        {
            GrossTotal = Math.Round(grossTotal, 2),
            TotalStopaj = Math.Round(totalStopaj, 2),
            NetTotal = Math.Round(netTotal, 2)
        };
    }
}

/// <summary>
/// Calculated producer receipt totals
/// </summary>
public class ProducerReceiptTotals
{
    /// <summary>
    /// Gross total (before stopaj)
    /// </summary>
    public decimal GrossTotal { get; set; }

    /// <summary>
    /// Total stopaj (withholding) amount
    /// </summary>
    public decimal TotalStopaj { get; set; }

    /// <summary>
    /// Net total (after stopaj)
    /// </summary>
    public decimal NetTotal { get; set; }
}

/// <summary>
/// Builder for producer information
/// </summary>
public class ProducerInfoBuilder
{
    private readonly ProducerInfo _producer = new();

    public ProducerInfoBuilder(string tckn, string firstName, string lastName)
    {
        _producer.Tckn = tckn;
        _producer.FirstName = firstName;
        _producer.LastName = lastName;
    }

    /// <summary>
    /// Sets the producer address
    /// </summary>
    public ProducerInfoBuilder WithAddress(string city, string? district = null, string? street = null)
    {
        _producer.Address = new Address
        {
            City = city,
            District = district,
            Street = street
        };
        return this;
    }

    /// <summary>
    /// Builds the producer info
    /// </summary>
    public ProducerInfo Build() => _producer;
}
