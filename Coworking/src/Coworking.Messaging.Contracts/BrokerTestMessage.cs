using Coworking.Messaging.Contracts.Abstracts;

namespace Coworking.Messaging.Contracts;

public sealed record BrokerTestMessage(string Payload) : IntegrationEvent
{
    public override string EventType => "broker.test";
}
