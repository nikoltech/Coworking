using Coworking.External.Squidex.Abstractions.Set;

namespace Coworking.Application.Ports.Squidex.Schemas.Email;

public interface IEmailRepository : ISquidexSet<EmailSchema>
{
    Task<string?> GetEmailsByNameAsync(string name, CancellationToken ct = default);
}