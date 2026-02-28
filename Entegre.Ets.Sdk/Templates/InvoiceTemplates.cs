using Entegre.Ets.Sdk.Builders;
using Entegre.Ets.Sdk.Models.Invoice;

namespace Entegre.Ets.Sdk.Templates;

/// <summary>
/// Pre-defined invoice templates for common scenarios
/// </summary>
public static class InvoiceTemplates
{
    /// <summary>
    /// Creates a standard retail sales invoice template
    /// </summary>
    public static InvoiceBuilder Retail() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Satis)
        .WithDocumentType(DocumentType.EArsiv)
        .WithCurrency("TRY");

    /// <summary>
    /// Creates a B2B e-invoice template
    /// </summary>
    public static InvoiceBuilder B2B() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Satis)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY");

    /// <summary>
    /// Creates a service invoice template
    /// </summary>
    public static InvoiceBuilder Service() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Satis)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY");

    /// <summary>
    /// Creates an export invoice template (0% VAT)
    /// </summary>
    public static InvoiceBuilder Export() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Istisna)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("USD")
        .WithExemption("301", "Mal İhracatı");

    /// <summary>
    /// Creates an EU export invoice template
    /// </summary>
    public static InvoiceBuilder ExportEU() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Istisna)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("EUR")
        .WithExemption("301", "Mal İhracatı");

    /// <summary>
    /// Creates a return/refund invoice template
    /// </summary>
    public static InvoiceBuilder Return() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Iade)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY");

    /// <summary>
    /// Creates a withholding tax invoice template (security services 9/10)
    /// </summary>
    public static InvoiceBuilder WithholdingSecurity() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Tevkifat)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY")
        .WithWithholding(0.90m, "603", "Güvenlik Hizmetleri");

    /// <summary>
    /// Creates a withholding tax invoice template (cleaning services 9/10)
    /// </summary>
    public static InvoiceBuilder WithholdingCleaning() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Tevkifat)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY")
        .WithWithholding(0.90m, "602", "Temizlik Hizmetleri");

    /// <summary>
    /// Creates a withholding tax invoice template (personnel services 9/10)
    /// </summary>
    public static InvoiceBuilder WithholdingPersonnel() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Tevkifat)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY")
        .WithWithholding(0.90m, "604", "İşgücü Temin Hizmetleri");

    /// <summary>
    /// Creates a withholding tax invoice template (construction 4/10)
    /// </summary>
    public static InvoiceBuilder WithholdingConstruction() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Tevkifat)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY")
        .WithWithholding(0.40m, "601", "Yapım İşleri");

    /// <summary>
    /// Creates a withholding tax invoice template (catering services 5/10)
    /// </summary>
    public static InvoiceBuilder WithholdingCatering() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Tevkifat)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY")
        .WithWithholding(0.50m, "605", "Yemek Servis Hizmetleri");

    /// <summary>
    /// Creates a government/public sector invoice template
    /// </summary>
    public static InvoiceBuilder Government() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Satis)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY");

    /// <summary>
    /// Creates an SGK (Social Security) invoice template
    /// </summary>
    public static InvoiceBuilder SGK() => InvoiceBuilder.Create()
        .WithType(InvoiceType.SGK)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY");

    /// <summary>
    /// Creates a free goods invoice template (0 amount)
    /// </summary>
    public static InvoiceBuilder FreeGoods() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Satis)
        .WithDocumentType(DocumentType.EFatura)
        .WithCurrency("TRY")
        .WithNote("Bedelsiz mal teslimi");

    /// <summary>
    /// Creates a proforma invoice template (draft)
    /// </summary>
    public static InvoiceBuilder Proforma() => InvoiceBuilder.Create()
        .WithType(InvoiceType.Satis)
        .WithDocumentType(DocumentType.EArsiv)
        .WithCurrency("TRY")
        .WithNote("PROFORMA - Kesin fatura değildir");
}

/// <summary>
/// Common invoice line templates
/// </summary>
public static class LineTemplates
{
    /// <summary>
    /// Software license line
    /// </summary>
    public static Action<InvoiceLineBuilder> SoftwareLicense(decimal price, int months = 12) =>
        line => line
            .WithName("Yazılım Lisansı")
            .WithQuantity(months)
            .WithUnitPrice(price)
            .WithUnit("MON")
            .WithVatRate(20);

    /// <summary>
    /// Technical support line
    /// </summary>
    public static Action<InvoiceLineBuilder> TechnicalSupport(decimal hourlyRate, decimal hours) =>
        line => line
            .WithName("Teknik Destek Hizmeti")
            .WithQuantity(hours)
            .WithUnitPrice(hourlyRate)
            .WithUnit("HUR")
            .WithVatRate(20);

    /// <summary>
    /// Consulting service line
    /// </summary>
    public static Action<InvoiceLineBuilder> Consulting(decimal dailyRate, decimal days) =>
        line => line
            .WithName("Danışmanlık Hizmeti")
            .WithQuantity(days)
            .WithUnitPrice(dailyRate)
            .WithUnit("DAY")
            .WithVatRate(20);

