using Coworking.Infrastructure.Services.Email.Messaging.Dtos;
using Coworking.Infrastructure.Services.Email.Messaging.Interfaces;
using System.Threading.Channels;

namespace Coworking.Infrastructure.Services.Email.Messaging;

public sealed class EmailChannel : IEmailChannel
{
    private readonly Channel<EmailMessageChannelDto> _channel =
        Channel.CreateBounded<EmailMessageChannelDto>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<EmailMessageChannelDto> Reader => _channel.Reader;
    public ChannelWriter<EmailMessageChannelDto> Writer => _channel.Writer;

    public ValueTask WriteAsync(EmailMessageChannelDto message, CancellationToken ct) =>
        _channel.Writer.WriteAsync(message, ct);
}
