using Coworking.Application.Behaviors;
using Coworking.Application.Features.Bookings.Commands.Create;
using Coworking.Application.Features.Bookings.Commands.Create.Responces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Coworking.IntegrationTests;

public class PipelineOrderTests
{
    private const string Database = "coworking_tests_pipeline";

    /// <summary>
    /// Registration order is nesting order, and the retry has to be innermost: any further out
    /// it would see exceptions already turned into BusinessRuleException by
    /// DomainExceptionBehavior, which IDbConflictDetector cannot recognise — retries would stop
    /// without a single failing test.
    /// </summary>
    [Fact]
    public void TransactionConflictRetryBehavior_IsRegisteredLast()
    {
        using var factory = new TestApiFactory(bypassCoordinator: false, Database);
        using var scope = factory.Services.CreateScope();

        var behaviors = scope.ServiceProvider
            .GetServices<IPipelineBehavior<CreateBookingCommand, CreateBookingCommandResponse>>()
            .ToList();

        Assert.NotEmpty(behaviors);
        Assert.IsType<TransactionConflictRetryBehavior<CreateBookingCommand, CreateBookingCommandResponse>>(
            behaviors[^1]);
    }
}
