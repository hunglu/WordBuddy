using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests.Features.Autofill;

/// <summary>Builders for auto-fill tests.</summary>
internal static class AutofillTestData
{
    public static DictionaryEntry Entry(string word = "apple") =>
        new(word, "/uk/", "/us/", "https://audio.test/apple-uk.mp3", "https://audio.test/apple-us.mp3", [PartOfSpeech.Noun]);

    public static GeneratedSense GeneratedSense(string definition = "a round fruit") =>
        new(definition, ["I eat an apple."], new Dictionary<string, string> { ["vi"] = "quả táo" }, [], [], [], ["food"], null, true);

    public static GeneratedWord Generated(params Guid[] matchedIds) =>
        new([new GeneratedLexeme(PartOfSpeech.Noun, "ap·ple", ["apples"], CefrLevel.A1, [GeneratedSense()], matchedIds)]);

    /// <summary>An auto-filled sense with its noun lexeme loaded.</summary>
    public static Sense CatalogSense(string word = "apple")
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), word, PartOfSpeech.Noun).Value;
        Sense sense = Sense.CreateAutoFill(Guid.NewGuid(), lexeme.Id, word, "a round fruit", ["I eat an apple."]).Value;
        typeof(Sense).GetProperty(nameof(Sense.Lexeme))!.SetValue(sense, lexeme);
        return sense;
    }
}
