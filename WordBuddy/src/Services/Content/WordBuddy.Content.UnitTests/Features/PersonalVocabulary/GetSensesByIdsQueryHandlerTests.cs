using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class GetSensesByIdsQueryHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly GetSensesByIdsQueryValidator _validator = new();
    private readonly Mock<ILogger<GetSensesByIdsQueryHandler>> _logger = new();
    private readonly Guid _callerId = Guid.NewGuid();

    private GetSensesByIdsQueryHandler CreateHandler() => new(_repository.Object, _validator, _logger.Object);

    private void SetupCandidates(params SenseReviewCandidate[] candidates) =>
        _repository
            .Setup(r => r.GetForReviewAsync(_callerId, It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<SenseReviewCandidate>>(candidates));

    /// <summary><see cref="LearnerWord.PersonalContext"/> has no writer yet; set it for the test only.</summary>
    private static LearnerWord LinkWithContext(Guid userId, Sense sense, string context)
    {
        LearnerWord link = new(Guid.NewGuid(), userId, sense, isAuthor: false);
        typeof(LearnerWord)
            .GetProperty(nameof(LearnerWord.PersonalContext), BindingFlags.Public | BindingFlags.Instance)!
            .SetValue(link, context);
        return link;
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_ChildCaller_OmitsNonChildVisibleSharedSense()
    {
        Sense hidden = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false, "secret");
        Sense visible = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, "cat");
        SetupCandidates(new SenseReviewCandidate(hidden, null), new SenseReviewCandidate(visible, null));

        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([hidden.Id, visible.Id], _callerId, AgeGroup.Child));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.SenseId.Should().Be(visible.Id);
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_AdultCaller_GetsNonChildVisibleSharedSense()
    {
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false, "secret");
        SetupCandidates(new SenseReviewCandidate(shared, null));

        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([shared.Id], _callerId, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.SenseId.Should().Be(shared.Id);
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_UnknownId_IsOmitted()
    {
        Sense known = TestWords.System();
        SetupCandidates(new SenseReviewCandidate(known, null));

        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([known.Id, Guid.NewGuid()], _callerId, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.SenseId.Should().Be(known.Id);
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_OtherLearnersPrivateSense_IsOmitted()
    {
        Sense foreign = TestWords.Learner(Guid.NewGuid(), "private");
        Sense own = TestWords.Learner(_callerId, "mine");
        SetupCandidates(new SenseReviewCandidate(foreign, null), new SenseReviewCandidate(own, null));

        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([foreign.Id, own.Id], _callerId, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle().Which.SenseId.Should().Be(own.Id);
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_OwnLink_ReturnsPersonalContext()
    {
        Sense sense = TestWords.System();
        SetupCandidates(new SenseReviewCandidate(sense, LinkWithContext(_callerId, sense, "at the zoo")));

        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([sense.Id], _callerId, AgeGroup.Adult));

        result.Value.Should().ContainSingle().Which.PersonalContext.Should().Be("at the zoo");
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_OtherUsersLink_ReturnsNoPersonalContext()
    {
        Sense sense = TestWords.System();
        SetupCandidates(new SenseReviewCandidate(sense, LinkWithContext(Guid.NewGuid(), sense, "not yours")));

        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([sense.Id], _callerId, AgeGroup.Adult));

        result.Value.Should().ContainSingle().Which.PersonalContext.Should().BeNull();
    }

    [Fact]
    public async Task GetSensesByIdsQueryHandler_HandleAsync_InvalidIds_ReturnsValidationFailure()
    {
        Result<IReadOnlyList<SenseReviewDto>> result = await CreateHandler().HandleAsync(
            new GetSensesByIdsQuery([], _callerId, AgeGroup.Adult));

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(
            r => r.GetForReviewAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
