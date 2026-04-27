using Coworking.Application.Ports.Squidex.Schemas.City;
using Coworking.Application.Ports.Squidex.Schemas.Email;
using Coworking.External.Squidex.Abstractions.Context;

namespace Coworking.Application.Ports.Squidex;

/// <summary>
/// Main Squidex app context. Available to Application via DI.
/// Exposes typed repositories as properties (analogous to EF DbSet&lt;T&gt; properties).
/// </summary>
public interface IMainSquidexContext : ISquidexContext
{
    ICityRepository Cities { get; }
    IEmailRepository Emails { get; }
}