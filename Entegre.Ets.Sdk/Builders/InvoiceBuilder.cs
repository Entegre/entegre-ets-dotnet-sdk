using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Invoice;

namespace Entegre.Ets.Sdk.Builders;

/// <summary>
/// Fluent builder for creating invoices
/// </summary>
public class InvoiceBuilder
{
    private readonly InvoiceRequest _invoice = new();

    /// <summary>
    /// Creates a new invoice builder
    /// </summary>
    public static InvoiceBuilder Create() => new();

    /// <summary>
    /// Sets the invoice UUID
    /// </summary>
    public InvoiceBuilder WithUuid(string uuid)
    {
        _invoice.Uuid = uuid;
        return this;
    }

    /// <summary>
    /// Sets the invoice number
    /// </summary>
    public InvoiceBuilder WithNumber(string number)
    {
        _invoice.InvoiceNumber = number;
        return this;
    }

    /// <summary>
    /// Sets the issue date
    /// </summary>
    public InvoiceBuilder WithDate(DateTime date)
    {
        _invoice.IssueDate = date;
        return this;
    }

    /// <summary>
    /// Sets the invoice type
    /// </summary>
    public InvoiceBuilder WithType(InvoiceType type)
    {
        _invoice.InvoiceType = type;
        return this;
    }

    /// <summary>
    /// Sets the document type
    /// </summary>
    public InvoiceBuilder WithDocumentType(DocumentType type)
    {
        _invoice.DocumentType = type;
        return this;
    }

    /// <summary>
    /// Sets the currency
    /// </summary>
    public InvoiceBuilder WithCurrency(string currency, decimal? exchangeRate = null)
    {
        _invoice.Currency = currency;
        _invoice.ExchangeRate = exchangeRate;
        return this;
    }

    /// <summary>
    /// Sets the sender information
    /// </summary>
    public InvoiceBuilder WithSender(string taxId, string name, Action<PartyBuilder>? configure = null)
    {
        var builder = new PartyBuilder(taxId, name);
        configure?.Invoke(builder);
        _invoice.Sender = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the sender from existing party
    /// </summary>
    public InvoiceBuilder WithSender(Party sender)
    {
        _invoice.Sender = sender;
        return this;
    }

    /// <summary>
    /// Sets the receiver information
    /// </summary>
    public InvoiceBuilder WithReceiver(string taxId, string name, Action<PartyBuilder>? configure = null)
    {
        var builder = new PartyBuilder(taxId, name);
        configure?.Invoke(builder);
        _invoice.Receiver = builder.Build();
        return this;
    }

    /// <summary>
    /// Sets the receiver from existing party
    /// </summary>
    public InvoiceBuilder WithReceiver(Party receiver)
    {
        _invoice.Receiver = receiver;
        return this;
    }

    /// <summary>
    /// Adds an invoice line
    /// </summary>
    public InvoiceBuilder AddLine(string name, decimal quantity, decimal unitPrice, decimal vatRate = 20, Action<InvoiceLineBuilder>? configure = null)
    {
        var builder = new InvoiceLineBuilder(name, quantity, unitPrice, vatRate);
        configure?.Invoke(builder);
        _invoice.Lines.Add(builder.Build());
        return this;
    }

    /// <summary>
    /// Adds an existing invoice line
    /// </summary>
    public InvoiceBuilder AddLine(InvoiceLine line)
    {
        _invoice.Lines.Add(line);
        return this;
    }

    /// <summary>
    /// Adds a note
    /// </summary>
    public InvoiceBuilder WithNote(string note)
    {
        _invoice.Notes ??= [];
        _invoice.Notes.Add(note);
        return this;
    }

    /// <summary>
    /// Sets payment terms
    /// </summary>
    public InvoiceBuilder WithPaymentTerms(DateTime? dueDate = null, string? iban = null, string? note = null)
    {
        _invoice.PaymentTerms = new PaymentTerms
        {
            DueDate = dueDate,
            Iban = iban,
            Note = note
        };
        return this;
    }

    /// <summary>
    /// Sets withholding information
    /// </summary>
    public InvoiceBuilder WithWithholding(decimal rate, string? reasonCode = null, string? reason = null)
    {
        _invoice.Withholding = new WithholdingInfo
        {
            Rate = rate,
            ReasonCode = reasonCode,
            Reason = reason
        };
        _invoice.InvoiceType = InvoiceType.Tevkifat;
        return this;
    }

    /// <summary>
    /// Builds the invoice request
    /// </summary>
    public InvoiceRequest Build()
    {
        _invoice.Uuid ??= Guid.NewGuid().ToString();
        return _invoice;
    }

    /// <summary>
    /// Builds and calculates totals
    /// </summary>
    public (InvoiceRequest Invoice, CalculatedTotals Totals) BuildWithTotals()
    {
        var invoice = Build();
        var totals = CalculateTotals();
        return (invoice, totals);
    }

    /// <summary>
    /// Calculates invoice totals
    /// </summary>
    public CalculatedTotals CalculateTotals()
    {
        decimal subtotal = 0;
        decimal totalVat = 0;
        decimal totalDiscount = 0;

        foreach (var line in _invoice.Lines)
        {
            var lineTotal = line.Quantity * line.UnitPrice;

            // Calculate discount
            if (line.DiscountAmount.HasValue)
            {
                totalDiscount += line.DiscountAmount.Value;
                lineTotal -= line.DiscountAmount.Value;
            }
            else if (line.DiscountRate.HasValue)
            {
                var discount = lineTotal * line.DiscountRate.Value / 100;
                totalDiscount += discount;
                lineTotal -= discount;
            }

            subtotal += lineTotal;
            totalVat += lineTotal * line.VatRate / 100;
        }

        var withholdingAmount = 0m;
        if (_invoice.Withholding != null)
        {
            withholdingAmount = totalVat * _invoice.Withholding.Rate;
        }

        var grandTotal = subtotal + totalVat;
        var payableAmount = grandTotal - withholdingAmount;

        return new CalculatedTotals
        {
            Subtotal = Math.Round(subtotal, 2),
            TotalVat = Math.Round(totalVat, 2),
            TotalDiscount = Math.Round(totalDiscount, 2),
            WithholdingAmount = Math.Round(withholdingAmount, 2),
            GrandTotal = Math.Round(grandTotal, 2),
            PayableAmount = Math.Round(payableAmount, 2)
        };
    }
}

/// <summary>
/// Calculated invoice totals
/// </summary>
public class CalculatedTotals
{
    /// <summary>
    /// Subtotal (before VAT)
    /// </summary>
    public decimal Subtotal { get; set; }

