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
}
