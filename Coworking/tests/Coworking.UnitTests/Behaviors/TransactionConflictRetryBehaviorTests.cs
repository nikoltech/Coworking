using Coworking.Application.Abstractions;
using Coworking.Application.Abstractions.Transactions;
using Coworking.Application.Behaviors;
using MediatR;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Coworking.UnitTests.Behaviors;

public class TransactionConflictRetryBehaviorTests
{
    private readonly IDbConflictDetector _detector = Substitute.For<IDbConflictDetector>();
    private readonly IAppDbContext _dbContext = Substitute.For<IAppDbContext>();

    private record Probe : IRequest<string>;

    [Fact]
    public async Task TransientFailure_IsRetriedAndSucceeds()
    {
        _detector.IsTransient(Arg.Any<Exception>()).Returns(true);

        var attempts = 0;

        var result = await Behavior().Handle(new Probe(), _ =>
        {
            attempts++;

            return attempts == 1
                ? throw new InvalidOperationException("conflict")
                : Task.FromResult("done");
        }, CancellationToken.None);

        Assert.Equal("done", result);
        Assert.Equal(2, attempts);
        _dbContext.Received(1).DiscardPendingChanges();
    }

    [Fact]
    public async Task NonTransientFailure_IsNotRetried()
    {
        _detector.IsTransient(Arg.Any<Exception>()).Returns(false);

        var attempts = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Behavior().Handle(new Probe(), _ =>
            {
                attempts++;

                throw new InvalidOperationException("business rule");
            }, CancellationToken.None));

        Assert.Equal(1, attempts);
        _dbContext.DidNotReceive().DiscardPendingChanges();
    }

    private TransactionConflictRetryBehavior<Probe, string> Behavior() =>
        new(_detector, _dbContext,
            NullLogger<TransactionConflictRetryBehavior<Probe, string>>.Instance);
}
