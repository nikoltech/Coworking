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

        // UseExceptionHandler already set 500; ProblemDetails.Status alone only changes the body
        httpContext.Response.StatusCode = status;

        Log(httpContext, exception, status, title);

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception
        });
    }

    /// <summary>
    /// The only place that sees every exception, including those raised outside MediatR.
    /// Mapped 4xx are normal outcomes, so only unmapped ones are logged as incidents.
    /// </summary>
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

        logger.LogInformation(
            "{Title}: {ExceptionType} for {Method} {Path} -> {Status}. {Message}",
            title, exception.GetType().Name, method, path, status, exception.Message);
    }
}
