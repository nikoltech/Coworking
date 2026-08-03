namespace Coworking.API.Infrastructure.Helpers;

public static class IpHelper
{
    public static string GetClientIp(HttpContext context)
    {
        var cfIp = context.Request.Headers["CF-Connecting-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(cfIp)) return cfIp;

        // first entry is the client; proxies append to the tail
        var forwardedIp = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrEmpty(forwardedIp))
        {
            return forwardedIp.Split(',')[0].Trim();
        }

        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
