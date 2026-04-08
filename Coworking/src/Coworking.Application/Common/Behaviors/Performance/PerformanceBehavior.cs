using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Coworking.Application.Common.Behaviors.Performance;

public class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger,
    PerformanceSettings settings)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
    {
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
