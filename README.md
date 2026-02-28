# Entegre.Ets.Sdk

Entegre ETS API için resmi .NET SDK - E-Fatura, E-Arşiv, E-İrsaliye, E-Müstahsil Makbuzu entegrasyonu.

## Kurulum

```bash
dotnet add package Entegre.Ets.Sdk
```

## Hızlı Başlangıç

### Temel Kullanım

```csharp
using Entegre.Ets.Sdk;
using Entegre.Ets.Sdk.Builders;
using Entegre.Ets.Sdk.Models.Invoice;

// Client oluştur
var client = new EtsClient(options =>
{
    options.BaseUrl = "https://ets-api.entegre.net";
    options.ApiKey = "your-api-key";
    options.ApiSecret = "your-api-secret";
    options.CustomerId = "your-customer-id";
    options.SoftwareId = "your-software-id";
});

// E-Fatura kullanıcısı kontrol
var userResult = await client.CheckEInvoiceUserAsync("1234567890");
if (userResult.Data?.IsEInvoiceUser == true)
{
    Console.WriteLine($"E-Fatura kullanıcısı: {userResult.Data.Title}");
}

// Fatura gönder
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Satis)
    .WithDate(DateTime.Now)
    .WithSender("1234567890", "Gönderici Firma", p => p
        .WithTaxOffice("Kadıköy")
        .WithAddress("İstanbul", "Caferağa Mah."))
    .WithReceiver("0987654321", "Alıcı Firma", p => p
        .WithTaxOffice("Beşiktaş")
        .WithAddress("İstanbul"))
    .AddLine("Ürün 1", quantity: 10, unitPrice: 100, vatRate: 20)
    .AddLine("Ürün 2", quantity: 5, unitPrice: 200, vatRate: 20)
    .WithNote("Ödeme vadesi 30 gündür")
    .Build();

var result = await client.SendInvoiceAsync(invoice);

if (result.Success)
{
    Console.WriteLine($"Fatura gönderildi: {result.Data?.Uuid}");
}
```

### ASP.NET Core Entegrasyonu

```csharp
// Program.cs
using Entegre.Ets.Sdk.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEtsClient(options =>
{
    options.BaseUrl = builder.Configuration["Ets:BaseUrl"]!;
    options.ApiKey = builder.Configuration["Ets:ApiKey"]!;
    options.ApiSecret = builder.Configuration["Ets:ApiSecret"]!;
    options.CustomerId = builder.Configuration["Ets:CustomerId"]!;
    options.SoftwareId = builder.Configuration["Ets:SoftwareId"]!;
});

// Controller'da kullanım
public class InvoiceController : ControllerBase
{
    private readonly IEtsClient _etsClient;

    public InvoiceController(IEtsClient etsClient)
    {
        _etsClient = etsClient;
    }

    [HttpPost]
    public async Task<IActionResult> SendInvoice([FromBody] InvoiceRequest invoice)
    {
        var result = await _etsClient.SendInvoiceAsync(invoice);
        return result.Success ? Ok(result.Data) : BadRequest(result);
    }
}
```

## Fatura Tipleri

### Satış Faturası

```csharp
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Satis)
    .WithSender("1234567890", "Satıcı Firma")
    .WithReceiver("0987654321", "Alıcı Firma")
    .AddLine("Ürün", 1, 1000, vatRate: 20)
    .Build();
```

### Tevkifatlı Fatura

```csharp
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Tevkifat)
    .WithSender("1234567890", "Satıcı Firma")
    .WithReceiver("0987654321", "Alıcı Firma")
    .AddLine("Hizmet", 1, 10000, vatRate: 20)
    .WithWithholding(rate: 0.90m, reasonCode: "601", reason: "Yapım işleri")
    .Build();
```

### İade Faturası

```csharp
var invoice = InvoiceBuilder.Create()
    .WithType(InvoiceType.Iade)
    .WithSender("1234567890", "Satıcı Firma")
    .WithReceiver("0987654321", "Alıcı Firma")
    .AddLine("İade Ürün", 1, 500, vatRate: 20)
    .WithNote("Orijinal fatura no: ABC2024000000001")
    .Build();
```

## E-İrsaliye

```csharp
using Entegre.Ets.Sdk.Models.Dispatch;

var dispatch = new DispatchRequest
{
    DispatchType = DispatchType.Sevk,
    IssueDate = DateTime.Now,
    Sender = new Party { TaxId = "1234567890", Name = "Gönderici" },
    Receiver = new Party { TaxId = "0987654321", Name = "Alıcı" },
    Shipment = new ShipmentInfo
    {
        ShipmentDate = DateTime.Now,
        VehiclePlate = "34ABC123",
        Driver = new DriverInfo
        {
            Name = "Ahmet",
            Surname = "Yılmaz",
            Tckn = "12345678901"
        }
    },
    Lines =
    [
        new DispatchLine { Name = "Ürün 1", Quantity = 100, UnitCode = "KGM" },
        new DispatchLine { Name = "Ürün 2", Quantity = 50, UnitCode = "C62" }
    ]
};

var result = await client.SendDispatchAsync(dispatch);
```

