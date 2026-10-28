using System.Net;
using Microsoft.AspNetCore.HttpOverrides;

namespace EnglishLiveTutor.Api.Configuration;

public static class ConfiguredForwardedHeadersExtensions
{
    public static ForwardedHeadersOptions? CreateForwardedHeadersOptions(
        IConfiguration configuration,
        ILogger? logger = null)
    {
        var proxiesConfig = configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>()
            ?? Array.Empty<string>();
        var networksConfig = configuration.GetSection("ForwardedHeaders:KnownNetworks").Get<string[]>()
            ?? Array.Empty<string>();

        var validProxies = new List<IPAddress>();
        foreach (var proxy in proxiesConfig)
        {
            if (string.IsNullOrWhiteSpace(proxy)) continue;
            string trimmed = proxy.Trim();
            if (IPAddress.TryParse(trimmed, out var ip))
            {
                validProxies.Add(ip);
            }
            else
            {
                logger?.LogWarning("Cấu hình IP proxy không hợp lệ trong ForwardedHeaders:KnownProxies: {Proxy}", trimmed);
            }
        }

        var validNetworks = new List<System.Net.IPNetwork>();
        foreach (var network in networksConfig)
        {
            if (string.IsNullOrWhiteSpace(network)) continue;
            string trimmed = network.Trim();
            if (System.Net.IPNetwork.TryParse(trimmed, out var net))
            {
                validNetworks.Add(net);
            }
            else
            {
                logger?.LogWarning("Cấu hình CIDR network không hợp lệ trong ForwardedHeaders:KnownNetworks: {Network}", trimmed);
            }
        }

        // Chỉ cấu hình khi có ít nhất một proxy/network hợp lệ được cấu hình
        if (validProxies.Count == 0 && validNetworks.Count == 0)
        {
            return null;
        }

        var options = new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
        };

        // Không gọi Clear() để giữ loopback mặc định (127.0.0.1, ::1)
        foreach (var ip in validProxies)
        {
            options.KnownProxies.Add(ip);
        }

        foreach (var net in validNetworks)
        {
            options.KnownIPNetworks.Add(net);
        }

        return options;
    }

    public static IApplicationBuilder UseConfiguredForwardedHeaders(
        this IApplicationBuilder app,
        IConfiguration configuration,
        ILogger? logger = null)
    {
        var options = CreateForwardedHeadersOptions(configuration, logger);
        if (options is not null)
        {
            app.UseForwardedHeaders(options);
        }

        return app;
    }
}
