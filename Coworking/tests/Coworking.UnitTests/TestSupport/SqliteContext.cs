using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Coworking.UnitTests.TestSupport;

/// <summary>
/// An in-memory SQLite database behind TestAppDbContext. The handler needs genuine EF:
/// Include/ThenInclude and transactions, which a substitute cannot provide.
/// </summary>
internal sealed class SqliteContext : IDisposable
{
    private readonly SqliteConnection _connection;

    public TestAppDbContext Db { get; }

    public SqliteContext()
    {
        // the connection must stay open: closing it drops the database
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        Db = NewContext();
        Db.Database.EnsureCreated();
    }

    /// A second context over the same database, for simulating another writer.
    public TestAppDbContext NewContext() =>
        new(new DbContextOptionsBuilder<TestAppDbContext>()
            .UseSqlite(_connection)
            .Options);

    public void Dispose()
    {
        Db.Dispose();
        _connection.Dispose();
    }
}
