using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-24: a child is blocked from learning (403 <c>Learner.SupporterRequired</c>) until an adult
/// supporter is linked. Tests that use a child learner call <see cref="LinkSupporterAsync"/> first.
/// </summary>
internal static class ChildSupport
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

    /// <summary>Links a fresh adult as Primary supporter and waits until Content and Progress allow learning.</summary>
    public static async Task LinkSupporterAsync(IAPIRequestContext identity, string childToken)
    {
        IAPIResponse register = await identity.PostAsync("/api/auth/register", new APIRequestContextOptions
        {
            DataObject = new
            {
                email = $"supporter-e2e-{Guid.NewGuid():N}@example.com",
                password = "ChangeMe123!",
                displayName = "E2E Supporter",
                ageGroup = "Adult",
            },
        });
        register.Ok.Should().BeTrue(await register.TextAsync());
        string supporterToken = (await register.JsonAsync())!.Value.GetProperty("token").GetString()!;

        IAPIResponse invitation = await identity.PostAsync("/api/auth/support-links/invitations", new APIRequestContextOptions
        {
            Headers = Auth(childToken),
            DataObject = new { inviteAs = "Learner", relationship = (string?)null },
        });
        invitation.Status.Should().Be(201, await invitation.TextAsync());
        string code = (await invitation.JsonAsync())!.Value.GetProperty("code").GetString()!;

        IAPIResponse accept = await identity.PostAsync("/api/auth/support-links/accept", new APIRequestContextOptions
        {
            Headers = Auth(supporterToken),
            DataObject = new { code, token = (string?)null },
        });
        accept.Status.Should().Be(200, await accept.TextAsync());

        await WaitForProjectionsAsync(childToken);
    }

    private static async Task WaitForProjectionsAsync(string childToken)
    {
        using IPlaywright playwright = await Playwright.CreateAsync();
        IAPIRequestContext content = await playwright.APIRequest.NewContextAsync(new() { BaseURL = ServiceUrls.Content });
        IAPIRequestContext progress = await playwright.APIRequest.NewContextAsync(new() { BaseURL = ServiceUrls.Progress });
        try
        {
            DateTime deadline = DateTime.UtcNow + Timeout;
            while (DateTime.UtcNow < deadline)
            {
                IAPIResponse words = await content.GetAsync("/api/lessons", new() { Headers = Auth(childToken) });
                // POST reviews with an empty body: 403 while gated, 400 (validation) once allowed.
                // A GET session is avoided here, it could cache an empty session for the day.
                IAPIResponse review = await progress.PostAsync("/api/progress/vocabulary/reviews", new()
                {
                    Headers = Auth(childToken),
                    DataObject = new { },
                });
                if (words.Status == 200 && review.Status == 400)
                {
                    return;
                }

                await Task.Delay(1000);
            }

            throw new TimeoutException($"Child supporter link did not reach Content and Progress within {Timeout.TotalSeconds}s.");
        }
        finally
        {
            await content.DisposeAsync();
            await progress.DisposeAsync();
        }
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
