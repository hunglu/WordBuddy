using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class LexemeTests
{
    [Fact]
    public void Lexeme_Create_TrimsAndNormalizesLemma()
    {
        Result<Lexeme> result = Lexeme.Create(Guid.NewGuid(), " Apple");

        result.IsSuccess.Should().BeTrue();
        result.Value.Lemma.Should().Be("Apple");
        result.Value.NormalizedLemma.Should().Be("APPLE");
    }

    [Fact]
    public void Lexeme_Create_DefaultsPartOfSpeechToNull()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        lexeme.PartOfSpeech.Should().BeNull();
        lexeme.WordForms.Should().BeEmpty();
    }

    [Fact]
    public void Lexeme_Create_StoresGivenPartOfSpeech()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple", PartOfSpeech.Noun).Value;

        lexeme.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Lexeme_Create_FailsOnEmptyLemma(string lemma)
    {
        Result<Lexeme> result = Lexeme.Create(Guid.NewGuid(), lemma);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Lexeme_Enrich_UnknownPos_SetsPosAndDictionaryData()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        Result result = lexeme.Enrich(PartOfSpeech.Noun, "/ˈæp.əl/", "/ˈæp.əl/", "ap·ple", ["apples", "apples"], CefrLevel.A1);

        result.IsSuccess.Should().BeTrue();
        lexeme.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        lexeme.IpaUk.Should().Be("/ˈæp.əl/");
        lexeme.Syllables.Should().Be("ap·ple");
        lexeme.WordForms.Should().Equal("apples");
        lexeme.CefrLevel.Should().Be(CefrLevel.A1);
    }

    [Fact]
    public void Lexeme_Enrich_DifferentPos_ReturnsConflict()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "run", PartOfSpeech.Verb).Value;

        Result result = lexeme.Enrich(PartOfSpeech.Noun, null, null, null, null, null);

        result.Error.Type.Should().Be(ErrorType.Conflict);
        lexeme.PartOfSpeech.Should().Be(PartOfSpeech.Verb);
    }

    [Fact]
    public void Lexeme_Enrich_EmptyValues_KeepOldValues()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;
        lexeme.Enrich(null, "/uk/", null, null, ["apples"], null);

        lexeme.Enrich(null, " ", null, null, [], null);

        lexeme.IpaUk.Should().Be("/uk/");
        lexeme.WordForms.Should().Equal("apples");
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    public void Lexeme_AttachAudio_KnownLocale_LinksAsset(string locale)
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;
        Guid assetId = Guid.NewGuid();

        lexeme.AttachAudio(locale, assetId).IsSuccess.Should().BeTrue();

        (locale == "en-GB" ? lexeme.UkAudioAssetId : lexeme.UsAudioAssetId).Should().Be(assetId);
    }

    [Fact]
    public void Lexeme_AttachAudio_OtherLocale_FailsValidation()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        lexeme.AttachAudio("en-AU", Guid.NewGuid()).Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Lexeme_AudioFileName_UsesLexemeIdAndLocale()
    {
        Guid id = Guid.NewGuid();

        Lexeme.AudioFileName(id, Lexeme.UkLocale).Should().Be($"lexeme-{id}-en-GB.mp3");
    }

    [Fact]
    public void Lexeme_RederiveLemma_TakesMatchingVisibleSenseWord()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "APPLE").Value;

        Result result = lexeme.RederiveLemma(["pear", " Apple "]);

        result.IsSuccess.Should().BeTrue();
        lexeme.Lemma.Should().Be("Apple");
    }

    [Fact]
    public void Lexeme_RederiveLemma_NoMatchingWord_FailsAndKeepsLemma()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        Result result = lexeme.RederiveLemma(["pear"]);

        result.IsFailure.Should().BeTrue();
        lexeme.Lemma.Should().Be("apple");
    }
}
