using Coworking.External.Squidex.Abstractions.Repository;

namespace Coworking.Application.Ports.Squidex.Schemas.Email;

public interface IEmailRepository : ISquidexRepository<EmailSchema>
{
    Task<string?> GetEmailsByNameAsync(string name, CancellationToken ct = default);
}