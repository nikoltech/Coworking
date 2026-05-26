using Coworking.API.Controllers.Abstractions;
using Coworking.Messaging.Contracts;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace Coworking.API.Controllers;

[Route("api/broker-test")]
[Tags("Dev")]
public sealed class BrokerTestController(IPublishEndpoint publishEndpoint) : ApiControllerBase
{
    private const int MaxPayloadLength = 500;

    /// <summary>
    /// Publishes a test message to the broker. Both consumers will receive it.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    public async Task<IActionResult> Publish([FromBody] BrokerTestRequest request, CancellationToken ct)
    {
        var payload = Sanitize(request.Message);

        await publishEndpoint.Publish(new BrokerTestMessage(payload), ct);

        return Accepted();
    }

    private static string Sanitize(string input)
    {
        var trimmed = input.Trim();

        if (trimmed.Length > MaxPayloadLength)
            trimmed = trimmed[..MaxPayloadLength];

        // Remove control characters (except whitespace)
        return Regex.Replace(trimmed, @"\p{C}", string.Empty);
    }
}

public sealed record BrokerTestRequest(string Message);
