using Coworking.Application.Abstractions.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using System.Data.Common;

namespace Coworking.Application.Behaviors
{
    // Note: metrics for future
    public class TransactionConflictRetryBehavior<TRequest, TResponse>(
        IDbConflictDetector dbConflictDetector,
        ILogger<TransactionConflictRetryBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private const int MaxRetries = 3;

        public async Task<TResponse> Handle(TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken ct)
        {
            var retryPolicy = Policy
                .Handle<DbUpdateException>(dbConflictDetector.IsTransient)
                    .Or<DbException>(dbConflictDetector.IsTransient)
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt)
                    , onRetry: (ex, time, retryCount, context) =>
                    {
                        // optional
                        logger.LogWarning("Retry {RetryCount} for {RequestName} due to {Exception}",
                            retryCount, typeof(TRequest).Name, ex.GetType().Name);
                    });

            return await retryPolicy.ExecuteAsync(async (cancellationToken) => await next(cancellationToken), ct);
        }
    }
}
