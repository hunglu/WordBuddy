using FluentAssertions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;
using WordBuddy.Content.Application.Features.Autofill.Commands.ApproveChildWord;
using WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Autofill;

public class AddAutofillSenseToMyListCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private LearnerWord? _linked;

    public AddAutofillSenseToMyListCommandHandlerTests()
    {
        _repository
            .Setup(r => r.GetLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "no")));
        _repository
            .Setup(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()))
            .Callback((LearnerWord link, CancellationToken _) => _linked = link)
            .ReturnsAsync(Result.Success());
    }

    private AddAutofillSenseToMyListCommandHandler CreateHandler() =>
        new(_repository.Object, new AddAutofillSenseToMyListCommandValidator(), Mock.Of<ILogger<AddAutofillSenseToMyListCommandHandler>>());

    private Sense Stored(Sense sense)
    {
        _repository.Setup(r => r.GetByIdAsync(sense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(sense));
        return sense;
    }

    [Fact]
    public async Task AddAutofillSenseToMyListCommandHandler_HandleAsync_Adult_LinksWithoutApproval()
    {
        Sense sense = Stored(AutofillTestData.CatalogSense());

        Result<Guid> result = await CreateHandler().HandleAsync(new AddAutofillSenseToMyListCommand(sense.Id, Guid.NewGuid(), AgeGroup.Adult));

        result.Value.Should().Be(sense.Id);
        _linked!.AddedBy.Should().Be(LearnerWordAddedBy.Learner);
        _linked.RequiresChildApproval.Should().BeFalse();
    }

    [Fact]
    public async Task AddAutofillSenseToMyListCommandHandler_HandleAsync_Child_LinkWaitsForApproval()
    {
        Sense sense = Stored(AutofillTestData.CatalogSense());

        await CreateHandler().HandleAsync(new AddAutofillSenseToMyListCommand(sense.Id, Guid.NewGuid(), AgeGroup.Child));

        _linked!.RequiresChildApproval.Should().BeTrue();
    }

    [Fact]
    public async Task AddAutofillSenseToMyListCommandHandler_HandleAsync_AlreadyLinked_IsIdempotent()
    {
        Guid userId = Guid.NewGuid();
        Sense sense = Stored(AutofillTestData.CatalogSense());
        _repository
            .Setup(r => r.GetLinkAsync(userId, sense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LearnerWord(Guid.NewGuid(), userId, sense, isAuthor: false)));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddAutofillSenseToMyListCommand(sense.Id, userId, AgeGroup.Adult));

        result.Value.Should().Be(sense.Id);
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddAutofillSenseToMyListCommandHandler_HandleAsync_ManualSense_ReturnsNotFound()
    {
        Sense sense = Stored(TestWords.System());

        Result<Guid> result = await CreateHandler().HandleAsync(new AddAutofillSenseToMyListCommand(sense.Id, Guid.NewGuid(), AgeGroup.Adult));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }
}

public class ApproveChildWordCommandHandlerTests
{
    private readonly Mock<IAutofillRepository> _repository = new();
    private readonly Mock<IDistributedCache> _cache = new();

    public ApproveChildWordCommandHandlerTests()
    {
        _repository.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
    }

    private ApproveChildWordCommandHandler CreateHandler() =>
        new(_repository.Object, _cache.Object, new ApproveChildWordCommandValidator(), Mock.Of<ILogger<ApproveChildWordCommandHandler>>());

    private static LearnerWord PendingLink(Guid childId, Sense sense)
    {
        LearnerWord link = new(Guid.NewGuid(), childId, sense, isAuthor: false);
        link.RequireChildApproval();
        return link;
    }

