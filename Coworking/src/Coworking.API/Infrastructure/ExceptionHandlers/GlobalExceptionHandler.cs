using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Coworking.API.Infrastructure.ExceptionHandlers;

internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken ct)
    {
        var (status, title) = ExceptionStatusMap.Map(exception);

        httpContext.Response.StatusCode = status;

        if (status == StatusCodes.Status503ServiceUnavailable)
            httpContext.Response.Headers.RetryAfter = RetryAfterSeconds();

        Log(httpContext, exception, status, title);

        var problemDetails = exception is ValidationException validation
            ? new ValidationProblemDetails(ToErrors(validation))
            : new ProblemDetails();

        problemDetails.Status = status;
        problemDetails.Title = title;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails
        });
    }

    /// ValidationProblemDetails owns the wire name for the field map, so it is never spelled here.
    private static Dictionary<string, string[]> ToErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(f => f.ErrorMessage).ToArray());

    private static string RetryAfterSeconds() => Random.Shared.Next(1, 4).ToString();

    private void Log(HttpContext httpContext, Exception exception, int status, string title)
    {
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path;

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception,
                "Unhandled {ExceptionType} for {Method} {Path} -> {Status}",
                exception.GetType().Name, method, path, status);

            return;
        }

        if (logger.IsEnabled(LogLevel.Information))
        {
            logger.LogInformation(
                "{Title}: {ExceptionType} for {Method} {Path} -> {Status}. {Message}",
                title, exception.GetType().Name, method, path, status, exception.Message);
        }
    }
}
