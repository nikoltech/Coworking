using MediatR;
using Microsoft.Extensions.Logging;

namespace Coworking.Application.Behaviors;

public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        try
        {
            return await next(ct);
        }
        catch (Exception ex)
        {
            // TODO: set logger as Async globally in Presentation layer
            logger.LogError(ex, "Unhandled exception for {RequestName}: {Message}", typeof(TRequest).Name, ex.Message);

            throw;
        }
    }
}
