using Microsoft.EntityFrameworkCore;

namespace WordBuddy.Identity.Infrastructure.Persistence.SqlServer;

/// <summary>SQL Server flavour of <see cref="IdentityDbContext"/>. Adds nothing to the model; owns <c>Migrations/SqlServer</c>.</summary>
public sealed class SqlServerIdentityDbContext : IdentityDbContext
{
    /// <summary>Creates the context.</summary>
    /// <param name="options">SQL Server options.</param>
    public SqlServerIdentityDbContext(DbContextOptions<SqlServerIdentityDbContext> options) : base(options)
    {
    }
}
