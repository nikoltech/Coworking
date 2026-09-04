using Coworking.Application.Common.Exceptions;
using Coworking.Application.Ports;
using Coworking.Application.Ports.Transactions;
using MediatR;
using Microsoft.Extensions.Logging;
using Polly;

namespace Coworking.Application.Behaviors;

public class TransactionConflictRetryBehavior<TRequest, TResponse>(
    IDbConflictDetector dbConflictDetector,
    IAppDbContext dbContext,
    ILogger<TransactionConflictRetryBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
{
    private const int MaxRetries = 3;

    public async Task<TResponse> Handle(TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken ct)
    {
        var retryPolicy = Policy
            .Handle<Exception>(dbConflictDetector.IsTransient)
            .WaitAndRetryAsync(
                MaxRetries,
                retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt)
                , onRetry: (ex, time, retryCount, context) =>
                {
                    // the rolled-back attempt left entities tracked as if they were saved
                    dbContext.DiscardPendingChanges();

                    logger.LogWarning("Retry {RetryCount} for {RequestName} due to {Exception}",
                        retryCount, typeof(TRequest).Name, ex.GetType().Name);
                });

        try
        {
            return await retryPolicy.ExecuteAsync(async (cancellationToken) => await next(cancellationToken), ct);
        }
        catch (Exception ex) when (dbConflictDetector.IsTransient(ex))
        {
            // still transient after leaving the policy means the attempts ran out
            throw new TransactionConflictException(
                $"{typeof(TRequest).Name} could not complete after {MaxRetries} retries.", ex);
        }
    }
}
