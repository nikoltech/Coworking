using Coworking.API.ExceptionHandlers;
using Coworking.API.Infrastructure.Extensions;
using Coworking.Application.Common.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;

namespace Coworking.API;

public static class DependencyInjection
{
    public static IServiceCollection AddApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers();

        services.ConfigureErrorHandling();

        services.AddCors(configuration);

        //services.AddHttpContextAccessor();

        services.AddServices(configuration);

        return services;
    }
}
