using System.Net.Sockets;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Runs a test only when <c>E2E_PRODUCTION_URL</c> is set (for example <c>https://localhost</c>);
/// otherwise the test is reported as skipped.
/// </summary>
public sealed class ProductionFactAttribute : FactAttribute
{
    public ProductionFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(ProductionSmokeTests.ProductionUrl))
        {
            Skip = "Set E2E_PRODUCTION_URL (e.g. https://localhost) to run the production Compose smoke tests.";
        }
    }
}

/// <summary>
/// Smoke tests for the single-host production Compose stack (<c>WordBuddy/deploy/single-host</c>,
/// started with <c>SITE_ADDRESS=localhost</c>). Variables: <c>E2E_PRODUCTION_URL</c>,
/// <c>E2E_ADMIN_EMAIL</c> and <c>E2E_ADMIN_PASSWORD</c> (the bootstrap admin).
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public class ProductionSmokeTests
{
    private static readonly int[] InternalPorts = [5432, 5672, 15672, 5080, 5081, 5082, 5083, 5084];

    private readonly ApiRequestContextFixture _fixture;

    public ProductionSmokeTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    internal static string? ProductionUrl => Environment.GetEnvironmentVariable("E2E_PRODUCTION_URL");

    private static string AdminEmail =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_EMAIL") ?? "admin@example.com";

    private static string AdminPassword =>
        Environment.GetEnvironmentVariable("E2E_ADMIN_PASSWORD") ?? string.Empty;

    private Task<IAPIRequestContext> NewContextAsync(string baseUrl)
    {
        return _fixture.Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = baseUrl,
            IgnoreHTTPSErrors = true,
        });
    }

    [ProductionFact]
    public async Task GetRoot_OverHttps_ReturnsOk()
    {
        IAPIRequestContext context = await NewContextAsync(ProductionUrl!);
        try
        {
            IAPIResponse response = await context.GetAsync("/");

            response.Status.Should().Be(200);
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    [ProductionFact]
    public async Task GetRoot_OverHttp_RedirectsToHttps()
    {
        string httpUrl = new UriBuilder(ProductionUrl!) { Scheme = Uri.UriSchemeHttp, Port = -1 }.Uri.ToString();
        IAPIRequestContext context = await NewContextAsync(httpUrl);
        try
        {
            IAPIResponse response = await context.GetAsync("/", new APIRequestContextOptions { MaxRedirects = 0 });

            response.Status.Should().BeOneOf(301, 302, 307, 308);
            response.Headers["location"].Should().StartWith("https://");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    [ProductionFact]
    public async Task PostLogin_WithBootstrapAdmin_ReturnsToken()
    {
        AdminPassword.Should().NotBeEmpty("E2E_ADMIN_PASSWORD must hold the bootstrap admin password");
        IAPIRequestContext context = await NewContextAsync(ProductionUrl!);
        try
        {
            IAPIResponse response = await context.PostAsync("/api/auth/login", new APIRequestContextOptions
            {
                DataObject = new { email = AdminEmail, password = AdminPassword },
            });

            response.Status.Should().Be(200);
            (await response.TextAsync()).Should().Contain("token");
        }
        finally
        {
            await context.DisposeAsync();
        }
    }

    [ProductionTheory]
    [InlineData(5432)]
    [InlineData(5672)]
    [InlineData(15672)]
    [InlineData(5080)]
    [InlineData(5081)]
    [InlineData(5082)]
    [InlineData(5083)]
    [InlineData(5084)]
    public async Task Connect_InternalPort_IsNotReachableFromHost(int port)
    {
        string host = new Uri(ProductionUrl!).Host;
        using TcpClient client = new();
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(3));

        Func<Task> connect = async () => await client.ConnectAsync(host, port, timeout.Token);

        await connect.Should().ThrowAsync<Exception>($"port {port} must not be published");
    }
}

/// <summary>Theory variant of <see cref="ProductionFactAttribute"/>.</summary>
public sealed class ProductionTheoryAttribute : TheoryAttribute
{
    public ProductionTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(ProductionSmokeTests.ProductionUrl))
        {
            Skip = "Set E2E_PRODUCTION_URL (e.g. https://localhost) to run the production Compose smoke tests.";
        }
    }
}
