using Coworking.API.Infrastructure.ExceptionHandlers;
using Coworking.Application.Common.Exceptions;
using FluentValidation;
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

                        ctx.ProblemDetails.Detail = (isTechnicalError && !env.IsDevelopment())
                            ? "An internal error occurred."
                            : error.Message;

                        if (error is ValidationException ve)
                        {
                            ctx.ProblemDetails.Extensions["errors"] = ve.Errors
                                .GroupBy(e => e.PropertyName)
                                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage));
                        }
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
