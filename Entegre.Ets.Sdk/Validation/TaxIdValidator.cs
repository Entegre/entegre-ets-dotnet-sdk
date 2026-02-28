namespace Entegre.Ets.Sdk.Validation;

/// <summary>
/// Validation result
/// </summary>
/// <param name="IsValid">Whether the value is valid</param>
/// <param name="ErrorMessage">Error message if invalid</param>
public record ValidationResult(bool IsValid, string? ErrorMessage = null);

/// <summary>
/// Turkish Tax ID (VKN) and Identity Number (TCKN) validator
/// </summary>
public static class TaxIdValidator
{
    /// <summary>
    /// Validates a Turkish Tax Identification Number (VKN - Vergi Kimlik Numarası)
    /// VKN is 10 digits with a specific checksum algorithm
    /// </summary>
    public static ValidationResult ValidateVkn(string? vkn)
    {
        if (string.IsNullOrWhiteSpace(vkn))
            return new ValidationResult(false, "VKN boş olamaz");

        var cleaned = vkn.Trim();

        if (cleaned.Length != 10)
            return new ValidationResult(false, "VKN 10 haneli olmalıdır");

        if (!cleaned.All(char.IsDigit))
            return new ValidationResult(false, "VKN sadece rakam içermelidir");

        // VKN checksum algorithm
        var digits = cleaned.Select(c => c - '0').ToArray();
        var sum = 0;

        for (var i = 0; i < 9; i++)
        {
            var tmp = (digits[i] + (9 - i)) % 10;
            sum += (tmp * (int)Math.Pow(2, 9 - i)) % 9;
            if (tmp != 0 && (tmp * (int)Math.Pow(2, 9 - i)) % 9 == 0)
                sum += 9;
        }

        var checkDigit = (10 - (sum % 10)) % 10;

        if (checkDigit != digits[9])
            return new ValidationResult(false, "VKN kontrol hanesi geçersiz");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates a Turkish Citizen Identity Number (TCKN - T.C. Kimlik Numarası)
    /// TCKN is 11 digits with a specific checksum algorithm
    /// </summary>
    public static ValidationResult ValidateTckn(string? tckn)
    {
        if (string.IsNullOrWhiteSpace(tckn))
            return new ValidationResult(false, "TCKN boş olamaz");

        var cleaned = tckn.Trim();

        if (cleaned.Length != 11)
            return new ValidationResult(false, "TCKN 11 haneli olmalıdır");

        if (!cleaned.All(char.IsDigit))
            return new ValidationResult(false, "TCKN sadece rakam içermelidir");

        if (cleaned[0] == '0')
            return new ValidationResult(false, "TCKN sıfır ile başlayamaz");

        var digits = cleaned.Select(c => c - '0').ToArray();

        // First checksum: ((d1+d3+d5+d7+d9)*7 - (d2+d4+d6+d8)) % 10 = d10
        var oddSum = digits[0] + digits[2] + digits[4] + digits[6] + digits[8];
        var evenSum = digits[1] + digits[3] + digits[5] + digits[7];
        var check10 = ((oddSum * 7) - evenSum) % 10;

        if (check10 < 0) check10 += 10;

        if (check10 != digits[9])
            return new ValidationResult(false, "TCKN kontrol hanesi geçersiz");

        // Second checksum: (d1+d2+...+d10) % 10 = d11
        var sumFirst10 = digits.Take(10).Sum();
        var check11 = sumFirst10 % 10;

        if (check11 != digits[10])
            return new ValidationResult(false, "TCKN kontrol hanesi geçersiz");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates either VKN (10 digits) or TCKN (11 digits) based on length
    /// </summary>
    public static ValidationResult ValidateTaxId(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return new ValidationResult(false, "Vergi numarası boş olamaz");

        var cleaned = taxId.Trim();

        return cleaned.Length switch
        {
            10 => ValidateVkn(cleaned),
            11 => ValidateTckn(cleaned),
            _ => new ValidationResult(false, "Vergi numarası 10 (VKN) veya 11 (TCKN) haneli olmalıdır")
        };
    }

    /// <summary>
    /// Quick check if the tax ID format is valid (without checksum verification)
    /// </summary>
    public static bool IsValidFormat(string? taxId)
    {
        if (string.IsNullOrWhiteSpace(taxId))
            return false;

        var cleaned = taxId.Trim();
        return (cleaned.Length == 10 || cleaned.Length == 11) && cleaned.All(char.IsDigit);
    }
}

/// <summary>
/// IBAN validator
/// </summary>
public static class IbanValidator
{
    /// <summary>
    /// Validates an IBAN using the MOD-97 algorithm
    /// </summary>
    public static ValidationResult Validate(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return new ValidationResult(false, "IBAN boş olamaz");

        // Clean and normalize
        var cleaned = iban.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        // Check basic format
        if (cleaned.Length < 15 || cleaned.Length > 34)
            return new ValidationResult(false, "IBAN uzunluğu geçersiz");

        if (!cleaned[..2].All(char.IsLetter))
            return new ValidationResult(false, "IBAN ülke kodu geçersiz");

        if (!cleaned[2..4].All(char.IsDigit))
            return new ValidationResult(false, "IBAN kontrol hanesi geçersiz");

        // Turkish IBAN specific check
        if (cleaned.StartsWith("TR") && cleaned.Length != 26)
            return new ValidationResult(false, "Türk IBAN'ı 26 karakter olmalıdır");

        // MOD-97 validation
        var rearranged = cleaned[4..] + cleaned[..4];

        var numericString = string.Concat(rearranged.Select(c =>
            char.IsLetter(c) ? (c - 'A' + 10).ToString() : c.ToString()));

        // Calculate MOD 97 using big integer arithmetic
        var remainder = 0;
        foreach (var c in numericString)
        {
            remainder = (remainder * 10 + (c - '0')) % 97;
        }

        if (remainder != 1)
            return new ValidationResult(false, "IBAN kontrol kodu geçersiz");

        return new ValidationResult(true);
    }

    /// <summary>
    /// Validates a Turkish IBAN
    /// </summary>
    public static ValidationResult ValidateTurkishIban(string? iban)
    {
        if (string.IsNullOrWhiteSpace(iban))
            return new ValidationResult(false, "IBAN boş olamaz");

        var cleaned = iban.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        if (!cleaned.StartsWith("TR"))
            return new ValidationResult(false, "Türk IBAN'ı TR ile başlamalıdır");

        if (cleaned.Length != 26)
            return new ValidationResult(false, "Türk IBAN'ı 26 karakter olmalıdır");

        return Validate(cleaned);
    }

    /// <summary>
    /// Formats an IBAN with spaces for display
    /// </summary>
    public static string Format(string iban)
    {
        var cleaned = iban.Replace(" ", "").Replace("-", "").ToUpperInvariant();

        var groups = Enumerable.Range(0, (int)Math.Ceiling(cleaned.Length / 4.0))
            .Select(i => cleaned.Substring(i * 4, Math.Min(4, cleaned.Length - i * 4)));

        return string.Join(" ", groups);
    }
}
