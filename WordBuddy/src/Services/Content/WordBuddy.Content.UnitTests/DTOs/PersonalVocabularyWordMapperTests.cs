using FluentAssertions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests.DTOs;

public class PersonalVocabularyWordMapperTests
{
    [Fact]
    public void PersonalVocabularyWordMapper_ToDtoLink_CarriesPersonalContext()
    {
        Sense sense = TestWords.System();
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), sense, isAuthor: false);
        link.SetPersonalContext("at the zoo");

        PersonalVocabularyWordDto dto = PersonalVocabularyWordMapper.ToDto(link, AgeGroup.Adult);

        dto.PersonalContext.Should().Be("at the zoo");
    }

    [Fact]
    public void PersonalVocabularyWordMapper_ToDtoLink_NoNote_IsNull()
    {
        Sense sense = TestWords.System();
        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), sense, isAuthor: false);

        PersonalVocabularyWordMapper.ToDto(link, AgeGroup.Child).PersonalContext.Should().BeNull();
    }

    [Fact]
    public void PersonalVocabularyWordMapper_ToDtoSense_NeverCarriesPersonalContext()
    {
        Sense sense = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);

        PersonalVocabularyWordMapper.ToDto(sense).PersonalContext.Should().BeNull();
    }
}
