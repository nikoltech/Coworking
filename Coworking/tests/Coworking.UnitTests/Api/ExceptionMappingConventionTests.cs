using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Cancel;
using Coworking.Domain.Entities;
using Coworking.Domain.Exceptions;
using System.Reflection;

namespace Coworking.UnitTests.Api;

/// <summary>
/// ExceptionStatusMap ends in a "_ =>" arm, so a new exception inheriting from plain Exception
/// becomes a 500 silently — which is how InvalidTransitionException and BookingOverlapException
/// got there.
/// </summary>
public class ExceptionMappingConventionTests
{
    /// Bases ExceptionStatusMap has an arm for. Keep in sync when adding one.
    private static readonly Type[] MappedBases =
    [
        typeof(DomainException),
        typeof(FluentValidation.ValidationException),
        typeof(NotFoundException),
        typeof(ConflictException),
        typeof(BusinessRuleException)
    ];

    /// Exceptions that are meant to surface as 500.
    private static readonly Type[] DeliberateServerErrors = [];

    private static readonly Assembly[] Scanned =
    [
        typeof(Booking).Assembly,
        typeof(CancelBookingCommand).Assembly
    ];

    [Fact]
    public void EveryExceptionRootsUnderAMappedBase()
    {
        var unmapped = Scanned
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAssignableTo(typeof(Exception)))
            .Where(t => !DeliberateServerErrors.Contains(t))
            .Where(t => !MappedBases.Any(t.IsAssignableTo))
            .Select(t => t.FullName)
            .ToList();

        Assert.True(unmapped.Count == 0,
            "These exceptions fall into the 500 arm of ExceptionStatusMap. Give each one an arm "
            + "in the map, or inherit it from DomainException:\n  " + string.Join("\n  ", unmapped));
    }
}
