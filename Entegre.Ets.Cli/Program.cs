using System.CommandLine;
using System.Text.Json;
using Entegre.Ets.Sdk;
using Entegre.Ets.Sdk.Builders;
using Entegre.Ets.Sdk.Models.Invoice;
using Entegre.Ets.Sdk.Validation;

namespace Entegre.Ets.Cli;

class Program
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Entegre ETS CLI - E-Fatura, E-Arşiv, E-İrsaliye komut satırı aracı");

        // Check user command
        var checkUserCommand = new Command("check-user", "E-Fatura mükellefi sorgula");
        var taxIdArgument = new Argument<string>("taxId", "Vergi/TC Kimlik numarası");
        var jsonOption = new Option<bool>("--json", "JSON formatında çıktı");
        var testOption = new Option<bool>("--test", "Test ortamını kullan");

        checkUserCommand.AddArgument(taxIdArgument);
        checkUserCommand.AddOption(jsonOption);
        checkUserCommand.AddOption(testOption);

        checkUserCommand.SetHandler(async (taxId, json, useTest) =>
        {
            var client = CreateClient(useTest);
            var result = await client.CheckEInvoiceUserAsync(taxId);

            if (json)
            {
                Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
            }
            else
            {
                if (result.Success && result.Data != null)
                {
                    Console.WriteLine($"VKN/TCKN: {result.Data.TaxId}");
                    Console.WriteLine($"E-Fatura Mükellefi: {(result.Data.IsEInvoiceUser ? "Evet" : "Hayır")}");
                    if (result.Data.IsEInvoiceUser)
                    {
                        Console.WriteLine($"Unvan: {result.Data.Title}");
                        if (result.Data.Aliases?.Count > 0)
                        {
                            Console.WriteLine($"Posta Kutuları: {string.Join(", ", result.Data.Aliases)}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Hata: {result.Message}");
                }
            }
        }, taxIdArgument, jsonOption, testOption);

        // Status command
        var statusCommand = new Command("status", "Belge durumu sorgula");
        var uuidArgument = new Argument<string>("uuid", "Belge UUID");
        var typeOption = new Option<string>("--type", () => "invoice", "Belge tipi (invoice, dispatch, receipt)");
        var statusJsonOption = new Option<bool>("--json", "JSON formatında çıktı");
        var statusTestOption = new Option<bool>("--test", "Test ortamını kullan");

        statusCommand.AddArgument(uuidArgument);
        statusCommand.AddOption(typeOption);
        statusCommand.AddOption(statusJsonOption);
        statusCommand.AddOption(statusTestOption);

        statusCommand.SetHandler(async (uuid, type, json, useTest) =>
        {
            var client = CreateClient(useTest);

            if (type == "invoice")
            {
                var result = await client.GetInvoiceStatusAsync(new InvoiceStatusRequest { Uuid = uuid });

                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
                }
                else
                {
                    if (result.Success && result.Data != null)
                    {
                        Console.WriteLine($"UUID: {result.Data.Uuid}");
                        Console.WriteLine($"Fatura No: {result.Data.InvoiceNumber}");
                        Console.WriteLine($"Durum: {result.Data.Status}");
                        Console.WriteLine($"Açıklama: {result.Data.StatusDescription}");
                    }
                    else
                    {
                        Console.WriteLine($"Hata: {result.Message}");
                    }
                }
            }
            else if (type == "dispatch")
            {
                var result = await client.GetDispatchStatusAsync(uuid);

                if (json)
                {
                    Console.WriteLine(JsonSerializer.Serialize(result, JsonOptions));
                }
                else
                {
                    if (result.Success && result.Data != null)
                    {
                        Console.WriteLine($"UUID: {result.Data.Uuid}");
                        Console.WriteLine($"Durum: {result.Data.Status}");
                    }
                    else
                    {
                        Console.WriteLine($"Hata: {result.Message}");
                    }
                }
            }
        }, uuidArgument, typeOption, statusJsonOption, statusTestOption);

        // Validate command
        var validateCommand = new Command("validate", "Doğrulama işlemleri");

        var validateVknCommand = new Command("vkn", "VKN doğrula");
        var vknArgument = new Argument<string>("vkn", "Vergi kimlik numarası");
        validateVknCommand.AddArgument(vknArgument);
        validateVknCommand.SetHandler((vkn) =>
        {
            var result = TaxIdValidator.ValidateVkn(vkn);
            if (result.IsValid)
            {
                Console.WriteLine($"Geçerli VKN: {vkn}");
            }
            else
            {
                Console.WriteLine($"Geçersiz VKN: {result.ErrorMessage}");
            }
        }, vknArgument);

        var validateTcknCommand = new Command("tckn", "TCKN doğrula");
        var tcknArgument = new Argument<string>("tckn", "TC kimlik numarası");
        validateTcknCommand.AddArgument(tcknArgument);
        validateTcknCommand.SetHandler((tckn) =>
        {
            var result = TaxIdValidator.ValidateTckn(tckn);
            if (result.IsValid)
            {
                Console.WriteLine($"Geçerli TCKN: {tckn}");
            }
            else
            {
                Console.WriteLine($"Geçersiz TCKN: {result.ErrorMessage}");
            }
        }, tcknArgument);

        var validateIbanCommand = new Command("iban", "IBAN doğrula");
        var ibanArgument = new Argument<string>("iban", "IBAN numarası");
        validateIbanCommand.AddArgument(ibanArgument);
        validateIbanCommand.SetHandler((iban) =>
        {
            var result = IbanValidator.Validate(iban);
            if (result.IsValid)
            {
                Console.WriteLine($"Geçerli IBAN: {IbanValidator.Format(iban)}");
            }
            else
            {
                Console.WriteLine($"Geçersiz IBAN: {result.ErrorMessage}");
            }
        }, ibanArgument);

        validateCommand.AddCommand(validateVknCommand);
        validateCommand.AddCommand(validateTcknCommand);
        validateCommand.AddCommand(validateIbanCommand);

        // Config command
        var configCommand = new Command("config", "Yapılandırma işlemleri");

        var configShowCommand = new Command("show", "Mevcut yapılandırmayı göster");
        configShowCommand.SetHandler(() =>
        {
            var configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                var config = File.ReadAllText(configPath);
                Console.WriteLine(config);
            }
            else
            {
                Console.WriteLine("Yapılandırma dosyası bulunamadı.");
                Console.WriteLine($"Konum: {configPath}");
            }
        });

        var configSetCommand = new Command("set", "Yapılandırma değeri ayarla");
        var keyArgument = new Argument<string>("key", "Yapılandırma anahtarı");
        var valueArgument = new Argument<string>("value", "Değer");
        configSetCommand.AddArgument(keyArgument);
        configSetCommand.AddArgument(valueArgument);
        configSetCommand.SetHandler((key, value) =>
        {
            var config = LoadConfig();
            config[key] = value;
            SaveConfig(config);
            Console.WriteLine($"{key} = {(key.Contains("secret", StringComparison.OrdinalIgnoreCase) ? "***" : value)}");
        }, keyArgument, valueArgument);

        var configInitCommand = new Command("init", "Yapılandırmayı interaktif olarak oluştur");
        configInitCommand.SetHandler(() =>
        {
            Console.Write("API Key: ");
            var apiKey = Console.ReadLine() ?? "";

            Console.Write("API Secret: ");
            var apiSecret = Console.ReadLine() ?? "";

            Console.Write("Customer ID: ");
            var customerId = Console.ReadLine() ?? "";

            Console.Write("Software ID: ");
            var softwareId = Console.ReadLine() ?? "";

            Console.Write("Ortam [test/prod]: ");
            var env = Console.ReadLine() ?? "prod";

            var config = new Dictionary<string, string>
            {
                ["apiKey"] = apiKey,
                ["apiSecret"] = apiSecret,
                ["customerId"] = customerId,
                ["softwareId"] = softwareId,
                ["environment"] = env.ToLower() == "test" ? "test" : "production"
            };

            SaveConfig(config);
            Console.WriteLine($"\nYapılandırma kaydedildi: {GetConfigPath()}");
        });

        configCommand.AddCommand(configShowCommand);
        configCommand.AddCommand(configSetCommand);
        configCommand.AddCommand(configInitCommand);

        // Version command
        var versionCommand = new Command("version", "Sürüm bilgisi");
        versionCommand.SetHandler(() =>
        {
            Console.WriteLine("Entegre ETS CLI v1.0.0");
            Console.WriteLine("SDK: Entegre.Ets.Sdk v1.2.0");
            Console.WriteLine("Runtime: .NET " + Environment.Version);
        });

        // Add commands
        rootCommand.AddCommand(checkUserCommand);
        rootCommand.AddCommand(statusCommand);
        rootCommand.AddCommand(validateCommand);
        rootCommand.AddCommand(configCommand);
        rootCommand.AddCommand(versionCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static EtsClient CreateClient(bool useTest = false)
    {
        var config = LoadConfig();

        return new EtsClient(options =>
        {
            options.ApiKey = config.GetValueOrDefault("apiKey", "");
            options.ApiSecret = config.GetValueOrDefault("apiSecret", "");
            options.CustomerId = config.GetValueOrDefault("customerId", "");
            options.SoftwareId = config.GetValueOrDefault("softwareId", "");

            if (useTest || config.GetValueOrDefault("environment", "") == "test")
            {
                options.UseTestEnvironment();
            }
        });
    }

    private static string GetConfigPath()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".ets", "config.json");
    }

    private static Dictionary<string, string> LoadConfig()
    {
        var path = GetConfigPath();
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        return new();
    }

    private static void SaveConfig(Dictionary<string, string> config)
    {
        var path = GetConfigPath();
        var dir = Path.GetDirectoryName(path)!;

        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(path, json);
    }
}
