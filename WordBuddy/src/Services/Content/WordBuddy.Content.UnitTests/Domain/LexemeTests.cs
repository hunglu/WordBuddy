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
    public void Lexeme_Correct_ReplacesAllFields()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;
        lexeme.Enrich(PartOfSpeech.Noun, "/old/", "/old-us/", "old", ["olds"], CefrLevel.C2);

        Result result = lexeme.Correct(PartOfSpeech.Verb, "/uk/", "/us/", "ap·ple", ["apples"], CefrLevel.A1, 42);

        result.IsSuccess.Should().BeTrue();
        lexeme.PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        lexeme.IpaUk.Should().Be("/uk/");
        lexeme.IpaUs.Should().Be("/us/");
        lexeme.Syllables.Should().Be("ap·ple");
        lexeme.WordForms.Should().Equal("apples");
        lexeme.CefrLevel.Should().Be(CefrLevel.A1);
        lexeme.FrequencyRank.Should().Be(42);
        lexeme.NormalizedLemma.Should().Be("APPLE");
    }

    [Fact]
    public void Lexeme_Correct_EmptyStringsBecomeNull()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;
        lexeme.Enrich(PartOfSpeech.Noun, "/old/", "/old-us/", "old", ["olds"], CefrLevel.B1);

        Result result = lexeme.Correct(PartOfSpeech.Noun, "", "  ", "", [], null, null);

        result.IsSuccess.Should().BeTrue();
        lexeme.IpaUk.Should().BeNull();
        lexeme.IpaUs.Should().BeNull();
        lexeme.Syllables.Should().BeNull();
        lexeme.WordForms.Should().BeEmpty();
        lexeme.CefrLevel.Should().BeNull();
        lexeme.FrequencyRank.Should().BeNull();
    }

    [Fact]
    public void Lexeme_Correct_TooLongIpa_ReturnsValidation()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;
        lexeme.Enrich(PartOfSpeech.Noun, "/keep/", null, null, null, null);

        Result result = lexeme.Correct(PartOfSpeech.Noun, new string('a', Lexeme.IpaMaxLength + 1), null, null, [], null, null);

        result.Error.Type.Should().Be(ErrorType.Validation);
        lexeme.IpaUk.Should().Be("/keep/");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(Lexeme.FrequencyRankMax + 1)]
    public void Lexeme_Correct_BadFrequencyRank_ReturnsValidation(int rank)
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        lexeme.Correct(PartOfSpeech.Noun, null, null, null, [], null, rank).Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Lexeme_Correct_DuplicateWordForms_AreMerged()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        Result result = lexeme.Correct(PartOfSpeech.Noun, null, null, null, [" apples ", "APPLES", "", "apple's"], null, null);

        result.IsSuccess.Should().BeTrue();
        lexeme.WordForms.Should().Equal("apples", "apple's");
    }

    [Theory]
    [InlineData("en-GB")]
    [InlineData("en-US")]
    public void Lexeme_DetachAudio_ClearsLocaleAndReturnsOldId(string locale)
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;
        Guid uk = Guid.NewGuid();
        Guid us = Guid.NewGuid();
        lexeme.AttachAudio("en-GB", uk);
        lexeme.AttachAudio("en-US", us);

        Result<Guid?> result = lexeme.DetachAudio(locale);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(locale == "en-GB" ? uk : us);
        if (locale == "en-GB")
        {
            lexeme.UkAudioAssetId.Should().BeNull();
            lexeme.UsAudioAssetId.Should().Be(us);
        }
        else
        {
            lexeme.UsAudioAssetId.Should().BeNull();
            lexeme.UkAudioAssetId.Should().Be(uk);
        }
    }

    [Fact]
    public void Lexeme_DetachAudio_NothingLinked_ReturnsNull()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        Result<Guid?> result = lexeme.DetachAudio("en-GB");

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public void Lexeme_DetachAudio_UnknownLocale_ReturnsValidation()
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

        lexeme.DetachAudio("en-AU").Error.Type.Should().Be(ErrorType.Validation);
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
