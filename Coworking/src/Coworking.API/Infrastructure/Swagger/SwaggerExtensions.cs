namespace Coworking.API.Infrastructure.Swagger;

internal static class SwaggerExtensions
{
    internal static IServiceCollection AddSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "Coworking API",
                Version = "v1",
                Description = "Booking system API for coworking spaces"
            });

            // enums as strings — clients bind to names, not ordinals
            c.UseInlineDefinitionsForEnums();

            c.EnableAnnotations();
        });

        return services;
    }
}
