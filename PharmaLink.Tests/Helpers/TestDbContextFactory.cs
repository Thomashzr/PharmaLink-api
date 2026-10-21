using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PharmaLink.Context;

namespace PharmaLink.Tests.Helpers;

public static class TestDbContextFactory
{
    public static (PharmaLinkContext Context, SqliteConnection Connection) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<PharmaLinkContext>()
            .UseSqlite(connection)
            .Options;

        var context = new PharmaLinkContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
