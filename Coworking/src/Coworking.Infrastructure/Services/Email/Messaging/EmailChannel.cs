using Coworking.Application.Abstractions.Messaging;
using Coworking.Application.Notifications.Email;
using System.Threading.Channels;

namespace Coworking.Infrastructure.Services.Email.Messaging;

public sealed class EmailChannel : IEmailChannel
{
    private readonly Channel<EmailNotification> _channel =
        Channel.CreateBounded<EmailNotification>(new BoundedChannelOptions(1000)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<EmailNotification> Reader => _channel.Reader;
    public ChannelWriter<EmailNotification> Writer => _channel.Writer;

    public ValueTask WriteAsync(EmailNotification notification, CancellationToken ct) =>
        _channel.Writer.WriteAsync(notification, ct);
}
