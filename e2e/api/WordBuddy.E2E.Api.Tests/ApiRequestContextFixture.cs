using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Shares one Playwright driver instance across all API test classes in the collection, since
/// starting the driver per test class is unnecessarily slow. Each test class creates its own
/// <see cref="IAPIRequestContext"/> scoped to the service base URL it needs.
/// </summary>
public sealed class ApiRequestContextFixture : IAsyncLifetime
{
    public IPlaywright Playwright { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
    }

    public Task DisposeAsync()
    {
        Playwright.Dispose();
        return Task.CompletedTask;
    }

    public Task<IAPIRequestContext> NewContextAsync(string baseUrl)
    {
        return Playwright.APIRequest.NewContextAsync(new APIRequestNewContextOptions
        {
            BaseURL = baseUrl,
        });
    }
}

[CollectionDefinition(Name)]
public sealed class ApiRequestContextCollection : ICollectionFixture<ApiRequestContextFixture>
{
    public const string Name = "Api Request Context";
}
