using Coworking.API.Infrastructure.ExceptionHandlers;
using Microsoft.AspNetCore.Diagnostics;

namespace Coworking.API.Infrastructure.Extensions
{
    internal static class ErrorHandlerExtensions
    {
        internal static IServiceCollection ConfigureErrorHandling(this IServiceCollection services)
        {
            services.AddCustomizedProblemDetails();
            services.AddExceptionHandler<GlobalExceptionHandler>();

            return services;
        }

        private static IServiceCollection AddCustomizedProblemDetails(this IServiceCollection services)
        {
            return services.AddProblemDetails(options =>
                options.CustomizeProblemDetails = ctx =>
                {
                    var error = ctx.Exception
                        ?? ctx.HttpContext.Features.Get<IExceptionHandlerFeature>()?.Error;
                    var env = ctx.HttpContext.RequestServices.GetRequiredService<IHostEnvironment>();

                    if (error is not null)
                    {
                        var statusCode = ctx.HttpContext.Response.StatusCode;

                        bool isTechnicalError = statusCode >= 500;

                        bool hideDetails = isTechnicalError && !env.IsDevelopment();

                        ctx.ProblemDetails.Detail = hideDetails ? "An internal error occurred." : error.Message;

                    }

                    // traceId stays in every environment — it is the only handle
                    // tying a user-reported error to a log entry
                    if (env.IsDevelopment())
                    {
                        ctx.ProblemDetails.Extensions["instance"] = $"{ctx.HttpContext.Request.Method} {ctx.HttpContext.Request.Path}";
                        ctx.ProblemDetails.Extensions["environment"] = env.EnvironmentName;
                    }
                });
        }
    }
}
