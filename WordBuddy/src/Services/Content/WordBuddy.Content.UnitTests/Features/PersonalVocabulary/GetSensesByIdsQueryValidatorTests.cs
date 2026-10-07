using FluentAssertions;
using FluentValidation.Results;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class GetSensesByIdsQueryValidatorTests
{
    private readonly GetSensesByIdsQueryValidator _validator = new();

    private static GetSensesByIdsQuery Query(IReadOnlyList<Guid> ids) => new(ids, Guid.NewGuid(), AgeGroup.Adult);

    [Fact]
    public void GetSensesByIdsQueryValidator_Validate_EmptyList_IsInvalid()
    {
        ValidationResult result = _validator.Validate(Query([]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSensesByIdsQueryValidator_Validate_MoreThan100Ids_IsInvalid()
    {
        List<Guid> ids = Enumerable.Range(0, 101).Select(_ => Guid.NewGuid()).ToList();

        ValidationResult result = _validator.Validate(Query(ids));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSensesByIdsQueryValidator_Validate_EmptyGuid_IsInvalid()
    {
        ValidationResult result = _validator.Validate(Query([Guid.NewGuid(), Guid.Empty]));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void GetSensesByIdsQueryValidator_Validate_100Ids_IsValid()
    {
        List<Guid> ids = Enumerable.Range(0, 100).Select(_ => Guid.NewGuid()).ToList();

        ValidationResult result = _validator.Validate(Query(ids));

        result.IsValid.Should().BeTrue();
    }
}
