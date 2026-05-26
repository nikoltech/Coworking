using Coworking.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Coworking.Messaging.Consumers;

internal sealed class BrokerTestConsumerA(ILogger<BrokerTestConsumerA> logger)
    : IConsumer<BrokerTestMessage>
{
    public Task Consume(ConsumeContext<BrokerTestMessage> context)
    {
        logger.LogInformation("[Consumer A] Received: {Payload}", context.Message.Payload);
        return Task.CompletedTask;
    }
}
