using Coworking.API.Infrastructure.Helpers;
using System.Threading.RateLimiting;

namespace Coworking.API.Infrastructure.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// WARNING: Ensure proxy settings are configured to capture client IPs correctly.
    /// </summary>
    /// <param name="services"></param>
    /// <returns></returns>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: context.User.Identity?.Name
                                  ?? IpHelper.GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 10
                    })!);

            options.AddPolicy("booking-write", context =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.User.Identity?.Name
                                  ?? IpHelper.GetClientIp(context),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 6,
                        QueueLimit = 0
                    }));

            options.AddPolicy("read-heavy", context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: IpHelper.GetClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 300,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 50
                    }));
        });

        return services;
    }
}