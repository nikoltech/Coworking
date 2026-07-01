using Coworking.Application.Ports.Squidex.Schemas.Email;
using Coworking.External.Squidex.Abstractions.Filters;
using Coworking.External.Squidex.Abstractions.Models;
using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Repository;
using Coworking.External.Squidex.Context;

namespace Coworking.Infrastructure.External.Squidex.Schemas.Email;

public sealed class EmailRepository(
    ISquidexApiClient client,
    ISquidexPaginator paginator)
    : SquidexSet<EmailSchema>(client, paginator, EmailSchema.SchemaName), IEmailRepository
{
    public async Task<string?> GetEmailsByNameAsync(string name, CancellationToken ct = default)
    {
        var result = await QueryAsync(
                RequestQuery.Create()
                    .WithTake(1)
                    .WithFilter(SquidexFilter.Eq(EmailPaths.Name, name)),
                ct: ct);

        return result.Items.FirstOrDefault()?.Data.Value?.Value;
    }
}