using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace Web.Api.Extensions;

internal static class ForwardedHeadersExtensions
{
    internal static IServiceCollection AddForwardedHeadersInternal(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ForwardedHeadersSettings settings =
            configuration.GetSection(ForwardedHeadersSettings.SectionName).Get<ForwardedHeadersSettings>()
            ?? new ForwardedHeadersSettings();

        if (!settings.Enabled)
        {
            return services;
        }

        if (settings.KnownProxies.Length == 0 && settings.KnownNetworks.Length == 0)
        {
            throw new InvalidOperationException(
                "ForwardedHeaders:Enabled is true but neither ForwardedHeaders:KnownProxies nor " +
                "ForwardedHeaders:KnownNetworks is configured. Trusting forwarded headers from unknown " +
                "peers would let any client spoof its address.");
        }

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
            options.ForwardLimit = settings.ForwardLimit;

            // Replace the loopback defaults: only explicitly configured peers are trusted.
            options.KnownProxies.Clear();
            options.KnownIPNetworks.Clear();

            foreach (string proxy in settings.KnownProxies)
            {
                options.KnownProxies.Add(IPAddress.Parse(proxy));
            }

            foreach (string network in settings.KnownNetworks)
            {
                options.KnownIPNetworks.Add(System.Net.IPNetwork.Parse(network));
            }
        });

        return services;
    }
}