    /// <summary>
    /// Hosting service line
    /// </summary>
    public static Action<InvoiceLineBuilder> Hosting(decimal monthlyPrice, int months = 1) =>
        line => line
            .WithName("Sunucu Barındırma Hizmeti")
            .WithQuantity(months)
            .WithUnitPrice(monthlyPrice)
            .WithUnit("MON")
            .WithVatRate(20);

    /// <summary>
    /// Domain registration line
    /// </summary>
    public static Action<InvoiceLineBuilder> Domain(string domainName, decimal price, int years = 1) =>
        line => line
            .WithName($"Alan Adı Kaydı: {domainName}")
            .WithQuantity(years)
            .WithUnitPrice(price)
            .WithUnit("ANN")
            .WithVatRate(20);

    /// <summary>
    /// SSL certificate line
    /// </summary>
    public static Action<InvoiceLineBuilder> SslCertificate(decimal price, int years = 1) =>
        line => line
            .WithName("SSL Sertifikası")
            .WithQuantity(years)
            .WithUnitPrice(price)
            .WithUnit("ANN")
            .WithVatRate(20);

    /// <summary>
    /// Training service line
    /// </summary>
    public static Action<InvoiceLineBuilder> Training(decimal price, decimal days) =>
        line => line
            .WithName("Eğitim Hizmeti")
            .WithQuantity(days)
            .WithUnitPrice(price)
            .WithUnit("DAY")
            .WithVatRate(20);

    /// <summary>
    /// Maintenance service line
    /// </summary>
    public static Action<InvoiceLineBuilder> Maintenance(decimal monthlyPrice, int months) =>
        line => line
            .WithName("Bakım ve Destek Hizmeti")
            .WithQuantity(months)
            .WithUnitPrice(monthlyPrice)
            .WithUnit("MON")
            .WithVatRate(20);

    /// <summary>
    /// Custom development line
    /// </summary>
    public static Action<InvoiceLineBuilder> CustomDevelopment(decimal totalPrice) =>
        line => line
            .WithName("Özel Yazılım Geliştirme")
            .WithQuantity(1)
            .WithUnitPrice(totalPrice)
            .WithUnit("C62")
            .WithVatRate(20);

    /// <summary>
    /// API usage line
    /// </summary>
    public static Action<InvoiceLineBuilder> ApiUsage(decimal pricePerCall, decimal calls) =>
        line => line
            .WithName("API Kullanım Ücreti")
            .WithQuantity(calls)
            .WithUnitPrice(pricePerCall)
            .WithUnit("C62")
            .WithVatRate(20);
}

/// <summary>
/// Withholding tax codes and rates
/// </summary>
public static class WithholdingCodes
{
    /// <summary>
    /// Construction work (4/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Construction => (0.40m, "601", "Yapım İşleri");

    /// <summary>
    /// Cleaning services (9/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Cleaning => (0.90m, "602", "Temizlik Hizmetleri");

    /// <summary>
    /// Security services (9/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Security => (0.90m, "603", "Güvenlik Hizmetleri");

    /// <summary>
    /// Personnel services (9/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Personnel => (0.90m, "604", "İşgücü Temin Hizmetleri");

    /// <summary>
    /// Catering services (5/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Catering => (0.50m, "605", "Yemek Servis Hizmetleri");

    /// <summary>
    /// Machine/equipment rental (5/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) EquipmentRental => (0.50m, "606", "Makine/Ekipman Kiralama");

    /// <summary>
    /// Cargo/transport services (2/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Transport => (0.20m, "607", "Kargo/Nakliye Hizmetleri");

    /// <summary>
    /// Advertising services (3/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Advertising => (0.30m, "608", "Reklam Hizmetleri");

    /// <summary>
    /// Tourism services (2/10)
    /// </summary>
    public static (decimal Rate, string Code, string Reason) Tourism => (0.20m, "609", "Turizm Hizmetleri");
}

/// <summary>
/// VAT exemption codes and reasons
/// </summary>
public static class ExemptionCodes
{
    /// <summary>
    /// Goods export
    /// </summary>
    public static (string Code, string Reason) GoodsExport => ("301", "Mal İhracatı");

    /// <summary>
    /// Service export
    /// </summary>
    public static (string Code, string Reason) ServiceExport => ("302", "Hizmet İhracatı");

    /// <summary>
    /// Transit trade
    /// </summary>
    public static (string Code, string Reason) TransitTrade => ("303", "Transit Ticaret");

    /// <summary>
    /// Free zone delivery
    /// </summary>
    public static (string Code, string Reason) FreeZone => ("304", "Serbest Bölge Teslimi");

    /// <summary>
    /// Diplomatic exemption
    /// </summary>
    public static (string Code, string Reason) Diplomatic => ("305", "Diplomatik İstisna");

    /// <summary>
    /// International organization
    /// </summary>
    public static (string Code, string Reason) InternationalOrg => ("306", "Uluslararası Kuruluş İstisnası");

    /// <summary>
    /// Investment incentive
    /// </summary>
    public static (string Code, string Reason) InvestmentIncentive => ("350", "Yatırım Teşvik Belgesi");
}
