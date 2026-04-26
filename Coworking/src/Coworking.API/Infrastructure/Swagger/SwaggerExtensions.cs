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

            // show enums as strings (very important for the frontend)
            c.UseInlineDefinitionsForEnums();

            c.EnableAnnotations();
        });

        return services;
    }
}
