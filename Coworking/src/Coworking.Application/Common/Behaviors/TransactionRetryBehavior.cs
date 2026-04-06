using Coworking.Application.Common.Interfaces.Transactions;
using MediatR;
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
                //.Handle<DbUpdateException>(dbConflictDetector.IsTransient)
                .Handle<DbException>(dbConflictDetector.IsTransient)
                .WaitAndRetryAsync(MaxRetries, retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt));

            return await retryPolicy.ExecuteAsync(async (cancellationToken) => await next(cancellationToken), ct);
        }
    }
}
