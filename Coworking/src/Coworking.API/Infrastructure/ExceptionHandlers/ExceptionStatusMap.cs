using Coworking.Application.Common.Exceptions;
using Coworking.Domain.Exceptions;
using FluentValidation;

namespace Coworking.API.Infrastructure.ExceptionHandlers;

internal static class ExceptionStatusMap
{
    public static IReadOnlyDictionary<Type, (int Status, string Title)> Mapped { get; } =
        new Dictionary<Type, (int, string)>
        {
            [typeof(ValidationException)] = (StatusCodes.Status400BadRequest, "Validation Failed"),
            [typeof(NotFoundException)] = (StatusCodes.Status404NotFound, "Not Found"),
            [typeof(ConflictException)] = (StatusCodes.Status409Conflict, "Conflict"),
            [typeof(BusinessRuleException)] = (StatusCodes.Status422UnprocessableEntity, "Business Rule Violated"),

            // DomainExceptionBehavior converts these on the MediatR path; this covers the rest
            [typeof(DomainException)] = (StatusCodes.Status422UnprocessableEntity, "Business Rule Violated"),
            [typeof(TransactionConflictException)] = (StatusCodes.Status503ServiceUnavailable, "Service Busy")
        };

    private static readonly (int Status, string Title) Unmapped =
        (StatusCodes.Status500InternalServerError, "Server Error");

    public static (int Status, string Title) Map(Exception? error) => Map(error?.GetType());

    public static (int Status, string Title) Map(Type? exceptionType)
    {
        for (var type = exceptionType; type is not null; type = type.BaseType)
        {
            if (Mapped.TryGetValue(type, out var mapped))
                return mapped;
        }

        return Unmapped;
    }
}
