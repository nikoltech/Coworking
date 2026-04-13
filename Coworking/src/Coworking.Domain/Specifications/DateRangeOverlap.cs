using System;
using System.Collections.Generic;
using System.Text;

namespace Coworking.Domain.Specifications;

public static class DateRangeOverlap
{
    /// <summary>
    /// Two ranges overlap if one starts before the other ends.
    /// Border times are allowed — [10:00-11:00] and [11:00-12:00] do not overlap.
    /// </summary>
    public static bool Check(
        DateTimeOffset start, DateTimeOffset end,
        DateTimeOffset otherStart, DateTimeOffset otherEnd) =>
        start < otherEnd && end > otherStart;
}
