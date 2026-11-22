using System.Net;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using WordBuddy.Progress.Api;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>WB-75: Swagger is a Development-only feature, so a Production host must not serve it.</summary>
public sealed class ProductionEnvironmentTests
{
    [Fact]
    public async Task ProductionHost_GetSwaggerIndex_ReturnsNotFound()
    {
        // Touching the factory runs its static setup (JWT secret, connection string, provider) for this assembly.
        _ = ProgressApiFactory.IsPostgres;

        await using WebApplicationFactory<Program> factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using HttpClient client = factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync("/swagger/index.html");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