    [Fact]
    public async Task ApproveChildWordCommandHandler_HandleAsync_Supporter_ApprovesOnlyTheLink()
    {
        Guid childId = Guid.NewGuid();
        Sense sense = AutofillTestData.CatalogSense();
        LearnerWord link = PendingLink(childId, sense);
        _repository.Setup(r => r.GetTrackedLinkAsync(childId, sense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(link));

        Result result = await CreateHandler().HandleAsync(new ApproveChildWordCommand(sense.Id, childId, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        link.ChildApprovedAtUtc.Should().NotBeNull();
        sense.VisibleToChildren.Should().BeFalse();
        _cache.Verify(c => c.RemoveAsync($"content:sense:{sense.Id}", It.IsAny<CancellationToken>()), Times.Once);
        _cache.Verify(c => c.RemoveAsync("content:autofill:APPLE", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveChildWordCommandHandler_HandleAsync_Admin_ApprovesGlobally()
    {
        Sense sense = AutofillTestData.CatalogSense();
        _repository.Setup(r => r.GetTrackedSenseAsync(sense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(sense));

        Result result = await CreateHandler().HandleAsync(new ApproveChildWordCommand(sense.Id, null, Guid.NewGuid()));

        result.IsSuccess.Should().BeTrue();
        sense.VisibleToChildren.Should().BeTrue();
        _cache.Verify(c => c.RemoveAsync("content:vocabulary-shared:True", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ApproveChildWordCommandHandler_HandleAsync_SupporterLinkNotPending_ReturnsNotFound()
    {
        Guid childId = Guid.NewGuid();
        Sense sense = AutofillTestData.CatalogSense();
        _repository
            .Setup(r => r.GetTrackedLinkAsync(childId, sense.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LearnerWord(Guid.NewGuid(), childId, sense, isAuthor: false)));

        Result result = await CreateHandler().HandleAsync(new ApproveChildWordCommand(sense.Id, childId, Guid.NewGuid()));

        result.Error.Type.Should().Be(ErrorType.NotFound);
        _repository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ApproveChildWordCommandHandler_HandleAsync_SupporterTwice_ReturnsConflict()
    {
        Guid childId = Guid.NewGuid();
        Sense sense = AutofillTestData.CatalogSense();
        LearnerWord link = PendingLink(childId, sense);
        link.ApproveForChild(Guid.NewGuid());
        _repository.Setup(r => r.GetTrackedLinkAsync(childId, sense.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(link));

        Result result = await CreateHandler().HandleAsync(new ApproveChildWordCommand(sense.Id, childId, Guid.NewGuid()));

        result.Error.Type.Should().Be(ErrorType.Conflict);
    }
}

public class GetPendingChildApprovalsQueryHandlerTests
{
    private readonly Mock<IAutofillRepository> _repository = new();

    private GetPendingChildApprovalsQueryHandler CreateHandler() =>
        new(_repository.Object, Mock.Of<ILogger<GetPendingChildApprovalsQueryHandler>>());

    [Fact]
    public async Task GetPendingChildApprovalsQueryHandler_HandleAsync_Supporter_ListsLearnerLinksWithHint()
    {
        Guid childId = Guid.NewGuid();
        Sense sense = AutofillTestData.CatalogSense();
        LearnerWord link = new(Guid.NewGuid(), childId, sense, isAuthor: false);
        _repository
            .Setup(r => r.GetPendingLinksForLearnerAsync(childId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<LearnerWord>>([link]));

        Result<IReadOnlyList<ChildApprovalDto>> result = await CreateHandler().HandleAsync(new GetPendingChildApprovalsQuery(childId));

        ChildApprovalDto item = result.Value.Should().ContainSingle().Subject;
        item.SenseId.Should().Be(sense.Id);
        item.LearnerId.Should().Be(childId);
        item.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        _repository.Verify(r => r.GetPendingSensesForAdminAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetPendingChildApprovalsQueryHandler_HandleAsync_Admin_ListsSensesWithoutLearner()
    {
        Sense sense = AutofillTestData.CatalogSense();
        _repository
            .Setup(r => r.GetPendingSensesForAdminAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([sense]));

        Result<IReadOnlyList<ChildApprovalDto>> result = await CreateHandler().HandleAsync(new GetPendingChildApprovalsQuery(null));

        result.Value.Should().ContainSingle(i => i.SenseId == sense.Id && i.LearnerId == null);
    }
}
