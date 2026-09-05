using MassTransit.Logging;
using System.Diagnostics;

namespace Coworking.API.Infrastructure.Extensions;

public static class TraceContextExtensions
{
    /// <summary>
    /// Propagates trace ids without collecting telemetry — nothing propagates unless something listens.
    /// </summary>
    public static IServiceCollection AddTraceContext(this IServiceCollection services)
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == DiagnosticHeaders.DefaultListenerName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) =>
                ActivitySamplingResult.PropagationData,
            SampleUsingParentId = (ref ActivityCreationOptions<string> _) =>
                ActivitySamplingResult.PropagationData
        };

        ActivitySource.AddActivityListener(listener);

        // singleton only so the container disposes it
        return services.AddSingleton(listener);
    }
}
