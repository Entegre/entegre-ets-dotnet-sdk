using Entegre.Ets.Sdk.Models.Invoice;
using Entegre.Ets.Sdk.Models.Common;

namespace Entegre.Ets.Sdk.Validation;

/// <summary>
/// Invoice validation error
/// </summary>
public class InvoiceValidationError
{
    /// <summary>
    /// Error code
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Field path
    /// </summary>
    public string? Field { get; set; }
}

/// <summary>
/// Invoice validation result
/// </summary>
public class InvoiceValidationResult
{
    /// <summary>
    /// Whether the invoice is valid
    /// </summary>
    public bool IsValid => Errors.Count == 0;

    /// <summary>
    /// Critical errors (invoice cannot be sent)
    /// </summary>
    public List<InvoiceValidationError> Errors { get; set; } = new();

    /// <summary>
    /// Warnings (invoice can be sent but should be reviewed)
    /// </summary>
    public List<InvoiceValidationError> Warnings { get; set; } = new();

    /// <summary>
    /// Suggestions for improvement
    /// </summary>
    public List<string> Suggestions { get; set; } = new();
}

/// <summary>
/// Invoice validation options
/// </summary>
public class InvoiceValidationOptions
{
    /// <summary>
    /// Skip calculation checks
    /// </summary>
    public bool SkipCalculationCheck { get; set; }

    /// <summary>
    /// Skip date checks
    /// </summary>
    public bool SkipDateCheck { get; set; }
}

/// <summary>
/// Invoice validation error codes
/// </summary>
public static class ValidationErrorCodes
{
    // Required fields
    public const string IssueDateRequired = "ISSUE_DATE_REQUIRED";
    public const string InvoiceTypeRequired = "INVOICE_TYPE_REQUIRED";
    public const string SenderRequired = "SENDER_REQUIRED";
    public const string ReceiverRequired = "RECEIVER_REQUIRED";
    public const string LinesRequired = "LINES_REQUIRED";

    // Party validation
    public const string SenderTaxIdInvalid = "SENDER_TAX_ID_INVALID";
    public const string ReceiverTaxIdInvalid = "RECEIVER_TAX_ID_INVALID";
    public const string SenderNameRequired = "SENDER_NAME_REQUIRED";
    public const string ReceiverNameRequired = "RECEIVER_NAME_REQUIRED";

    // Line validation
    public const string LineItemNameRequired = "LINE_ITEM_NAME_REQUIRED";
    public const string LineQuantityInvalid = "LINE_QUANTITY_INVALID";
    public const string LinePriceInvalid = "LINE_PRICE_INVALID";

    // Calculation
    public const string CalculationMismatch = "CALCULATION_MISMATCH";

    // Date
    public const string DateInFuture = "DATE_IN_FUTURE";
    public const string DateTooOld = "DATE_TOO_OLD";
    public const string DateInvalidFormat = "DATE_INVALID_FORMAT";

    // Business rules
    public const string WithholdingRequired = "WITHHOLDING_REQUIRED";
    public const string ExportCountryRequired = "EXPORT_COUNTRY_REQUIRED";
}

/// <summary>
/// Invoice pre-validation service
/// </summary>
public static class InvoiceValidator
{
    /// <summary>
    /// Validates an invoice request
    /// </summary>
    /// <param name="invoice">Invoice request to validate</param>
    /// <param name="options">Validation options</param>
    /// <returns>Validation result with errors, warnings, and suggestions</returns>
    public static InvoiceValidationResult Validate(InvoiceRequest invoice, InvoiceValidationOptions? options = null)
    {
        options ??= new InvoiceValidationOptions();
        var result = new InvoiceValidationResult();

        // 1. Required fields
        ValidateRequiredFields(invoice, result);

        // 2. Party validation
        if (invoice.Sender != null)
            ValidateParty(invoice.Sender, "Sender", result);
        if (invoice.Receiver != null)
            ValidateParty(invoice.Receiver, "Receiver", result);

        // 3. Line validation
        if (invoice.Lines.Count > 0)
            ValidateLines(invoice.Lines, result);

        // 4. Date validation
        if (!options.SkipDateCheck)
            ValidateDate(invoice.IssueDate, result);

        // 5. Business rules
        ValidateBusinessRules(invoice, result);

        // 6. Calculation validation
        if (!options.SkipCalculationCheck && invoice.Lines.Count > 0)
            ValidateCalculations(invoice, result);

        return result;
    }

