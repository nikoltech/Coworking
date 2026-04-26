namespace Coworking.API.Infrastructure.Extensions.Security;

using Microsoft.AspNetCore.HttpOverrides;
using System.Net;

public static class NetworkExtensions
{
    /* appsetings.json:
    {
      "ProxySettings": {
        "TrustedProxies": [
          "127.0.0.1",
          "::1",
          "172.16.0.0/12"
        ]
      }
    }
     */
    /// <summary>
    /// WARNING: Ensure UseForwardedHeaders is configured. 
    /// Uses configuration 'ProxySettings:TrustedProxies'. Set or leave empty.
    /// </summary>
    /// <param name="services"></param>
    /// <param name="configuration"></param>
    /// <returns></returns>
    public static IServiceCollection AddProxySettings(this IServiceCollection services, IConfiguration configuration)
    {

        services.Configure<ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            var trustedProxies = configuration.GetSection("ProxySettings:TrustedProxies").Get<string[]>() ?? [];

            if (trustedProxies.Length > 0)
            {
                options.KnownProxies.Clear();
                options.KnownIPNetworks.Clear();
            }

            foreach (var proxy in trustedProxies)
            {
                if (IPAddress.TryParse(proxy, out var ip))
                {
                    options.KnownProxies.Add(ip);
                }
                else if (System.Net.IPNetwork.TryParse(proxy, out var network))
                {
                    options.KnownIPNetworks.Add(network);
                }
            }
        });

        // logging the configured proxies and networks for visibility
        services.PostConfigure<ForwardedHeadersOptions>(options =>
        {
            var loggerCategory = "ProxySecurity";

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConfiguration(configuration.GetSection("Logging"));
                builder.AddConsole();
            });

            var logger = loggerFactory.CreateLogger(loggerCategory);

            logger.LogInformation(
                "Proxy security: {NetCount} networks and {ProxyCount} IPs loaded.",
                options.KnownIPNetworks.Count, options.KnownProxies.Count);

            var isProxyHidden = logger.IsEnabled(LogLevel.Debug) is false;

            if (isProxyHidden)
            {
                logger.LogInformation("To see the full listing, set 'Logging__LogLevel__{Category}' to 'Debug'.", loggerCategory);
            }
            else
            {
                logger.LogDebug("--- Proxy Security Detail ---");

                foreach (var network in options.KnownIPNetworks)
                {
                    logger.LogDebug("Sensitive Proxy Network: {BaseAddress}/{PrefixLength}",
                        network.BaseAddress, network.PrefixLength);
                }

                foreach (var proxy in options.KnownProxies)
                {
                    logger.LogDebug("Sensitive Proxy IP: {Proxy}", proxy);
                }
            }
        });

        return services;
    }
}