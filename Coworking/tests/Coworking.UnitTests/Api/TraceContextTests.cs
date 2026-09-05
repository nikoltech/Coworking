using Coworking.API;
using MassTransit.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace Coworking.UnitTests.Api;

public class TraceContextTests
{
    /// <summary>
    /// MassTransit only writes trace context into the outbox while an activity exists, and an
    /// activity exists only while something listens. Dropping the listener breaks the link between
    /// a request and its consumer without breaking anything visible.
    /// </summary>
    [Fact]
    public void ConfigureApi_ListensToMassTransitAndNothingElse()
    {
        using var massTransit = new ActivitySource(DiagnosticHeaders.DefaultListenerName);
        using var unrelated = new ActivitySource("Coworking.Tests.Unrelated");

        var provider = new ServiceCollection()
            .ConfigureApi(new ConfigurationBuilder().Build())
            .BuildServiceProvider();

        try
        {
            Assert.NotNull(massTransit.StartActivity("probe"));
            Assert.Null(unrelated.StartActivity("probe"));
        }
        finally
        {
            // the listener stays process-wide until the container releases it
            provider.Dispose();
        }
    }
}
