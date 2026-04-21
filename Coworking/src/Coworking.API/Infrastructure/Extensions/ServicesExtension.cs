using Coworking.API.Mappings;

namespace Coworking.API.Infrastructure.Extensions
{
    internal static class ServicesExtension
    {
        internal static void AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAutoMapper(_ => { }, typeof(MappingProfile).Assembly);

        }
    }
}
