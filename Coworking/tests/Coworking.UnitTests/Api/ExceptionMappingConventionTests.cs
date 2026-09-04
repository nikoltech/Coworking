using Coworking.API.Infrastructure.ExceptionHandlers;
using Coworking.Application.Common.Exceptions;
using Coworking.Application.Features.Bookings.Commands.Cancel;
using Coworking.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Reflection;

namespace Coworking.UnitTests.Api;

/// <summary>
/// ExceptionStatusMap falls back to 500, so a new exception becomes a server error silently.
/// Only Domain and Application are scanned: anything else reaching a response is an adapter leak.
/// </summary>
public class ExceptionMappingConventionTests
{
    private static readonly Type[] DeliberateServerErrors = [];

    private static readonly Assembly[] Scanned =
    [
        typeof(Booking).Assembly,
        typeof(CancelBookingCommand).Assembly
    ];

    [Fact]
    public void EveryExceptionIsMapped()
    {
        var unmapped = Scanned
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsAssignableTo(typeof(Exception)))
            .Where(t => !DeliberateServerErrors.Contains(t))
            .Where(t => ExceptionStatusMap.Map(t).Status
                        == StatusCodes.Status500InternalServerError)
            .Select(t => t.FullName)
            .ToList();

        Assert.True(unmapped.Count == 0,
            "These exceptions fall back to 500. Add an entry to ExceptionStatusMap, or inherit "
            + "from one that is already mapped:\n  " + string.Join("\n  ", unmapped));
    }

    [Theory]
    [InlineData(typeof(NotFoundException), StatusCodes.Status404NotFound)]
    [InlineData(typeof(ConflictException), StatusCodes.Status409Conflict)]
    [InlineData(typeof(TransactionConflictException), StatusCodes.Status503ServiceUnavailable)]
    public void MappedException_KeepsItsStatus(Type exceptionType, int expected)
    {
        Assert.Equal(expected, ExceptionStatusMap.Map(exceptionType).Status);
    }
}
