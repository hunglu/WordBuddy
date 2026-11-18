using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Content.Infrastructure.Persistence.SqlServer;

/// <summary>SQL Server flavour of <see cref="ContentDbContext"/>. Adds nothing to the model; owns <c>Migrations/SqlServer</c>.</summary>
public sealed class SqlServerContentDbContext : ContentDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">SQL Server options.</param>
    public SqlServerContentDbContext(DbContextOptions<SqlServerContentDbContext> options) : base(options)
    {
    }

    /// <summary>Runtime constructor: <paramref name="serviceProvider"/> resolves the outbox publish endpoint lazily.</summary>
    /// <param name="options">SQL Server options.</param>
    /// <param name="serviceProvider">The request scope.</param>
    public SqlServerContentDbContext(DbContextOptions<SqlServerContentDbContext> options, IServiceProvider serviceProvider) : base(options, serviceProvider)
    {
    }
}
