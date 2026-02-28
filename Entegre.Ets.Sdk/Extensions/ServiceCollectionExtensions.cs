using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Entegre.Ets.Sdk.Webhooks;
using Entegre.Ets.Sdk.Logging;
using Entegre.Ets.Sdk.Caching;

namespace Entegre.Ets.Sdk.Extensions;

/// <summary>
/// Extension methods for IServiceCollection
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the ETS client to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsClient(
        this IServiceCollection services,
        Action<EtsClientOptions> configure)
    {
        var options = new EtsClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<IEtsClient, EtsClient>();

        return services;
    }

    /// <summary>
    /// Adds the ETS client to the service collection with options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="options">Client options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsClient(
        this IServiceCollection services,
        EtsClientOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IEtsClient, EtsClient>();

        return services;
    }

    /// <summary>
    /// Adds the ETS client as scoped service
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsClientScoped(
        this IServiceCollection services,
        Action<EtsClientOptions> configure)
    {
        var options = new EtsClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddScoped<IEtsClient, EtsClient>();

        return services;
    }

    /// <summary>
    /// Adds webhook handler to the service collection
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsWebhook(
        this IServiceCollection services,
        Action<WebhookOptions> configure)
    {
        var options = new WebhookOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<IWebhookHandler, WebhookHandler>();
        services.AddSingleton<WebhookRouter>();

        return services;
    }

    /// <summary>
    /// Adds webhook handler with options
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="options">Webhook options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsWebhook(
        this IServiceCollection services,
        WebhookOptions options)
    {
        services.AddSingleton(options);
        services.AddSingleton<IWebhookHandler, WebhookHandler>();
        services.AddSingleton<WebhookRouter>();

        return services;
    }

    /// <summary>
    /// Adds ETS client with logging support
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsClientWithLogging(
        this IServiceCollection services,
        Action<EtsClientOptions> configure)
    {
        var options = new EtsClientOptions();
        configure(options);

        services.AddSingleton(options);
        services.AddSingleton<EtsClient>();
        services.AddSingleton<IEtsClient>(sp =>
        {
            var inner = sp.GetRequiredService<EtsClient>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LoggingEtsClient>>();
            return new LoggingEtsClient(inner, logger);
        });

        return services;
    }

    /// <summary>
    /// Adds ETS client with caching support
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <param name="cacheOptions">Cache options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsClientWithCaching(
        this IServiceCollection services,
        Action<EtsClientOptions> configure,
        Action<EtsCacheOptions>? cacheOptions = null)
    {
        var options = new EtsClientOptions();
        configure(options);

        var cache = new EtsCacheOptions();
        cacheOptions?.Invoke(cache);

        services.AddSingleton(options);
        services.AddSingleton(cache);
        services.AddSingleton<EtsClient>();
        services.AddSingleton<IEtsClient>(sp =>
        {
            var inner = sp.GetRequiredService<EtsClient>();
            var memoryCache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var cacheOpts = sp.GetRequiredService<EtsCacheOptions>();
            return new CachingEtsClient(inner, memoryCache, cacheOpts);
        });

        return services;
    }

    /// <summary>
    /// Adds ETS client with all features (logging + caching)
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configure">Configuration action</param>
    /// <param name="cacheOptions">Cache options</param>
    /// <returns>Service collection</returns>
    public static IServiceCollection AddEtsClientFull(
        this IServiceCollection services,
        Action<EtsClientOptions> configure,
        Action<EtsCacheOptions>? cacheOptions = null)
    {
        var options = new EtsClientOptions();
        configure(options);

        var cache = new EtsCacheOptions();
        cacheOptions?.Invoke(cache);

        services.AddSingleton(options);
        services.AddSingleton(cache);
        services.AddSingleton<EtsClient>();
        services.AddSingleton<IEtsClient>(sp =>
        {
            var inner = sp.GetRequiredService<EtsClient>();
            var memoryCache = sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>();
            var cacheOpts = sp.GetRequiredService<EtsCacheOptions>();
            var logger = sp.GetRequiredService<Microsoft.Extensions.Logging.ILogger<LoggingEtsClient>>();

            // Wrap: EtsClient -> CachingEtsClient -> LoggingEtsClient
            var cached = new CachingEtsClient(inner, memoryCache, cacheOpts);
            return new LoggingEtsClient(cached, logger);
        });

        return services;
    }
}
