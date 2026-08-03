using Coworking.Application.Common.Exceptions;
using FluentValidation;

namespace Coworking.API.Infrastructure.ExceptionHandlers;

internal static class ExceptionStatusMap
{
    public static (int Status, string Title) Map(Exception? error) =>
        error switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation Failed"),
            NotFoundException => (StatusCodes.Status404NotFound, "Not Found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            BusinessRuleException => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violated"),
            _ => (StatusCodes.Status500InternalServerError, "Server Error")
        };
}
