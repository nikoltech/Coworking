namespace Coworking.API.Infrastructure.Extensions
{
    internal static class CorsExtensions
    {
        internal const string DefaultCorsPolicyName = "CW.Cors.Api";

        internal static IServiceCollection AddCors(this IServiceCollection services, IConfiguration configuration)
        {
            var allowedOrigins = configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [];

            services.AddCors(options =>
            {
                options.AddPolicy(DefaultCorsPolicyName, policy =>
                {
                    //policy.AllowAnyOrigin()
                    //    .AllowAnyHeader()
                    //    .AllowAnyMethod();

                    policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials();
                });
            });

            return services;
        }
    }
}
