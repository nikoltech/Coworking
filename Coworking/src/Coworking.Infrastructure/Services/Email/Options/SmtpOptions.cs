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
}
