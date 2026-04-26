namespace Coworking.API.Infrastructure.Helpers;

public static class IpHelper
{
    public static string GetClientIp(HttpContext context)
    {
        // Cloudflare
        var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIp)) return cfIp;

        // X-Forwarded-For (for Nginx/Docker)
        // IMPORTANT: take the first address in the list, as proxies append addresses to the end
        var forwardedIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedIp))
        {
            return forwardedIp.Split(',')[0].Trim();
        }

        // Default: Direct connection
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
