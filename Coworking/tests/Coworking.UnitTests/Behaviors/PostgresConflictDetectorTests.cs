using Coworking.Infrastructure.Persistence.Transactions.Conflicts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Coworking.UnitTests.Behaviors;

/// <summary>
/// Decides what TransactionConflictRetryBehavior replays. Too narrow and a recoverable conflict
/// surfaces as a 500; too wide and a genuine failure is retried three times before failing.
/// </summary>
public class PostgresConflictDetectorTests
{
    private readonly PostgresConflictDetector _detector = new();

    [Theory]
    [InlineData("40001")] // serialization failure
    [InlineData("40P01")] // deadlock detected
    public void SerializationConflicts_AreTransient(string sqlState)
    {
        Assert.True(_detector.IsTransient(PostgresError(sqlState)));
    }

    [Theory]
    [InlineData("23505")] // unique violation
    [InlineData("23503")] // foreign key violation
    [InlineData("25P02")] // transaction aborted
    public void OtherDatabaseErrors_AreNotTransient(string sqlState)
    {
        Assert.False(_detector.IsTransient(PostgresError(sqlState)));
    }

    [Fact]
    public void WrappedSerializationConflict_IsTransient()
    {
        var wrapped = new DbUpdateException("save failed", PostgresError("40001"));

        Assert.True(_detector.IsTransient(wrapped));
    }

    [Fact]
    public void NonDatabaseException_IsNotTransient()
    {
        Assert.False(_detector.IsTransient(new InvalidOperationException("business rule")));
    }

    [Fact]
    public void ConcurrencyConflict_IsNotTransient()
    {
        Assert.False(_detector.IsTransient(new DbUpdateConcurrencyException()));
    }

    private static PostgresException PostgresError(string sqlState) =>
        new("conflict", "ERROR", "ERROR", sqlState);
}
