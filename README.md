# Entegre.Ets.Sdk

[![NuGet](https://img.shields.io/nuget/v/Entegre.Ets.Sdk.svg)](https://www.nuget.org/packages/Entegre.Ets.Sdk)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)
[![.NET](https://img.shields.io/badge/.NET-6.0%20%7C%207.0%20%7C%208.0%20%7C%209.0%20%7C%2010.0-blue.svg)](https://dotnet.microsoft.com/)

Entegre ETS API için resmi .NET SDK. E-Fatura, E-Arşiv, E-İrsaliye ve E-Müstahsil işlemlerini kolayca yapmanızı sağlar.

## Özellikler

- 🚀 **Kolay Entegrasyon** - Builder pattern ile hızlı fatura oluşturma
- 📦 **Tam Tip Güvenliği** - C# ile compile-time hata kontrolü
- 🔄 **Otomatik Hesaplama** - KDV, tevkifat, indirim otomatik
- 🛡️ **Doğrulama** - VKN/TCKN/IBAN algoritma kontrolü
- ⚡ **Batch İşlemler** - Toplu fatura gönderimi
- 🔌 **Webhook** - Status değişikliği bildirimleri
- 📄 **PDF & XML** - PDF indirme ve UBL-TR XML desteği
- 🧪 **Test Araçları** - Mock client ve fixtures
- 💉 **DI Desteği** - ASP.NET Core entegrasyonu

## Kurulum

```bash
dotnet add package Entegre.Ets.Sdk
```

## Hızlı Başlangıç

```csharp
using Entegre.Ets.Sdk;
using Entegre.Ets.Sdk.Builders;
using Entegre.Ets.Sdk.Models.Invoice;

// 1. Client oluştur (Production)
var client = new EtsClient(options =>
{
    options.ApiKey = "your-api-key";
    options.ApiSecret = "your-api-secret";
    options.CustomerId = "your-customer-id";
    options.SoftwareId = "your-software-id";
});

// veya Test ortamı için
var testClient = new EtsClient(options =>
{
    options.UseTestEnvironment();  // https://ets-test.bulutix.com
    options.ApiKey = "your-api-key";
    options.ApiSecret = "your-api-secret";
    options.CustomerId = "your-customer-id";
    options.SoftwareId = "your-software-id";
});

// 2. E-Fatura kullanıcısı kontrol
var userCheck = await client.CheckEInvoiceUserAsync("9876543210");
if (userCheck.Data?.IsEInvoiceUser == true)
{
    Console.WriteLine($"E-Fatura mükellefi: {userCheck.Data.Title}");
}

// 3. Fatura oluştur (Builder ile)
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Satis)
    .WithDate(DateTime.Now)
    .WithSender("1234567890", "Satıcı Firma A.Ş.", p => p
        .WithTaxOffice("Kadıköy VD")
        .WithAddress("İstanbul", "Caferağa Mah."))
    .WithReceiver("9876543210", "Alıcı Firma Ltd.", p => p
        .WithTaxOffice("Çankaya VD")
        .WithAddress("Ankara"))
    .AddLine("Yazılım Lisansı", quantity: 1, unitPrice: 1000, vatRate: 20)
    .AddLine("Destek Paketi", quantity: 12, unitPrice: 100, vatRate: 20)
    .WithNote("Ödeme vadesi 30 gündür")
    .Build();

// 4. Gönder
var result = await client.SendInvoiceAsync(invoice);
Console.WriteLine($"UUID: {result.Data?.Uuid}");
```

---

# 📚 Entegrasyon Rehberi

## İçindekiler

1. [ASP.NET Core Entegrasyonu](#aspnet-core-entegrasyonu)
2. [Fatura İşlemleri](#fatura-i̇şlemleri)
3. [E-İrsaliye İşlemleri](#e-i̇rsaliye-i̇şlemleri)
4. [E-Müstahsil Makbuzu](#e-müstahsil-makbuzu)
5. [İndirim ve Tevkifat](#i̇ndirim-ve-tevkifat)
6. [Doğrulama](#doğrulama)
7. [PDF ve XML](#pdf-ve-xml)
8. [Webhook Entegrasyonu](#webhook-entegrasyonu)
9. [Toplu İşlemler](#toplu-i̇şlemler)
10. [Test ve Mock](#test-ve-mock)
11. [Hata Yönetimi](#hata-yönetimi)

---

## ASP.NET Core Entegrasyonu

### Servis Kaydı

```csharp
// Program.cs
using Entegre.Ets.Sdk.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Konfigürasyondan oku
builder.Services.AddEtsClient(options =>
{
    options.BaseUrl = builder.Configuration["Ets:BaseUrl"]!;
    options.ApiKey = builder.Configuration["Ets:ApiKey"]!;
    options.ApiSecret = builder.Configuration["Ets:ApiSecret"]!;
    options.CustomerId = builder.Configuration["Ets:CustomerId"]!;
    options.SoftwareId = builder.Configuration["Ets:SoftwareId"]!;
    options.EnableRetry = true;
    options.MaxRetries = 3;
});

var app = builder.Build();
```

### appsettings.json

```json
{
  "Ets": {
    "BaseUrl": "https://ets.bulutix.com",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "CustomerId": "your-customer-id",
    "SoftwareId": "your-software-id"
  }
}
```

### Ortam Ayarları

| Ortam | URL |
|-------|-----|
| **Production** | `https://ets.bulutix.com` |
| **Test/Sandbox** | `https://ets-test.bulutix.com` |

```csharp
// Production (varsayılan)
options.UseProductionEnvironment();

// Test ortamı
options.UseTestEnvironment();
```

### Controller Kullanımı

```csharp
using Entegre.Ets.Sdk;
using Entegre.Ets.Sdk.Builders;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class InvoicesController : ControllerBase
{
    private readonly IEtsClient _etsClient;

    public InvoicesController(IEtsClient etsClient)
    {
        _etsClient = etsClient;
    }

    [HttpPost]
    public async Task<IActionResult> SendInvoice([FromBody] CreateInvoiceDto dto)
    {
        var invoice = InvoiceBuilder.Create()
            .WithType(dto.InvoiceType)
            .WithSender(dto.SenderTaxId, dto.SenderName)
            .WithReceiver(dto.ReceiverTaxId, dto.ReceiverName)
            .AddLine(dto.ItemName, dto.Quantity, dto.UnitPrice, dto.VatRate)
            .Build();

        var result = await _etsClient.SendInvoiceAsync(invoice);

        return result.Success
            ? Ok(result.Data)
            : BadRequest(result);
    }

    [HttpGet("{uuid}/status")]
    public async Task<IActionResult> GetStatus(string uuid)
    {
        var result = await _etsClient.GetInvoiceStatusAsync(
            new InvoiceStatusRequest { Uuid = uuid });

        return Ok(result);
    }

    [HttpGet("check-user/{taxId}")]
    public async Task<IActionResult> CheckUser(string taxId)
    {
        var result = await _etsClient.CheckEInvoiceUserAsync(taxId);
        return Ok(result);
    }
}
```

### Minimal API

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEtsClient(options => { /* ... */ });

var app = builder.Build();

app.MapPost("/invoices", async (IEtsClient client, InvoiceRequest invoice) =>
{
    var result = await client.SendInvoiceAsync(invoice);
    return result.Success ? Results.Ok(result.Data) : Results.BadRequest(result);
});

app.MapGet("/invoices/{uuid}/status", async (IEtsClient client, string uuid) =>
{
    var result = await client.GetInvoiceStatusAsync(new() { Uuid = uuid });
    return Results.Ok(result);
});

app.Run();
```

---

## Fatura İşlemleri

### Fatura Tipleri

| Tip | Enum | Açıklama |
|-----|------|----------|
| Satış | `InvoiceType.Satis` | Standart satış faturası |
| İade | `InvoiceType.Iade` | İade faturası |
| Tevkifat | `InvoiceType.Tevkifat` | Tevkifatlı fatura |
| İstisna | `InvoiceType.Istisna` | İstisna faturası |
| İhraç Kayıtlı | `InvoiceType.IhracKayitli` | İhraç kayıtlı fatura |
| SGK | `InvoiceType.SGK` | SGK faturası |

### Satış Faturası

```csharp
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Satis)
    .WithDocumentType(DocumentType.EFatura)
    .WithDate(DateTime.Now)
    .WithCurrency("TRY")
    .WithSender("1234567890", "Satıcı Firma A.Ş.", p => p
        .WithTaxOffice("Kadıköy VD")
        .WithAddress("İstanbul", "Caferağa Mah. No: 1")
        .WithContact(phone: "0216 555 0000", email: "info@satici.com"))
    .WithReceiver("9876543210", "Alıcı Firma Ltd.", p => p
        .WithTaxOffice("Çankaya VD")
        .WithAddress("Ankara", "Kızılay Mah."))
    .AddLine("Yazılım Lisansı", quantity: 1, unitPrice: 10000, vatRate: 20, l => l
        .WithUnit("C62")
        .WithDescription("Yıllık lisans"))
    .AddLine("Teknik Destek", quantity: 12, unitPrice: 500, vatRate: 20)
    .WithNote("Ödeme vadesi 30 gündür")
    .WithPaymentTerms(
        dueDate: DateTime.Now.AddDays(30),
        iban: "TR330006100519786457841326",
        note: "Havale/EFT ile ödeme")
    .Build();
```

### İade Faturası

```csharp
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Iade)
    .WithSender("1234567890", "Satıcı Firma")
    .WithReceiver("9876543210", "Alıcı Firma")
    .AddLine("İade Ürün", quantity: 1, unitPrice: 500, vatRate: 20)
    .WithNote("Orijinal fatura no: ABC2024000000001")
    .Build();
```

### Hesaplama ile Build

```csharp
var (invoice, totals) = InvoiceBuilder.Create()
    .WithSender("1234567890", "Satıcı")
    .WithReceiver("9876543210", "Alıcı")
    .AddLine("Ürün 1", 10, 100, vatRate: 20)
    .AddLine("Ürün 2", 5, 200, vatRate: 20, l => l.WithDiscountRate(10))
    .BuildWithTotals();

Console.WriteLine($"Ara Toplam: {totals.Subtotal:C}");      // 1.900,00 ₺
Console.WriteLine($"KDV: {totals.TotalVat:C}");             // 380,00 ₺
Console.WriteLine($"İndirim: {totals.TotalDiscount:C}");    // 100,00 ₺
Console.WriteLine($"Genel Toplam: {totals.GrandTotal:C}");  // 2.280,00 ₺
```

### Durum Sorgulama

```csharp
var status = await client.GetInvoiceStatusAsync(new InvoiceStatusRequest
{
    Uuid = "12345678-1234-1234-1234-123456789012"
});

Console.WriteLine($"Durum: {status.Data?.Status}");
// SENT, DELIVERED, ACCEPTED, REJECTED, FAILED
```

---

## E-İrsaliye İşlemleri

### İrsaliye Oluşturma

```csharp
using Entegre.Ets.Sdk.Builders;
using Entegre.Ets.Sdk.Models.Dispatch;

var dispatch = DispatchBuilder.Create()
    .WithType(DispatchType.Sevk)
    .WithDate(DateTime.Now)
    .WithSender("1234567890", "Gönderici Firma", p => p
        .WithAddress("İstanbul"))
    .WithReceiver("9876543210", "Alıcı Firma", p => p
        .WithAddress("Ankara"))
    .WithShipment(s => s
        .WithDate(DateTime.Now)
        .WithVehicle("34 ABC 123")
        .WithDriver("Ahmet", "Yılmaz", "12345678901")
        .WithDeliveryAddress("Ankara", "Kızılay Mah. No: 5"))
    .AddLine("Ürün A", quantity: 100, unitCode: "KGM")
    .AddLine("Ürün B", quantity: 50, unitCode: "C62")
    .Build();

var result = await client.SendDispatchAsync(dispatch);
```

### İrsaliye Tipleri

| Tip | Enum | Açıklama |
|-----|------|----------|
| Sevk | `DispatchType.Sevk` | Sevk irsaliyesi |
| Satış | `DispatchType.Satis` | Satış irsaliyesi |
| İade | `DispatchType.Iade` | İade irsaliyesi |

---

## E-Müstahsil Makbuzu

```csharp
using Entegre.Ets.Sdk.Builders;
using Entegre.Ets.Sdk.Models.ProducerReceipt;

var receipt = ProducerReceiptBuilder.Create()
    .WithDate(DateTime.Now)
    .WithBuyer("1234567890", "Alıcı Firma A.Ş.", p => p
        .WithTaxOffice("Antalya VD")
        .WithAddress("Antalya"))
    .WithProducer("12345678901", "Mehmet", "Yılmaz", p => p
        .WithAddress("Antalya", "Merkez"))
    .AddLine("Domates", quantity: 1000, unitPrice: 15, unitCode: "KGM", stopajRate: 2)
    .AddLine("Biber", quantity: 500, unitPrice: 20, unitCode: "KGM", stopajRate: 2)
    .Build();

var result = await client.SendProducerReceiptAsync(receipt);
```

---

## İndirim ve Tevkifat

### Satır İndirimi

```csharp
var invoice = InvoiceBuilder.Create()
    // ...
    .AddLine("Ürün", 10, 100, vatRate: 20, l => l
        .WithDiscountRate(10))  // %10 indirim
    // veya
    .AddLine("Ürün 2", 5, 200, vatRate: 20, l => l
        .WithDiscountAmount(50))  // 50 TL indirim
    .Build();
```

### Tevkifatlı Fatura

```csharp
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Tevkifat)
    .WithSender("1234567890", "Satıcı Firma")
    .WithReceiver("9876543210", "Alıcı Firma")
    .WithWithholding(
        rate: 0.90m,           // 9/10 tevkifat
        reasonCode: "603",     // Güvenlik hizmetleri
        reason: "Güvenlik Hizmetleri")
    .AddLine("Güvenlik Hizmeti", quantity: 1, unitPrice: 10000, vatRate: 20)
    .Build();

// Hesaplama:
// Satır toplamı: 10.000 TL
// KDV: 2.000 TL
// Tevkifat: 1.800 TL (KDV'nin %90'ı)
// Ödenecek: 10.200 TL
```

### Tevkifat Kodları

| Kod | Açıklama | Oran |
|-----|----------|------|
| 601 | Yapım işleri | 4/10 |
| 602 | Temizlik hizmetleri | 9/10 |
| 603 | Güvenlik hizmetleri | 9/10 |
| 604 | Personel hizmetleri | 9/10 |
| 605 | Yemek hizmetleri | 5/10 |
| 606 | Makine/ekipman kiralama | 5/10 |

---

## Doğrulama

### Vergi Numarası

```csharp
using Entegre.Ets.Sdk.Validation;

// VKN doğrula
var vknResult = TaxIdValidator.ValidateVkn("1234567890");
if (!vknResult.IsValid)
{
    Console.WriteLine($"VKN Hatası: {vknResult.ErrorMessage}");
}

// TCKN doğrula
var tcknResult = TaxIdValidator.ValidateTckn("12345678901");

// Otomatik algıla (10 hane = VKN, 11 hane = TCKN)
var taxIdResult = TaxIdValidator.ValidateTaxId("1234567890");

// Format kontrolü (checksum olmadan)
bool isValid = TaxIdValidator.IsValidFormat("1234567890");
```

### IBAN Doğrulama

```csharp
using Entegre.Ets.Sdk.Validation;

// IBAN doğrula (MOD-97)
var ibanResult = IbanValidator.Validate("TR330006100519786457841326");

// Türk IBAN'ı doğrula
var trResult = IbanValidator.ValidateTurkishIban("TR330006100519786457841326");

// IBAN formatla
var formatted = IbanValidator.Format("TR330006100519786457841326");
// Sonuç: "TR33 0006 1005 1978 6457 8413 26"
```

---

## PDF ve XML

### PDF İndirme

```csharp
using Entegre.Ets.Sdk.Documents;

// PDF indir
var pdf = await client.GetInvoicePdfAsync("invoice-uuid");
if (pdf.Success && pdf.Data != null)
{
    // Base64'ten byte array'e çevir
    var bytes = Convert.FromBase64String(pdf.Data.PdfContent);
    await File.WriteAllBytesAsync("fatura.pdf", bytes);
}

// Stream olarak al
await using var stream = await client.GetInvoicePdfStreamAsync("invoice-uuid");
await using var file = File.Create("fatura.pdf");
await stream.CopyToAsync(file);
```

### XML İşlemleri

```csharp
using Entegre.Ets.Sdk.Xml;

// Faturayı XML'e çevir
var xmlContent = XmlSerializer.ToUblTr(invoice);
await File.WriteAllTextAsync("fatura.xml", xmlContent);

// XML'den fatura oku
var xmlString = await File.ReadAllTextAsync("fatura.xml");
var parsedInvoice = XmlParser.ParseInvoice(xmlString);

Console.WriteLine($"Fatura No: {parsedInvoice.InvoiceNumber}");
Console.WriteLine($"Toplam: {parsedInvoice.PayableAmount:C}");

// Base64 XML parse
var base64Xml = "PEludm9pY2U+Li4uPC9JbnZvaWNlPg==";
var invoice = XmlParser.ParseInvoiceFromBase64(base64Xml);
```

---

## Webhook Entegrasyonu

### ASP.NET Core Webhook Handler

```csharp
using Entegre.Ets.Sdk.Webhooks;

// Program.cs
builder.Services.AddEtsWebhook(options =>
{
    options.Secret = builder.Configuration["Ets:WebhookSecret"]!;
    options.TimestampTolerance = TimeSpan.FromMinutes(5);
});

// WebhookController.cs
[ApiController]
[Route("api/webhooks")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly IInvoiceService _invoiceService;

    public WebhookController(
        IWebhookHandler webhookHandler,
        IInvoiceService invoiceService)
    {
        _webhookHandler = webhookHandler;
        _invoiceService = invoiceService;
    }

    [HttpPost("ets")]
    public async Task<IActionResult> HandleWebhook()
    {
        var signature = Request.Headers["X-Ets-Signature"].FirstOrDefault();
        var timestamp = Request.Headers["X-Ets-Timestamp"].FirstOrDefault();

        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync();

        try
        {
            var webhookEvent = await _webhookHandler.ProcessAsync(
                payload, signature, timestamp);

            switch (webhookEvent.EventType)
            {
                case WebhookEventType.InvoiceSent:
                    await _invoiceService.UpdateStatusAsync(
                        webhookEvent.DocumentUuid, "SENT");
                    break;

                case WebhookEventType.InvoiceAccepted:
                    await _invoiceService.UpdateStatusAsync(
                        webhookEvent.DocumentUuid, "ACCEPTED");
                    break;

                case WebhookEventType.InvoiceRejected:
                    await _invoiceService.UpdateStatusAsync(
                        webhookEvent.DocumentUuid, "REJECTED",
                        webhookEvent.ErrorMessage);
                    break;
            }

            return Ok(new { received = true });
        }
        catch (WebhookSignatureException)
        {
            return Unauthorized();
        }
    }
}
```

### Event Tipleri

| Event | Açıklama |
|-------|----------|
| `InvoiceSent` | Fatura gönderildi |
| `InvoiceDelivered` | Fatura iletildi |
| `InvoiceAccepted` | Fatura kabul edildi |
| `InvoiceRejected` | Fatura reddedildi |
| `InvoiceFailed` | Fatura başarısız |
| `DispatchSent` | İrsaliye gönderildi |
| `DispatchDelivered` | İrsaliye iletildi |

---

## Toplu İşlemler

### Batch Gönderim

```csharp
using Entegre.Ets.Sdk.Batch;

var invoices = new List<InvoiceRequest> { invoice1, invoice2, invoice3 };

var result = await BatchProcessor.ProcessAsync(
    invoices,
    async (invoice, index, ct) => await client.SendInvoiceAsync(invoice, ct),
    new BatchOptions
    {
        Concurrency = 5,           // Paralel işlem sayısı
        ContinueOnError = true,    // Hata olsa da devam et
        Retries = 2,               // Başarısız olursa yeniden dene
        DelayBetweenMs = 100       // İşlemler arası bekleme
    },
    (completed, total, lastResult) =>
    {
        var percent = (int)((double)completed / total * 100);
        Console.WriteLine($"İlerleme: {percent}% ({completed}/{total})");

        if (!lastResult.Success)
        {
            Console.WriteLine($"Hata #{lastResult.Index}: {lastResult.Error?.Message}");
        }
    });

Console.WriteLine($"Toplam: {result.Total}");
Console.WriteLine($"Başarılı: {result.Successful}");
Console.WriteLine($"Başarısız: {result.Failed}");
Console.WriteLine($"Süre: {result.Duration.TotalSeconds:F2}s");

// Başarısız olanları listele
var failed = result.Results.Where(r => !r.Success);
foreach (var f in failed)
{
    Console.WriteLine($"#{f.Index}: {f.Error?.Message}");
}
```

### Paralel Limiter

```csharp
using Entegre.Ets.Sdk.Batch;

// Basit paralel işlem
var results = await Batch.ParallelAsync(
    invoices,
    invoice => client.SendInvoiceAsync(invoice),
    maxConcurrency: 5);
```

---

## Test ve Mock

### Mock Client

```csharp
using Entegre.Ets.Sdk.Testing;

// Test için mock client
var mockClient = new MockEtsClient(new MockClientOptions
{
    SimulatedDelay = TimeSpan.FromMilliseconds(100),
    FailureRate = 0.1  // %10 rastgele hata
});

// Test kullanıcısı ekle
mockClient.AddEInvoiceUser("1234567890", "Test Firma",
    ["urn:mail:test@test.com"]);

// Normal client gibi kullan
var result = await mockClient.SendInvoiceAsync(invoice);
Console.WriteLine($"UUID: {result.Data?.Uuid}");

// Durumu manuel değiştir
mockClient.SetInvoiceStatus(result.Data!.Uuid, "ACCEPTED");

// Webhook simüle et
await mockClient.SimulateWebhookAsync(
    result.Data.Uuid,
    WebhookEventType.InvoiceAccepted);
```

### xUnit Test

```csharp
using Entegre.Ets.Sdk.Testing;
using Xunit;

public class InvoiceTests
{
    private readonly MockEtsClient _client;

    public InvoiceTests()
    {
        _client = new MockEtsClient();
    }

    [Fact]
    public async Task SendInvoice_ShouldReturnUuid()
    {
        // Arrange
        var invoice = InvoiceBuilder.Create()
            .WithSender("1234567890", "Satıcı")
            .WithReceiver("9876543210", "Alıcı")
            .AddLine("Ürün", 1, 100, 20)
            .Build();

        // Act
        var result = await _client.SendInvoiceAsync(invoice);

        // Assert
        Assert.True(result.Success);
        Assert.NotNull(result.Data?.Uuid);
    }

    [Theory]
    [InlineData("1234567890", true)]
    [InlineData("0000000000", false)]
    public void ValidateVkn_ShouldValidateCorrectly(string vkn, bool expected)
    {
        var result = TaxIdValidator.ValidateVkn(vkn);
        Assert.Equal(expected, result.IsValid);
    }
}
```

### Test Fixtures

```csharp
using Entegre.Ets.Sdk.Testing;

// Hazır test verileri
var supplier = TestFixtures.Supplier;
var customer = TestFixtures.Customer;
var lines = TestFixtures.InvoiceLines;

// Rastgele veri üretimi
var randomVkn = TestGenerators.RandomVkn();      // "3847562910"
var randomTckn = TestGenerators.RandomTckn();    // "28374651029"
var randomIban = TestGenerators.RandomIban();    // "TR33..."
```

---

## Hata Yönetimi

### Exception Türleri

```csharp
using Entegre.Ets.Sdk;

try
{
    await client.SendInvoiceAsync(invoice);
}
catch (EtsAuthenticationException ex)
{
    // API key/secret hatalı
    Console.WriteLine("Kimlik doğrulama hatası");
}
catch (EtsValidationException ex)
{
    // Doğrulama hatası
    foreach (var error in ex.Errors)
    {
        Console.WriteLine($"{error.Field}: {error.Message}");
    }
}
catch (EtsApiException ex)
{
    // Genel API hatası
    Console.WriteLine($"API Hatası: {ex.Message}");
    Console.WriteLine($"HTTP Kod: {ex.StatusCode}");
}
```

### Retry Ayarları

```csharp
var client = new EtsClient(options =>
{
    options.EnableRetry = true;
    options.MaxRetries = 5;
    options.RetryDelayMs = 2000;  // 2 saniye başlangıç
    options.Timeout = TimeSpan.FromSeconds(60);
});
```

---

## Sabitler

### Birim Kodları

| Kod | Açıklama |
|-----|----------|
| `C62` | Adet |
| `KGM` | Kilogram |
| `LTR` | Litre |
| `MTR` | Metre |
| `MTK` | Metrekare |
| `HUR` | Saat |
| `DAY` | Gün |
| `MON` | Ay |

### Para Birimleri

| Kod | Açıklama |
|-----|----------|
| `TRY` | Türk Lirası |
| `USD` | Amerikan Doları |
| `EUR` | Euro |
| `GBP` | İngiliz Sterlini |

---

## Geliştirici

**On2 Elektronik Ltd. Şti.**

## Destek

- 📧 E-posta: destek@entegreyazilim.com.tr
- 🐛 [Hata Bildirimi](https://github.com/Entegre/entegre-ets-dotnet-sdk/issues)
- 📖 [API Dokümantasyonu](https://docs.entegre.net)

## Lisans

MIT
