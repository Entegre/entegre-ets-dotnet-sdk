using System.Text;
using System.Xml;
using System.Xml.Linq;
using Entegre.Ets.Sdk.Models.Common;
using Entegre.Ets.Sdk.Models.Invoice;

namespace Entegre.Ets.Sdk.Xml;

/// <summary>
/// UBL-TR XML result
/// </summary>
public class ParsedInvoice
{
    public string? Uuid { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? IssueDate { get; set; }
    public string? InvoiceType { get; set; }
    public string? DocumentCurrency { get; set; }
    public decimal? TaxExclusiveAmount { get; set; }
    public decimal? TaxInclusiveAmount { get; set; }
    public decimal? PayableAmount { get; set; }
    public decimal? TotalTaxAmount { get; set; }
    public PartyInfo? Supplier { get; set; }
    public PartyInfo? Customer { get; set; }
    public List<InvoiceLineInfo> Lines { get; set; } = new();
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Party information from XML
/// </summary>
public class PartyInfo
{
    public string? TaxId { get; set; }
    public string? Name { get; set; }
    public string? TaxOffice { get; set; }
    public string? City { get; set; }
    public string? District { get; set; }
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}

/// <summary>
/// Invoice line information from XML
/// </summary>
public class InvoiceLineInfo
{
    public int LineNumber { get; set; }
    public string? ItemCode { get; set; }
    public string? ItemName { get; set; }
    public decimal Quantity { get; set; }
    public string? UnitCode { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal { get; set; }
    public decimal VatRate { get; set; }
    public decimal VatAmount { get; set; }
}

/// <summary>
/// UBL-TR XML parser
/// </summary>
public static class XmlParser
{
    private static readonly XNamespace Cbc = "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2";
    private static readonly XNamespace Cac = "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2";
    private static readonly XNamespace Invoice = "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2";

    /// <summary>
    /// Parses UBL-TR invoice XML
    /// </summary>
    public static ParsedInvoice ParseInvoice(string xml)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root;

        if (root == null)
            throw new XmlParseException("Invalid XML: no root element");

        var result = new ParsedInvoice
        {
            Uuid = GetValue(root, Cbc + "UUID"),
            InvoiceNumber = GetValue(root, Cbc + "ID"),
            IssueDate = ParseDate(GetValue(root, Cbc + "IssueDate")),
            InvoiceType = GetValue(root, Cbc + "InvoiceTypeCode"),
            DocumentCurrency = GetValue(root, Cbc + "DocumentCurrencyCode")
        };

        // Parse monetary totals
        var monetaryTotal = root.Element(Cac + "LegalMonetaryTotal");
        if (monetaryTotal != null)
        {
            result.TaxExclusiveAmount = ParseDecimal(GetValue(monetaryTotal, Cbc + "TaxExclusiveAmount"));
            result.TaxInclusiveAmount = ParseDecimal(GetValue(monetaryTotal, Cbc + "TaxInclusiveAmount"));
            result.PayableAmount = ParseDecimal(GetValue(monetaryTotal, Cbc + "PayableAmount"));
        }

        // Parse tax total
        var taxTotal = root.Element(Cac + "TaxTotal");
        if (taxTotal != null)
        {
            result.TotalTaxAmount = ParseDecimal(GetValue(taxTotal, Cbc + "TaxAmount"));
        }

        // Parse supplier
        var supplier = root.Element(Cac + "AccountingSupplierParty")?.Element(Cac + "Party");
        if (supplier != null)
        {
            result.Supplier = ParseParty(supplier);
        }

        // Parse customer
        var customer = root.Element(Cac + "AccountingCustomerParty")?.Element(Cac + "Party");
        if (customer != null)
        {
            result.Customer = ParseParty(customer);
        }

        // Parse invoice lines
        foreach (var line in root.Elements(Cac + "InvoiceLine"))
        {
            result.Lines.Add(ParseInvoiceLine(line));
        }

        // Parse notes
        foreach (var note in root.Elements(Cbc + "Note"))
        {
            var noteValue = note.Value;
            if (!string.IsNullOrEmpty(noteValue))
                result.Notes.Add(noteValue);
        }

        return result;
    }

    /// <summary>
    /// Parses UBL-TR invoice XML from Base64
    /// </summary>
    public static ParsedInvoice ParseInvoiceFromBase64(string base64Xml)
    {
        var bytes = Convert.FromBase64String(base64Xml);
        var xml = Encoding.UTF8.GetString(bytes);
        return ParseInvoice(xml);
    }

    /// <summary>
    /// Parses UBL-TR invoice XML from file
    /// </summary>
    public static async Task<ParsedInvoice> ParseInvoiceFromFileAsync(string filePath)
    {
        var xml = await File.ReadAllTextAsync(filePath);
        return ParseInvoice(xml);
    }

    private static PartyInfo ParseParty(XElement party)
    {
        var result = new PartyInfo();

        // Tax ID
        var partyId = party.Element(Cac + "PartyIdentification")?.Element(Cbc + "ID");
        result.TaxId = partyId?.Value;

        // Name
        var partyName = party.Element(Cac + "PartyName")?.Element(Cbc + "Name");
        result.Name = partyName?.Value;

        // Tax office
        var taxScheme = party.Element(Cac + "PartyTaxScheme");
        result.TaxOffice = GetValue(taxScheme, Cbc + "Name") ?? GetValue(taxScheme?.Element(Cac + "TaxScheme"), Cbc + "Name");

        // Address
        var address = party.Element(Cac + "PostalAddress");
        if (address != null)
        {
            result.City = GetValue(address, Cbc + "CityName");
            result.District = GetValue(address, Cbc + "CitySubdivisionName");
            result.Address = GetValue(address, Cbc + "StreetName");
        }

        // Contact
        var contact = party.Element(Cac + "Contact");
        if (contact != null)
        {
            result.Phone = GetValue(contact, Cbc + "Telephone");
            result.Email = GetValue(contact, Cbc + "ElectronicMail");
        }

        return result;
    }

    private static InvoiceLineInfo ParseInvoiceLine(XElement line)
    {
        var result = new InvoiceLineInfo
        {
            LineNumber = int.TryParse(GetValue(line, Cbc + "ID"), out var ln) ? ln : 0,
            Quantity = ParseDecimal(GetValue(line, Cbc + "InvoicedQuantity")) ?? 0,
            UnitCode = line.Element(Cbc + "InvoicedQuantity")?.Attribute("unitCode")?.Value,
            LineTotal = ParseDecimal(GetValue(line, Cbc + "LineExtensionAmount")) ?? 0
        };

        // Item
        var item = line.Element(Cac + "Item");
        if (item != null)
        {
            result.ItemName = GetValue(item, Cbc + "Name");
            result.ItemCode = GetValue(item.Element(Cac + "SellersItemIdentification"), Cbc + "ID");
        }

        // Price
        var price = line.Element(Cac + "Price");
        result.UnitPrice = ParseDecimal(GetValue(price, Cbc + "PriceAmount")) ?? 0;

        // Tax
        var taxTotal = line.Element(Cac + "TaxTotal");
        if (taxTotal != null)
        {
            result.VatAmount = ParseDecimal(GetValue(taxTotal, Cbc + "TaxAmount")) ?? 0;

            var taxSubtotal = taxTotal.Element(Cac + "TaxSubtotal");
            result.VatRate = ParseDecimal(GetValue(taxSubtotal, Cbc + "Percent")) ?? 0;
        }

        return result;
    }

    private static string? GetValue(XElement? element, XName name)
    {
        return element?.Element(name)?.Value;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return DateTime.TryParse(value, out var date) ? date : null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return decimal.TryParse(value, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}

/// <summary>
/// XML serializer for UBL-TR format
/// </summary>
public static class XmlSerializer
{
    /// <summary>
    /// Converts invoice to UBL-TR XML
    /// </summary>
    public static string ToUblTr(InvoiceRequest invoice)
    {
        var settings = new XmlWriterSettings
        {
            Indent = true,
            Encoding = Encoding.UTF8
        };

        using var stream = new MemoryStream();
        using (var writer = XmlWriter.Create(stream, settings))
        {
            WriteInvoice(writer, invoice);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Converts invoice to UBL-TR XML and saves to file
    /// </summary>
    public static async Task SaveToFileAsync(InvoiceRequest invoice, string filePath)
    {
        var xml = ToUblTr(invoice);
        await File.WriteAllTextAsync(filePath, xml, Encoding.UTF8);
    }

    private static void WriteInvoice(XmlWriter writer, InvoiceRequest invoice)
    {
        writer.WriteStartDocument();
        writer.WriteStartElement("Invoice", "urn:oasis:names:specification:ubl:schema:xsd:Invoice-2");

        writer.WriteAttributeString("xmlns", "cac", null, "urn:oasis:names:specification:ubl:schema:xsd:CommonAggregateComponents-2");
        writer.WriteAttributeString("xmlns", "cbc", null, "urn:oasis:names:specification:ubl:schema:xsd:CommonBasicComponents-2");

        // UUID
        writer.WriteElementString("cbc", "UUID", null, invoice.Uuid ?? Guid.NewGuid().ToString());

        // Invoice Number
        if (!string.IsNullOrEmpty(invoice.InvoiceNumber))
            writer.WriteElementString("cbc", "ID", null, invoice.InvoiceNumber);

        // Issue Date
        writer.WriteElementString("cbc", "IssueDate", null, invoice.IssueDate.ToString("yyyy-MM-dd"));

        // Invoice Type
        writer.WriteElementString("cbc", "InvoiceTypeCode", null, invoice.InvoiceType.ToString().ToUpperInvariant());

        // Currency
        writer.WriteElementString("cbc", "DocumentCurrencyCode", null, invoice.Currency);

        // Notes
        if (invoice.Notes != null)
        {
            foreach (var note in invoice.Notes)
            {
                writer.WriteElementString("cbc", "Note", null, note);
            }
        }

        // Supplier Party
        WriteParty(writer, "AccountingSupplierParty", invoice.Sender);

        // Customer Party
        WriteParty(writer, "AccountingCustomerParty", invoice.Receiver);

        // Invoice Lines
        var lineNumber = 1;
        foreach (var line in invoice.Lines)
        {
            WriteInvoiceLine(writer, line, lineNumber++);
        }

        writer.WriteEndElement(); // Invoice
        writer.WriteEndDocument();
    }

    private static void WriteParty(XmlWriter writer, string elementName, Party party)
    {
        writer.WriteStartElement("cac", elementName, null);
        writer.WriteStartElement("cac", "Party", null);

        // Party Identification
        writer.WriteStartElement("cac", "PartyIdentification", null);
        writer.WriteStartElement("cbc", "ID", null);
        writer.WriteAttributeString("schemeID", party.TaxId.Length == 11 ? "TCKN" : "VKN");
        writer.WriteString(party.TaxId);
        writer.WriteEndElement(); // ID
        writer.WriteEndElement(); // PartyIdentification

        // Party Name
        writer.WriteStartElement("cac", "PartyName", null);
        writer.WriteElementString("cbc", "Name", null, party.Name);
        writer.WriteEndElement(); // PartyName

        // Address
        if (party.Address != null)
        {
            writer.WriteStartElement("cac", "PostalAddress", null);
            if (!string.IsNullOrEmpty(party.Address.Street))
                writer.WriteElementString("cbc", "StreetName", null, party.Address.Street);
            if (!string.IsNullOrEmpty(party.Address.District))
                writer.WriteElementString("cbc", "CitySubdivisionName", null, party.Address.District);
            writer.WriteElementString("cbc", "CityName", null, party.Address.City);
            writer.WriteStartElement("cac", "Country", null);
            writer.WriteElementString("cbc", "Name", null, party.Address.Country);
            writer.WriteEndElement(); // Country
            writer.WriteEndElement(); // PostalAddress
        }

        // Tax Scheme
        if (!string.IsNullOrEmpty(party.TaxOffice))
        {
            writer.WriteStartElement("cac", "PartyTaxScheme", null);
            writer.WriteStartElement("cac", "TaxScheme", null);
            writer.WriteElementString("cbc", "Name", null, party.TaxOffice);
            writer.WriteEndElement(); // TaxScheme
            writer.WriteEndElement(); // PartyTaxScheme
        }

        writer.WriteEndElement(); // Party
        writer.WriteEndElement(); // AccountingXxxParty
    }

    private static void WriteInvoiceLine(XmlWriter writer, InvoiceLine line, int lineNumber)
    {
        writer.WriteStartElement("cac", "InvoiceLine", null);

        writer.WriteElementString("cbc", "ID", null, lineNumber.ToString());

        // Quantity
        writer.WriteStartElement("cbc", "InvoicedQuantity", null);
        writer.WriteAttributeString("unitCode", line.UnitCode);
        writer.WriteString(line.Quantity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        // Line Total
        var lineTotal = line.Quantity * line.UnitPrice;
        writer.WriteStartElement("cbc", "LineExtensionAmount", null);
        writer.WriteAttributeString("currencyID", "TRY");
        writer.WriteString(lineTotal.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteEndElement();

        // Item
        writer.WriteStartElement("cac", "Item", null);
        writer.WriteElementString("cbc", "Name", null, line.Name);
        writer.WriteEndElement(); // Item

        // Price
        writer.WriteStartElement("cac", "Price", null);
        writer.WriteStartElement("cbc", "PriceAmount", null);
        writer.WriteAttributeString("currencyID", "TRY");
        writer.WriteString(line.UnitPrice.ToString("F2", System.Globalization.CultureInfo.InvariantCulture));
        writer.WriteEndElement();
        writer.WriteEndElement(); // Price

        writer.WriteEndElement(); // InvoiceLine
    }
}

/// <summary>
/// XML parse exception
/// </summary>
public class XmlParseException : Exception
{
    public XmlParseException(string message) : base(message) { }
}