    /// <summary>
    /// Quick validation - only checks critical errors
    /// </summary>
    public static bool IsValid(InvoiceRequest invoice)
    {
        var result = Validate(invoice, new InvoiceValidationOptions
        {
            SkipCalculationCheck = true,
            SkipDateCheck = true
        });
        return result.IsValid;
    }

    private static void ValidateRequiredFields(InvoiceRequest invoice, InvoiceValidationResult result)
    {
        if (invoice.IssueDate == default)
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = ValidationErrorCodes.IssueDateRequired,
                Message = "Fatura tarihi zorunludur",
                Field = "IssueDate"
            });
        }

        if (invoice.Sender == null)
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = ValidationErrorCodes.SenderRequired,
                Message = "Gönderici bilgisi zorunludur",
                Field = "Sender"
            });
        }

        if (invoice.Receiver == null)
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = ValidationErrorCodes.ReceiverRequired,
                Message = "Alıcı bilgisi zorunludur",
                Field = "Receiver"
            });
        }

        if (invoice.Lines.Count == 0)
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = ValidationErrorCodes.LinesRequired,
                Message = "En az bir fatura kalemi zorunludur",
                Field = "Lines"
            });
        }
    }

    private static void ValidateParty(Party party, string partyType, InvoiceValidationResult result)
    {
        var isReceiver = partyType == "Receiver";
        var label = isReceiver ? "Alıcı" : "Gönderici";

        if (string.IsNullOrWhiteSpace(party.TaxId))
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = isReceiver ? ValidationErrorCodes.ReceiverTaxIdInvalid : ValidationErrorCodes.SenderTaxIdInvalid,
                Message = $"{label} VKN/TCKN zorunludur",
                Field = $"{partyType}.TaxId"
            });
        }
        else
        {
            var taxIdResult = TaxIdValidator.ValidateTaxId(party.TaxId);
            if (!taxIdResult.IsValid)
            {
                result.Errors.Add(new InvoiceValidationError
                {
                    Code = isReceiver ? ValidationErrorCodes.ReceiverTaxIdInvalid : ValidationErrorCodes.SenderTaxIdInvalid,
                    Message = $"{label}: {taxIdResult.ErrorMessage}",
                    Field = $"{partyType}.TaxId"
                });
            }
        }

        if (string.IsNullOrWhiteSpace(party.Name))
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = isReceiver ? ValidationErrorCodes.ReceiverNameRequired : ValidationErrorCodes.SenderNameRequired,
                Message = $"{label} unvanı zorunludur",
                Field = $"{partyType}.Name"
            });
        }
    }

    private static void ValidateLines(List<InvoiceLine> lines, InvoiceValidationResult result)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var lineNum = i + 1;

            if (string.IsNullOrWhiteSpace(line.Name))
            {
                result.Errors.Add(new InvoiceValidationError
                {
                    Code = ValidationErrorCodes.LineItemNameRequired,
                    Message = $"Satır {lineNum}: Ürün/hizmet adı zorunludur",
                    Field = $"Lines[{i}].Name"
                });
            }

            if (line.Quantity <= 0)
            {
                result.Errors.Add(new InvoiceValidationError
                {
                    Code = ValidationErrorCodes.LineQuantityInvalid,
                    Message = $"Satır {lineNum}: Miktar 0'dan büyük olmalıdır",
                    Field = $"Lines[{i}].Quantity"
                });
            }

            if (line.UnitPrice < 0)
            {
                result.Errors.Add(new InvoiceValidationError
                {
                    Code = ValidationErrorCodes.LinePriceInvalid,
                    Message = $"Satır {lineNum}: Birim fiyat negatif olamaz",
                    Field = $"Lines[{i}].UnitPrice"
                });
            }
        }
    }

    private static void ValidateDate(DateTime issueDate, InvoiceValidationResult result)
    {
        if (issueDate == default)
            return;

        var today = DateTime.Today;

        // Future date check
        if (issueDate.Date > today)
        {
            result.Errors.Add(new InvoiceValidationError
            {
                Code = ValidationErrorCodes.DateInFuture,
                Message = "Fatura tarihi gelecekte olamaz",
                Field = "IssueDate"
            });
        }

        // 7 days old check
        if (issueDate.Date < today.AddDays(-7))
        {
            result.Warnings.Add(new InvoiceValidationError
            {
                Code = ValidationErrorCodes.DateTooOld,
                Message = "Fatura tarihi 7 günden eski. GİB tarafından reddedilebilir.",
                Field = "IssueDate"
            });
            result.Suggestions.Add("Fatura tarihini güncelleyin veya GİB ile iletişime geçin.");
        }
    }

    private static void ValidateBusinessRules(InvoiceRequest invoice, InvoiceValidationResult result)
    {
        // Withholding invoice check
        if (invoice.InvoiceType == InvoiceType.Tevkifat)
        {
            if (invoice.Withholding == null || invoice.Withholding.Rate <= 0)
            {
                result.Warnings.Add(new InvoiceValidationError
                {
                    Code = ValidationErrorCodes.WithholdingRequired,
                    Message = "Tevkifat faturası için tevkifat oranı tanımlanmalıdır",
                    Field = "Withholding"
                });
                result.Suggestions.Add("Tevkifat oranı ve sebebi ekleyin (örn: 9/10 Güvenlik Hizmetleri)");
            }
        }

        // Export invoice check
        if (invoice.InvoiceType == InvoiceType.Istisna || invoice.InvoiceType == InvoiceType.IhracKayitli)
        {
            var country = invoice.Receiver?.Address?.Country;
            if (string.IsNullOrWhiteSpace(country) ||
                country.Equals("TR", StringComparison.OrdinalIgnoreCase) ||
                country.Equals("Türkiye", StringComparison.OrdinalIgnoreCase) ||
                country.Equals("TÜRKİYE", StringComparison.OrdinalIgnoreCase))
            {
                result.Warnings.Add(new InvoiceValidationError
                {
                    Code = ValidationErrorCodes.ExportCountryRequired,
                    Message = "İhracat/İstisna faturası için yabancı ülke belirtilmelidir",
                    Field = "Receiver.Address.Country"
                });
            }
        }

        // Currency suggestion
        if (!string.IsNullOrEmpty(invoice.Currency) && invoice.Currency != "TRY")
        {
            if (!invoice.ExchangeRate.HasValue || invoice.ExchangeRate == 0)
            {
                result.Suggestions.Add($"Döviz faturası için TCMB kurunu kullanabilirsiniz: await Tcmb.GetInvoiceRateAsync(\"{invoice.Currency}\")");
            }
        }
    }

    private static void ValidateCalculations(InvoiceRequest invoice, InvoiceValidationResult result)
    {
        // Calculate line totals
        decimal calculatedTotal = 0;
        decimal calculatedVat = 0;

        foreach (var line in invoice.Lines)
        {
            var lineTotal = line.Quantity * line.UnitPrice;

            // Apply discount
            if (line.DiscountAmount.HasValue)
                lineTotal -= line.DiscountAmount.Value;
            else if (line.DiscountRate.HasValue)
                lineTotal -= lineTotal * line.DiscountRate.Value / 100;

            calculatedTotal += lineTotal;
            calculatedVat += lineTotal * line.VatRate / 100;
        }

        // Round to 2 decimal places
        calculatedTotal = Math.Round(calculatedTotal, 2);
        calculatedVat = Math.Round(calculatedVat, 2);

        // This is informational - calculations are handled by the builder
        // Only add suggestion if user might want to verify
        result.Suggestions.Add($"Hesaplanan toplam: {calculatedTotal:N2} TRY, KDV: {calculatedVat:N2} TRY");
    }

    /// <summary>
    /// Formats validation result as a string
    /// </summary>
    public static string FormatResult(InvoiceValidationResult result)
    {
        var lines = new List<string>();

        if (result.IsValid)
        {
            lines.Add("✓ Fatura doğrulaması başarılı");
        }
        else
        {
            lines.Add("✗ Fatura doğrulaması başarısız");
        }

        if (result.Errors.Count > 0)
        {
            lines.Add("");
            lines.Add("Hatalar:");
            foreach (var error in result.Errors)
            {
                lines.Add($"  - [{error.Code}] {error.Message}");
                if (!string.IsNullOrEmpty(error.Field))
                    lines.Add($"    Alan: {error.Field}");
            }
        }

        if (result.Warnings.Count > 0)
        {
            lines.Add("");
            lines.Add("Uyarılar:");
            foreach (var warning in result.Warnings)
            {
                lines.Add($"  - [{warning.Code}] {warning.Message}");
            }
        }

        if (result.Suggestions.Count > 0)
        {
            lines.Add("");
            lines.Add("Öneriler:");
            foreach (var suggestion in result.Suggestions)
            {
                lines.Add($"  - {suggestion}");
            }
        }

        return string.Join(Environment.NewLine, lines);
    }
}
