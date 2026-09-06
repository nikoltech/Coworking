using Coworking.API.Infrastructure.Helpers;
using Coworking.API.Infrastructure.RateLimiting;
using System.Threading.RateLimiting;

namespace Coworking.API.Infrastructure.Extensions;

public static class RateLimitingExtensions
{
    /// <summary>
    /// Partitions on the client IP — requires the proxy settings to be configured.
    /// </summary>
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

            options.AddPolicy(RateLimitPolicies.BookingWrite, context =>
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

            options.AddPolicy(RateLimitPolicies.ReadHeavy, context =>
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