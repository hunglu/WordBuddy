using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Progress.Infrastructure.Persistence.SqlServer;

/// <summary>SQL Server flavour of <see cref="ProgressDbContext"/>. Adds nothing to the model; owns <c>Migrations/SqlServer</c>.</summary>
public sealed class SqlServerProgressDbContext : ProgressDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">SQL Server options.</param>
    public SqlServerProgressDbContext(DbContextOptions<SqlServerProgressDbContext> options) : base(options)
    {
    }
}
