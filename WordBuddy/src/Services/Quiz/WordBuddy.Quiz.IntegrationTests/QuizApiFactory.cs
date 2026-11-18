using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WordBuddy.Quiz.Api;
using WordBuddy.Quiz.Infrastructure.Persistence;

namespace WordBuddy.Quiz.IntegrationTests;

/// <summary>
/// Boots the real Quiz API host against a per-run database (migrated and seeded by the Development
/// startup, dropped on disposal) — never mocks EF Core. Default: local SQL Server (override with
/// <c>QUIZ_TEST_CONNECTION_STRING</c>). Set <c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c> and
/// <c>WORDBUDDY_TEST_PG_CONNECTION_STRING</c> (server part only, e.g. <c>Host=localhost;Username=postgres;Password=...</c>)
/// to run on PostgreSQL. Configuration goes through environment variables, as in the other services.
/// </summary>
public sealed class QuizApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string JwtSecret = "integration-test-secret-do-not-use-in-production-32chars";
    public const string JwtIssuer = "WordBuddy";

    /// <summary>True when the run targets PostgreSQL (<c>WORDBUDDY_TEST_DB_PROVIDER=Postgres</c>).</summary>
    public static readonly bool IsPostgres = string.Equals(
        Environment.GetEnvironmentVariable("WORDBUDDY_TEST_DB_PROVIDER"), "Postgres", StringComparison.OrdinalIgnoreCase);

    private static readonly string ConnectionString = BuildConnectionString();

    private static string BuildConnectionString()
    {
        string databaseName = $"WordBuddyQuizTests_{Guid.NewGuid():N}";
        if (IsPostgres)
        {
            string server = Environment.GetEnvironmentVariable("WORDBUDDY_TEST_PG_CONNECTION_STRING")
                ?? throw new InvalidOperationException("Set WORDBUDDY_TEST_PG_CONNECTION_STRING (server part only) when WORDBUDDY_TEST_DB_PROVIDER=Postgres.");
            return $"{server.TrimEnd(';')};Database={databaseName}";
        }

        return Environment.GetEnvironmentVariable("QUIZ_TEST_CONNECTION_STRING")
            ?? $"Server=(localdb)\\mssqllocaldb;Database={databaseName};Trusted_Connection=true";
    }

    static QuizApiFactory()
    {
        Environment.SetEnvironmentVariable("Database__Provider", IsPostgres ? "Postgres" : "SqlServer");
        Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Secret", JwtSecret);
        Environment.SetEnvironmentVariable("Jwt__Issuer", JwtIssuer);
    }

    /// <summary>Creates a JWT shaped like Identity's: <c>sub</c> and <c>age_group</c> claims.</summary>
    public static string CreateToken(Guid userId, string ageGroup)
    {
        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("age_group", ageGroup),
            new("is_admin", "false"),
        ];

        SigningCredentials credentials = new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtSecret)), SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(issuer: JwtIssuer, claims: claims, expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
    }

    public Task InitializeAsync()
    {
        // Building the host runs the Development migrate + seed.
        _ = Services;
        return Task.CompletedTask;
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        QuizDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
        await dbContext.Database.EnsureDeletedAsync();
    }
}

/// <summary>All Quiz API test classes share one factory (one database per run).</summary>
[CollectionDefinition(Name)]
public sealed class QuizApiCollection : ICollectionFixture<QuizApiFactory>
{
    public const string Name = "QuizApi";
}
