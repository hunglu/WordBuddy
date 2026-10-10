using System.Text.Json;
using FluentAssertions;
using Microsoft.Playwright;

namespace WordBuddy.E2E.Api.Tests;

/// <summary>
/// WB-28 helpers: the server builds each exercise and checks each answer, so a test first asks for an
/// exercise (<c>POST /api/progress/vocabulary/exercises</c>) and then sends a raw answer. Blackbox HTTP only.
/// </summary>
internal static class VocabularyExercises
{
    /// <summary>Asks Progress for an exercise for one word of the open session.</summary>
    public static async Task<JsonElement> CreateAsync(IAPIRequestContext progress, string token, Guid sessionId, Guid senseId)
    {
        IAPIResponse response = await progress.PostAsync("/api/progress/vocabulary/exercises", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new { sessionId, senseId },
        });
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value;
    }

    /// <summary>Sends a raw answer to an issued exercise. The caller checks the status.</summary>
    public static Task<IAPIResponse> AnswerAsync(
        IAPIRequestContext progress, string token, Guid exerciseId, string? optionKey, string? text, int clientResponseMs = 0) =>
        progress.PostAsync("/api/progress/vocabulary/reviews", new APIRequestContextOptions
        {
            Headers = Auth(token),
            DataObject = new
            {
                exerciseId,
                answer = new { optionKey, text },
                clientResponseMs,
                hintUsed = false,
            },
        });

    /// <summary>Creates an exercise and answers it right (the test knows the word). Returns the review result.</summary>
    public static async Task<JsonElement> AnswerRightAsync(
        IAPIRequestContext progress, string token, Guid sessionId, Guid senseId, string word)
    {
        JsonElement exercise = await CreateAsync(progress, token, sessionId, senseId);
        (string? optionKey, string? text) = RightAnswer(exercise, word);
        IAPIResponse response = await AnswerAsync(progress, token, exercise.GetProperty("exerciseId").GetGuid(), optionKey, text);
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value;
    }

    /// <summary>Creates an exercise and answers it wrong. Returns the review result.</summary>
    public static async Task<JsonElement> AnswerWrongAsync(
        IAPIRequestContext progress, string token, Guid sessionId, Guid senseId, string? word = null)
    {
        JsonElement exercise = await CreateAsync(progress, token, sessionId, senseId);
        (string? optionKey, string? text) = WrongAnswer(exercise, word);
        IAPIResponse response = await AnswerAsync(progress, token, exercise.GetProperty("exerciseId").GetGuid(), optionKey, text);
        response.Status.Should().Be(200, await response.TextAsync());
        return (await response.JsonAsync())!.Value;
    }

    /// <summary>The raw answer that is right for <paramref name="exercise"/>.</summary>
    public static (string? OptionKey, string? Text) RightAnswer(JsonElement exercise, string word)
    {
        if (exercise.GetProperty("exerciseType").GetString() == "Typing")
        {
            return (null, word);
        }

        string key = exercise.GetProperty("options").EnumerateArray()
            .Single(o => o.GetProperty("text").GetString() == word).GetProperty("key").GetString()!;
        return (key, null);
    }

    /// <summary>A raw answer that is wrong for <paramref name="exercise"/>. For a choice exercise with an unknown word, the first option.</summary>
    public static (string? OptionKey, string? Text) WrongAnswer(JsonElement exercise, string? word)
    {
        if (exercise.GetProperty("exerciseType").GetString() == "Typing")
        {
            return (null, "e2e-definitely-wrong");
        }

        string key = exercise.GetProperty("options").EnumerateArray()
            .First(o => word is null || o.GetProperty("text").GetString() != word).GetProperty("key").GetString()!;
        return (key, null);
    }

    private static Dictionary<string, string> Auth(string token) => new() { ["Authorization"] = $"Bearer {token}" };
}