## E-Müstahsil Makbuzu

```csharp
using Entegre.Ets.Sdk.Models.ProducerReceipt;

var receipt = new ProducerReceiptRequest
{
    IssueDate = DateTime.Now,
    Buyer = new Party { TaxId = "1234567890", Name = "Alıcı Firma" },
    Producer = new ProducerInfo
    {
        Tckn = "12345678901",
        FirstName = "Mehmet",
        LastName = "Yılmaz",
        Address = new Address { City = "Antalya", District = "Merkez" }
    },
    Lines =
    [
        new ProducerReceiptLine
        {
            Name = "Domates",
            Quantity = 1000,
            UnitCode = "KGM",
            UnitPrice = 15,
            StopajRate = 2
        }
    ]
};

var result = await client.SendProducerReceiptAsync(receipt);
```

## Validasyon

### Vergi Numarası Doğrulama

```csharp
using Entegre.Ets.Sdk.Validation;

// VKN doğrula
var vknResult = TaxIdValidator.ValidateVkn("1234567890");
if (!vknResult.IsValid)
{
    Console.WriteLine($"Hata: {vknResult.ErrorMessage}");
}

// TCKN doğrula
var tcknResult = TaxIdValidator.ValidateTckn("12345678901");

// Otomatik algıla (10 hane = VKN, 11 hane = TCKN)
var taxIdResult = TaxIdValidator.ValidateTaxId("1234567890");
```

### IBAN Doğrulama

```csharp
// IBAN doğrula
var ibanResult = IbanValidator.Validate("TR320010009999901234567890");

// Türk IBAN'ı doğrula
var trIbanResult = IbanValidator.ValidateTurkishIban("TR320010009999901234567890");

// IBAN formatla
var formatted = IbanValidator.Format("TR320010009999901234567890");
// Sonuç: "TR32 0010 0099 9990 1234 5678 90"
```

## Hesaplama

### Fatura Toplamları

```csharp
var (invoice, totals) = InvoiceBuilder.Create()
    .WithSender("1234567890", "Satıcı")
    .WithReceiver("0987654321", "Alıcı")
    .AddLine("Ürün 1", 10, 100, vatRate: 20)
    .AddLine("Ürün 2", 5, 200, vatRate: 20, l => l.WithDiscountRate(10))
    .BuildWithTotals();

Console.WriteLine($"Ara Toplam: {totals.Subtotal:C}");
Console.WriteLine($"KDV: {totals.TotalVat:C}");
Console.WriteLine($"İndirim: {totals.TotalDiscount:C}");
Console.WriteLine($"Genel Toplam: {totals.GrandTotal:C}");
```

## Konfigürasyon

### appsettings.json

```json
{
  "Ets": {
    "BaseUrl": "https://ets-api.entegre.net",
    "ApiKey": "your-api-key",
    "ApiSecret": "your-api-secret",
    "CustomerId": "your-customer-id",
    "SoftwareId": "your-software-id",
    "Timeout": "00:00:30",
    "EnableRetry": true,
    "MaxRetries": 3
  }
}
```

### Retry ve Timeout

```csharp
var client = new EtsClient(options =>
{
    options.Timeout = TimeSpan.FromSeconds(60);
    options.EnableRetry = true;
    options.MaxRetries = 5;
    options.RetryDelayMs = 2000; // 2 saniye başlangıç gecikmesi
});
```

## Test Desteği

```csharp
// Mock handler ile test
var mockHandler = new MockHttpMessageHandler();
mockHandler.When("*").Respond("application/json",
    JsonSerializer.Serialize(new ApiResponse<InvoiceResult>
    {
        Success = true,
        Data = new InvoiceResult { Uuid = "test-uuid" }
    }));

var client = new EtsClient(options =>
{
    options.HttpMessageHandler = mockHandler;
    options.ApiKey = "test";
    options.ApiSecret = "test";
});

var result = await client.SendInvoiceAsync(invoice);
Assert.True(result.Success);
```

## Gereksinimler

- .NET 6.0, .NET 7.0 veya .NET 8.0
- System.Text.Json

## Geliştirici

On2 Elektronik Ltd. Şti.

## Lisans

MIT

## Destek

- E-posta: destek@entegreyazilim.com.tr
- GitHub Issues: https://github.com/Entegre/entegre-ets-dotnet-sdk/issues
- Dokümantasyon: https://docs.entegre.net
