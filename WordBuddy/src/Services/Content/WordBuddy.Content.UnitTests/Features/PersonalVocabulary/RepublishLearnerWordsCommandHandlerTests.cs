using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class RepublishLearnerWordsCommandHandlerTests
{
    private static readonly DateTime AddedAt = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly Mock<ILearnerWordEventPublisher> _publisher = new();
    private readonly List<IReadOnlyList<LearnerWordLink>> _batches = [];

    public RepublishLearnerWordsCommandHandlerTests()
    {
        _publisher
            .Setup(p => p.PublishAddedAsync(It.IsAny<IReadOnlyList<LearnerWordLink>>(), It.IsAny<CancellationToken>()))
            .Callback<IReadOnlyList<LearnerWordLink>, CancellationToken>((links, _) => _batches.Add(links))
            .ReturnsAsync((IReadOnlyList<LearnerWordLink> links, CancellationToken _) => Result.Success(links.Count));
    }

    private RepublishLearnerWordsCommandHandler CreateHandler() =>
        new(_repository.Object, _publisher.Object, new RepublishLearnerWordsCommandValidator(),
            Mock.Of<ILogger<RepublishLearnerWordsCommandHandler>>());

    private static List<LearnerWordLink> Links(int count) =>
        Enumerable.Range(0, count).Select(_ => new LearnerWordLink(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), AddedAt)).ToList();

    private void GivenLinks(List<LearnerWordLink> all) =>
        _repository
            .Setup(r => r.GetLearnerLinksPageAsync(It.IsAny<Guid?>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid? afterLinkId, int take, CancellationToken _) =>
                Result.Success<IReadOnlyList<LearnerWordLink>>(
                    all.SkipWhile(l => afterLinkId is not null && l.LinkId != afterLinkId).Skip(afterLinkId is null ? 0 : 1).Take(take).ToList()));

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_PublishesInBatchesAndReturnsCount()
    {
        List<LearnerWordLink> all = Links(5);
        GivenLinks(all);

        Result<RepublishLearnerWordsResult> result = await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand(BatchSize: 2));

        result.IsSuccess.Should().BeTrue();
        result.Value.Published.Should().Be(5);
        _batches.Select(b => b.Count).Should().Equal(2, 2, 1);
        _batches.SelectMany(b => b).Should().Equal(all);
    }

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_KeepsOriginalAddedAtUtc()
    {
        GivenLinks(Links(1));

        await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand());

        _batches.Should().ContainSingle().Which.Should().OnlyContain(l => l.AddedAtUtc == AddedAt);
    }

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_SkipsSystemOwner()
    {
        List<LearnerWordLink> all = Links(2);
        all.Add(new LearnerWordLink(Guid.NewGuid(), SystemOwner.UserId, Guid.NewGuid(), AddedAt));
        GivenLinks(all);

        Result<RepublishLearnerWordsResult> result = await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand());

        result.Value.Published.Should().Be(2);
        _batches.SelectMany(b => b).Should().NotContain(l => l.UserId == SystemOwner.UserId);
    }

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_NoLinksPublishesNothing()
    {
        GivenLinks([]);

        Result<RepublishLearnerWordsResult> result = await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand());

        result.Value.Published.Should().Be(0);
        _publisher.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_DefaultBatchSizeIs500()
    {
        GivenLinks(Links(501));

        Result<RepublishLearnerWordsResult> result = await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand());

        result.Value.Published.Should().Be(501);
        _batches.Select(b => b.Count).Should().Equal(500, 1);
    }

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_PublisherFailureStops()
    {
        GivenLinks(Links(3));
        Error error = Error.Failure("Outbox.Failed", "failed");
        _publisher
            .Setup(p => p.PublishAddedAsync(It.IsAny<IReadOnlyList<LearnerWordLink>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<int>(error));

        Result<RepublishLearnerWordsResult> result = await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand());

        result.Error.Should().Be(error);
    }

    [Fact]
    public async Task RepublishLearnerWordsCommandHandler_HandleAsync_InvalidBatchSizeFailsValidation()
    {
        Result<RepublishLearnerWordsResult> result = await CreateHandler().HandleAsync(new RepublishLearnerWordsCommand(BatchSize: 0));

        result.Error.Code.Should().Be("RepublishLearnerWords.Validation");
        _repository.VerifyNoOtherCalls();
    }
}
