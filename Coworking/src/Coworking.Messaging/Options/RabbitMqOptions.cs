using System.ComponentModel.DataAnnotations;

namespace Coworking.Messaging.Options;

public sealed class RabbitMqOptions
{
    public const string SectionName = "RabbitMq";

    [Required]
    public string Host { get; init; } = default!;

    public int Port { get; init; } = 5672;

    [Required]
    public string Username { get; init; } = default!;

    [Required]
    public string Password { get; init; } = default!;

    public string VirtualHost { get; init; } = "/";
}
