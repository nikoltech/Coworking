using Coworking.Infrastructure.Services.Email;
using Coworking.Infrastructure.Services.Email.Senders;
using MailKit.Net.Smtp;
using MailKit.Security;
using System.Net.Sockets;

namespace Coworking.UnitTests.Behaviors;

/// <summary>
/// What the sender throws is what ConsumerPipelines retries on. These drifted apart once, and
/// no email failure was retried at all.
/// </summary>
public class EmailFailureTranslationTests
{
    private const string Recipient = "guest@example.com";

    /// 4xx is a temporary SMTP reply, 5xx is final.
    [Theory]
    [InlineData(SmtpStatusCode.MailboxBusy, typeof(EmailTransientException))]
    [InlineData(SmtpStatusCode.ServiceNotAvailable, typeof(EmailTransientException))]
    [InlineData(SmtpStatusCode.InsufficientStorage, typeof(EmailTransientException))]
    [InlineData(SmtpStatusCode.MailboxUnavailable, typeof(EmailPermanentException))]
    [InlineData(SmtpStatusCode.TransactionFailed, typeof(EmailPermanentException))]
    [InlineData(SmtpStatusCode.ExceededStorageAllocation, typeof(EmailPermanentException))]
    public void ServerReply_IsTransientOnlyFor4xx(SmtpStatusCode reply, Type expected)
    {
        var rejected = new SmtpCommandException(SmtpErrorCode.RecipientNotAccepted, reply, "rejected");

        Assert.IsType(expected, SmtpEmailSender.Translate(rejected, Recipient));
    }

    [Fact]
    public void ProtocolFailure_IsAConnectionFailure()
    {
        Assert.IsType<EmailConnectionException>(
            SmtpEmailSender.Translate(new SmtpProtocolException("broken"), Recipient));
    }

    [Fact]
    public void SocketFailure_IsAConnectionFailure()
    {
        Assert.IsType<EmailConnectionException>(
            SmtpEmailSender.Translate(new SocketException(10061), Recipient));
    }

    [Fact]
    public void Timeout_IsAConnectionFailure()
    {
        Assert.IsType<EmailConnectionException>(
            SmtpEmailSender.Translate(new TimeoutException(), Recipient));
    }

    [Fact]
    public void BadCredentials_ArePermanent()
    {
        Assert.IsType<EmailPermanentException>(
            SmtpEmailSender.Translate(new AuthenticationException("bad"), Recipient));
    }

    [Fact]
    public void UnknownFailure_IsPermanent()
    {
        Assert.IsType<EmailPermanentException>(
            SmtpEmailSender.Translate(new InvalidOperationException("boom"), Recipient));
    }

    [Fact]
    public void ConnectionFailure_IsAlsoTransient()
    {
        var connection = SmtpEmailSender.Translate(new SocketException(10061), Recipient);

        Assert.IsAssignableFrom<EmailTransientException>(connection);
    }
}
