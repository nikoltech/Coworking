using Microsoft.AspNetCore.Diagnostics;

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

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = { Status = status, Title = title }
        });
    }

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
