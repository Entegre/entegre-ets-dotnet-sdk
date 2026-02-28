using System.Xml.Linq;
using System.Collections.Concurrent;

namespace Entegre.Ets.Sdk.ExchangeRate;

/// <summary>
/// Single currency rate from TCMB
/// </summary>
public class TcmbRate
{
    /// <summary>
    /// Currency code (USD, EUR, etc.)
    /// </summary>
    public string CurrencyCode { get; set; } = string.Empty;

    /// <summary>
    /// Currency name (Turkish)
    /// </summary>
    public string CurrencyName { get; set; } = string.Empty;

    /// <summary>
    /// Unit (1, 100, etc.)
    /// </summary>
    public int Unit { get; set; } = 1;

    /// <summary>
    /// Forex buying rate
    /// </summary>
    public decimal ForexBuying { get; set; }

    /// <summary>
    /// Forex selling rate
    /// </summary>
    public decimal ForexSelling { get; set; }

    /// <summary>
    /// Banknote buying rate
    /// </summary>
    public decimal BanknoteBuying { get; set; }

    /// <summary>
    /// Banknote selling rate
    /// </summary>
    public decimal BanknoteSelling { get; set; }

    /// <summary>
    /// Cross rate
    /// </summary>
    public decimal? CrossRate { get; set; }
}

/// <summary>
/// TCMB rates result
/// </summary>
public class TcmbRatesResult
{
    /// <summary>
    /// Rate date
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// Source (TCMB XML file name)
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// All rates (currency code -> rate)
    /// </summary>
    public Dictionary<string, TcmbRate> Rates { get; set; } = new();
}

/// <summary>
/// TCMB service configuration
/// </summary>
public class TcmbServiceOptions
{
    /// <summary>
    /// Cache duration (default: 1 hour)
    /// </summary>
    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// HTTP timeout (default: 10 seconds)
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}

/// <summary>
/// TCMB (Central Bank of Turkey) Exchange Rate Service
/// </summary>
public class TcmbService
{
    private const string TcmbBaseUrl = "https://www.tcmb.gov.tr/kurlar";
    private readonly HttpClient _httpClient;
    private readonly TcmbServiceOptions _options;
    private readonly ConcurrentDictionary<string, (TcmbRatesResult Result, DateTime CachedAt)> _cache = new();

    /// <summary>
    /// Creates a new TCMB service
    /// </summary>
    public TcmbService(TcmbServiceOptions? options = null)
    {
        _options = options ?? new TcmbServiceOptions();
        _httpClient = new HttpClient
        {
            Timeout = _options.Timeout
        };
    }

    /// <summary>
    /// Creates a new TCMB service with custom HttpClient
    /// </summary>
    public TcmbService(HttpClient httpClient, TcmbServiceOptions? options = null)
    {
        _httpClient = httpClient;
        _options = options ?? new TcmbServiceOptions();
    }

