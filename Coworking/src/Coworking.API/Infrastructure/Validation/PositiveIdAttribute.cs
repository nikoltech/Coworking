using System.ComponentModel.DataAnnotations;

namespace Coworking.API.Infrastructure.Validation;

/// <summary>
/// A route or query identifier must be positive. The ceiling is stated at every use so the
/// generated schema never promises a range the key type cannot hold.
/// </summary>
public sealed class PositiveIdAttribute(double maximum) : RangeAttribute(1d, maximum)
{
    public override string FormatErrorMessage(string name) => $"{name} must be a positive number.";
}
