using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Infrastructure.Services.Email.Options;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = default!;
    public int Port { get; init; }

    public string Username { get; init; } = default!;
    public string Password { get; init; } = default!;

    public string FromEmail { get; init; } = default!;
    public string FromName { get; init; } = default!;

    public bool UseSsl { get; init; } = true;

    /// <summary>
    /// Max parallel email send workers.
    /// Defaults to ProcessorCount * 2 (IO-bound optimized).
    /// </summary>
    public int MaxConcurrentConnections { get; init; } = Environment.ProcessorCount * 2;

    /// <summary>
    /// Maximum number of emails buffered in the channel before backpressure kicks in.
    /// Increase if bursts of outgoing emails are expected.
    /// </summary>
    public int ChannelCapacity { get; init; } = 1000;
}
