using Microsoft.Extensions.DependencyInjection;
using Entegre.Ets.Sdk.Webhooks;

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
}