    /// <summary>
    /// Total VAT amount
    /// </summary>
    public decimal TotalVat { get; set; }

    /// <summary>
    /// Total discount amount
    /// </summary>
    public decimal TotalDiscount { get; set; }

    /// <summary>
    /// Withholding amount
    /// </summary>
    public decimal WithholdingAmount { get; set; }

    /// <summary>
    /// Grand total (subtotal + VAT)
    /// </summary>
    public decimal GrandTotal { get; set; }

    /// <summary>
    /// Payable amount (grand total - withholding)
    /// </summary>
    public decimal PayableAmount { get; set; }
}

/// <summary>
/// Builder for party information
/// </summary>
public class PartyBuilder
{
    private readonly Party _party = new();

    public PartyBuilder(string taxId, string name)
    {
        _party.TaxId = taxId;
        _party.Name = name;
    }

    public PartyBuilder WithTaxOffice(string taxOffice)
    {
        _party.TaxOffice = taxOffice;
        return this;
    }

    public PartyBuilder WithAddress(string city, string? street = null, string? district = null, string? postalCode = null)
    {
        _party.Address = new Address
        {
            City = city,
            Street = street,
            District = district,
            PostalCode = postalCode
        };
        return this;
    }

    public PartyBuilder WithContact(string? phone = null, string? email = null, string? fax = null)
    {
        _party.Contact = new Contact
        {
            Phone = phone,
            Email = email,
            Fax = fax
        };
        return this;
    }

    public Party Build() => _party;
}

/// <summary>
/// Builder for invoice lines
/// </summary>
public class InvoiceLineBuilder
{
    private readonly InvoiceLine _line = new();

    public InvoiceLineBuilder(string name, decimal quantity, decimal unitPrice, decimal vatRate)
    {
        _line.Name = name;
        _line.Quantity = quantity;
        _line.UnitPrice = unitPrice;
        _line.VatRate = vatRate;
    }

    public InvoiceLineBuilder WithUnit(string unitCode)
    {
        _line.UnitCode = unitCode;
        return this;
    }

    public InvoiceLineBuilder WithDescription(string description)
    {
        _line.Description = description;
        return this;
    }

    public InvoiceLineBuilder WithDiscountAmount(decimal amount)
    {
        _line.DiscountAmount = amount;
        return this;
    }

    public InvoiceLineBuilder WithDiscountRate(decimal rate)
    {
        _line.DiscountRate = rate;
        return this;
    }

    public InvoiceLine Build() => _line;
}
