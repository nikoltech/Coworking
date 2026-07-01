using Coworking.Application.Ports.Squidex;
using Coworking.Application.Ports.Squidex.Schemas.City;
using Coworking.Application.Ports.Squidex.Schemas.Email;
using Coworking.External.Squidex.Abstractions.Client;
using Coworking.External.Squidex.Abstractions.Pagination;
using Coworking.External.Squidex.Client;
using Coworking.External.Squidex.Context;
using Coworking.Infrastructure.External.Squidex.Schemas.City;
using Coworking.Infrastructure.External.Squidex.Schemas.Email;

namespace Coworking.Infrastructure.External.Squidex.Contexts;

/// <summary>
/// Squidex context for app access via the Set<> method. 
/// Provides typed repositories as properties.
/// </summary>
public sealed class MainSquidexContext(
    ISquidexApiClient client,
    ISquidexPaginator paginator,
    SquidexClientFactory clientFactory)
    : SquidexContext(client, paginator, clientFactory, AppName),
      IMainSquidexContext
{
    public const string AppName = "Main";

    public ICityRepository Cities { get; } = new CityRepository(client, paginator);
    public IEmailRepository Emails { get; } = new EmailRepository(client, paginator);
}