    /// <summary>
    /// Gets today's exchange rates
    /// </summary>
    public async Task<TcmbRatesResult> GetTodayRatesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{TcmbBaseUrl}/today.xml";
        return await FetchAndParseRatesAsync(url, "today", cancellationToken);
    }

    /// <summary>
    /// Gets exchange rates for a specific date
    /// </summary>
    public async Task<TcmbRatesResult> GetRatesForDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        // TCMB format: YYYYMM/DDMMYYYY.xml
        var folder = date.ToString("yyyyMM");
        var filename = date.ToString("ddMMyyyy");
        var url = $"{TcmbBaseUrl}/{folder}/{filename}.xml";

        return await FetchAndParseRatesAsync(url, filename, cancellationToken);
    }

    /// <summary>
    /// Gets a single currency rate
    /// </summary>
    public async Task<TcmbRate?> GetRateAsync(string currencyCode, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var result = date.HasValue
            ? await GetRatesForDateAsync(date.Value, cancellationToken)
            : await GetTodayRatesAsync(cancellationToken);

        result.Rates.TryGetValue(currencyCode.ToUpperInvariant(), out var rate);
        return rate;
    }

    /// <summary>
    /// Gets invoice rate for a currency (ForexSelling / Unit)
    /// </summary>
    /// <param name="currencyCode">Currency code (USD, EUR, etc.)</param>
    /// <param name="date">Rate date (optional, default: today)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Invoice rate (1 unit of currency = X TRY)</returns>
    /// <exception cref="InvalidOperationException">When currency is not found</exception>
    public async Task<decimal> GetInvoiceRateAsync(string currencyCode, DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var code = currencyCode.ToUpperInvariant();

        // TRY always returns 1
        if (code == "TRY")
            return 1m;

        var rate = await GetRateAsync(code, date, cancellationToken);

        if (rate == null)
        {
            var dateStr = date?.ToString("yyyy-MM-dd") ?? "today";
            throw new InvalidOperationException($"Currency {code} not found in TCMB rates ({dateStr})");
        }

        // ForexSelling / Unit = 1 unit of currency in TRY
        return rate.ForexSelling / rate.Unit;
    }

    /// <summary>
    /// Gets available currency codes
    /// </summary>
    public async Task<IReadOnlyList<string>> GetAvailableCurrenciesAsync(DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var result = date.HasValue
            ? await GetRatesForDateAsync(date.Value, cancellationToken)
            : await GetTodayRatesAsync(cancellationToken);

        return result.Rates.Keys.ToList();
    }

    /// <summary>
    /// Clears the cache
    /// </summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    private async Task<TcmbRatesResult> FetchAndParseRatesAsync(string url, string cacheKey, CancellationToken cancellationToken)
    {
        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow - cached.CachedAt < _options.CacheDuration)
            {
                return cached.Result;
            }
        }

        try
        {
            var xml = await _httpClient.GetStringAsync(url, cancellationToken);
            var result = ParseXml(xml, url);

            // Update cache
            _cache[cacheKey] = (result, DateTime.UtcNow);

            return result;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new InvalidOperationException($"TCMB rate data not found: {url} (404). Date may be a weekend or holiday.", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException($"Failed to fetch TCMB rates: {ex.Message}", ex);
        }
    }

    private static TcmbRatesResult ParseXml(string xml, string source)
    {
        var doc = XDocument.Parse(xml);
        var root = doc.Root;

        if (root == null || root.Name.LocalName != "Tarih_Date")
        {
            throw new InvalidOperationException("Invalid TCMB XML format: Tarih_Date not found");
        }

        // Parse date
        var dateStr = root.Attribute("Date")?.Value ?? root.Attribute("Tarih")?.Value;
        DateTime date;
        if (!string.IsNullOrEmpty(dateStr))
        {
            // Try different formats: "01/15/2024" or "15.01.2024"
            if (dateStr.Contains('/'))
            {
                var parts = dateStr.Split('/');
                date = new DateTime(int.Parse(parts[2]), int.Parse(parts[0]), int.Parse(parts[1]));
            }
            else if (dateStr.Contains('.'))
            {
                var parts = dateStr.Split('.');
                date = new DateTime(int.Parse(parts[2]), int.Parse(parts[1]), int.Parse(parts[0]));
            }
            else
            {
                date = DateTime.Parse(dateStr);
            }
        }
        else
        {
            date = DateTime.Today;
        }

        var rates = new Dictionary<string, TcmbRate>();

        foreach (var currency in root.Elements("Currency"))
        {
            var code = currency.Attribute("CurrencyCode")?.Value ?? currency.Attribute("Kod")?.Value;
            if (string.IsNullOrEmpty(code))
                continue;

            var rate = new TcmbRate
            {
                CurrencyCode = code,
                CurrencyName = currency.Element("Isim")?.Value ?? currency.Element("CurrencyName")?.Value ?? "",
                Unit = ParseInt(currency.Element("Unit")?.Value),
                ForexBuying = ParseDecimal(currency.Element("ForexBuying")?.Value),
                ForexSelling = ParseDecimal(currency.Element("ForexSelling")?.Value),
                BanknoteBuying = ParseDecimal(currency.Element("BanknoteBuying")?.Value),
                BanknoteSelling = ParseDecimal(currency.Element("BanknoteSelling")?.Value),
            };

            var crossRate = currency.Element("CrossRateUSD")?.Value ?? currency.Element("CrossRateOther")?.Value;
            if (!string.IsNullOrEmpty(crossRate))
            {
                rate.CrossRate = ParseDecimal(crossRate);
            }

            rates[code] = rate;
        }

        return new TcmbRatesResult
        {
            Date = date,
            Source = source,
            Rates = rates
        };
    }

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 1;

        return int.TryParse(value.Trim(), out var result) ? result : 1;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        // Handle Turkish decimal format (comma as decimal separator)
        var normalized = value.Trim().Replace(',', '.');

        return decimal.TryParse(normalized, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : 0;
    }
}

/// <summary>
/// Static TCMB service instance for convenience
/// </summary>
public static class Tcmb
{
    private static readonly Lazy<TcmbService> _instance = new(() => new TcmbService());

    /// <summary>
    /// Default TCMB service instance
    /// </summary>
    public static TcmbService Instance => _instance.Value;

    /// <summary>
    /// Gets invoice rate for a currency
    /// </summary>
    public static Task<decimal> GetInvoiceRateAsync(string currencyCode, DateTime? date = null, CancellationToken cancellationToken = default)
        => Instance.GetInvoiceRateAsync(currencyCode, date, cancellationToken);

    /// <summary>
    /// Gets today's rates
    /// </summary>
    public static Task<TcmbRatesResult> GetTodayRatesAsync(CancellationToken cancellationToken = default)
        => Instance.GetTodayRatesAsync(cancellationToken);

    /// <summary>
    /// Gets rates for a specific date
    /// </summary>
    public static Task<TcmbRatesResult> GetRatesForDateAsync(DateTime date, CancellationToken cancellationToken = default)
        => Instance.GetRatesForDateAsync(date, cancellationToken);
}
