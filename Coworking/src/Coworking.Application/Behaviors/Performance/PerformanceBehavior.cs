using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Coworking.Application.Behaviors.Performance;

public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    IOptions<PerformanceSettings> options)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var settings = options.Value;

        if (settings.Enabled is false)
            return await next(ct);

        long start = Stopwatch.GetTimestamp();

        try
        {
            return await next(ct);
        }
        finally
        {
            var elapsed = Stopwatch.GetElapsedTime(start);

            if (elapsed.TotalMilliseconds > settings.ThresholdMs)
            {
                logger.LogWarning("Long Running Request: {Name} ({Elapsed}ms). Threshold: {Threshold}ms",
                    typeof(TRequest).Name, (long)elapsed.TotalMilliseconds, settings.ThresholdMs);
            }
        }
    }
}
