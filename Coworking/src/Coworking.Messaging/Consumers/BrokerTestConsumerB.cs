using Coworking.Messaging.Contracts;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Coworking.Messaging.Consumers;

internal sealed class BrokerTestConsumerB(ILogger<BrokerTestConsumerB> logger)
    : IConsumer<BrokerTestMessage>
{
    public Task Consume(ConsumeContext<BrokerTestMessage> context)
    {
        logger.LogInformation("[Consumer B] Received: {Payload}", context.Message.Payload);
        return Task.CompletedTask;
    }
}
