using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// Pins that Content's service-to-service <c>/internal/vocabulary-remaps</c> endpoint is not routed
/// through the public origin (the UI's Nginx in docker-compose, the Ingress in Kubernetes): only
/// <c>/api/&lt;prefix&gt;</c> paths reach the backend, so this path falls through to the SPA (or 404)
/// and never returns Content's <c>{ items }</c> JSON. Requires the full stack, including the UI
/// container (<c>Services__Public</c>, default <c>http://localhost:3000</c>).
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
    [InlineData("/internal/vocabulary-remaps")]
    [InlineData("/internal/vocabulary-remaps?limit=500")]
    public async Task InternalVocabularyRemaps_ThroughPublicOrigin_DoesNotReachContent(string path)
    {
        IAPIRequestContext publicOrigin = await _fixture.NewContextAsync(ServiceUrls.Public);
        try
        {
            IAPIResponse response = await publicOrigin.GetAsync(path);

            response.Status.Should().NotBe(401, "a 401 would mean Content's JWT middleware handled the request");
            response.Status.Should().NotBe(403, "a 403 would mean Content's InternalService policy handled the request");

            string body = await response.TextAsync();
            body.Should().NotContain("\"items\"", "Content's remap payload must never be served on the public origin");

            response.Headers.TryGetValue("content-type", out string? contentType);
            (contentType ?? string.Empty).Should().NotContain("application/json");
        }
        finally
        {
            await publicOrigin.DisposeAsync();
        }
    }
}
