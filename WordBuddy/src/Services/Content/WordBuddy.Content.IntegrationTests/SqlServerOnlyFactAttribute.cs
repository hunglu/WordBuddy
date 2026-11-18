namespace WordBuddy.Content.IntegrationTests;

/// <summary>
/// A fact that runs on SQL Server only. The migration tests replay the old SQL Server migrations with
/// SQL Server syntax; PostgreSQL starts empty with one <c>InitialCreate</c>, so there is nothing to replay.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public sealed class SqlServerOnlyFactAttribute : FactAttribute
{
    /// <summary>Skips the test when the run targets PostgreSQL.</summary>
    public SqlServerOnlyFactAttribute()
    {
        if (ContentApiFactory.IsPostgres)
        {
            Skip = "SQL Server migration replay; PostgreSQL has a single InitialCreate.";
        }
    }
}
