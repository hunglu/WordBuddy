using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Pins that Content's former service-to-service <c>/internal/vocabulary-remaps</c> routes are gone
/// (removed with the remap table in WB-12). Called on Content directly, both routes return 404 —
/// not 401/403, which would mean an endpoint still exists behind auth.
/// </summary>
[Collection(ApiRequestContextCollection.Name)]
public sealed class InternalEndpointExposureTests
{
    private readonly ApiRequestContextFixture _fixture;

    public InternalEndpointExposureTests(ApiRequestContextFixture fixture)
    {
        _fixture = fixture;
    }

    [Theory]
    [InlineData("GET", "/internal/vocabulary-remaps?limit=500")]
    [InlineData("POST", "/internal/vocabulary-remaps/acknowledge")]
    public async Task InternalVocabularyRemaps_OnContent_Returns404(string method, string path)
    {
        IAPIRequestContext content = await _fixture.NewContextAsync(ServiceUrls.Content);
        try
        {
            IAPIResponse response = method == "GET"
                ? await content.GetAsync(path)
                : await content.PostAsync(path, new APIRequestContextOptions { DataObject = new { oldIds = Array.Empty<Guid>() } });

            response.Status.Should().Be(404, "the remap routes were removed from Content");
        }
        finally
        {
            await content.DisposeAsync();
        }
    }
}
