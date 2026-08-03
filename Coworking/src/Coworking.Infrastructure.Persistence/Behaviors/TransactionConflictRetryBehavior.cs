using Coworking.Application.Abstractions.Transactions;
using Coworking.Infrastructure.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System.Data.Common;

namespace Coworking.Infrastructure.Persistence.Behaviors;

/// <summary>
/// Retries a request when the database reports a transient conflict (serialization
/// failure, deadlock). 
/// Lives next to EF because recovering from one means resetting
/// the DbContext, which the Application layer cannot reach.
/// </summary>
public class TransactionConflictRetryBehavior<TRequest, TResponse>(
    IDbConflictDetector dbConflictDetector,
    AppDbContext dbContext,
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
                    dbContext.ChangeTracker.Clear();

                    logger.LogWarning("Retry {RetryCount} for {RequestName} due to {Exception}",
                        retryCount, typeof(TRequest).Name, ex.GetType().Name);
                });

        return await retryPolicy.ExecuteAsync(async (cancellationToken) => await next(cancellationToken), ct);
    }
}
