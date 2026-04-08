using Coworking.Application.Common.Interfaces.Transactions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Polly;
using System.Data.Common;

namespace Coworking.Application.Common.Behaviors
{
    public class TransactionRetryBehavior<TRequest, TResponse>(IDbConflictDetector dbConflictDetector)
        : IPipelineBehavior<TRequest, TResponse> where TRequest : IRequest<TResponse>
    {
        private const int MaxRetries = 3;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
        {
            var retryPolicy = Policy
                .Handle<DbUpdateException>(dbConflictDetector.IsTransient)
                    .Or<DbException>(dbConflictDetector.IsTransient)
                .WaitAndRetryAsync(
                    MaxRetries,
                    retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt)
                    /*,onRetry: (ex, time, retryCount, context) =>
                    {
                        // optional
                        // logger.LogWarning("Retry {RetryCount} for {RequestName} due to {Exception}", 
                        //     retryCount, typeof(TRequest).Name, ex.GetType().Name);
                    }*/);

            return await retryPolicy.ExecuteAsync(async (cancellationToken) => await next(cancellationToken), ct);
        }
    }
}
