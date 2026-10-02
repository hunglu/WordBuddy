using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.PersonalVocabulary;

public class AddPersonalVocabularyWordCommandHandlerTests
{
    private readonly Mock<IVocabularyWordRepository> _repository = new();
    private readonly AddPersonalVocabularyWordCommandValidator _validator = new();
    private readonly Mock<ILogger<AddPersonalVocabularyWordCommandHandler>> _logger = new();

    public AddPersonalVocabularyWordCommandHandlerTests()
    {
        _repository
            .Setup(r => r.AddAsync(It.IsAny<VocabularyWord>(), It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _repository
            .Setup(r => r.LinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _repository
            .Setup(r => r.GetLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<UserVocabularyWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));
    }

    private AddPersonalVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    private void SetupCandidates(params VocabularyWord[] candidates) =>
        _repository
            .Setup(r => r.FindByContentHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<VocabularyWord>>(candidates));

    private void VerifyNoNewWord() =>
        _repository.Verify(
            r => r.AddAsync(It.IsAny<VocabularyWord>(), It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()),
            Times.Never);

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_NoDuplicate_CreatesLearnerWordWithAuthorLink()
    {
        SetupCandidates();
        Guid ownerId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(ownerId, AgeGroup.Adult, "apple", "a fruit", "I ate an apple."));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.AddAsync(
                It.Is<VocabularyWord>(w =>
                    w.Id == result.Value && w.Word == "apple" && w.Source == VocabularySource.Learner &&
                    w.OwnerUserId == ownerId && w.ShareStatus == VocabularyShareStatus.Private),
                It.Is<UserVocabularyWord>(l => l.UserId == ownerId && l.VocabularyWordId == result.Value && l.IsAuthor),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_OwnDuplicateAlreadyLinked_ReturnsExistingIdWithoutWriting()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord own = TestWords.Learner(ownerId);
        SetupCandidates(own);
        _repository
            .Setup(r => r.GetLinkAsync(ownerId, own.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new UserVocabularyWord(Guid.NewGuid(), ownerId, own, isAuthor: true)));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(ownerId, AgeGroup.Adult, "apple", "a fruit", null));

        result.Value.Should().Be(own.Id);
        VerifyNoNewWord();
        _repository.Verify(r => r.LinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_OwnUnlinkedDuplicate_RelinksAsAuthor()
    {
        Guid ownerId = Guid.NewGuid();
        VocabularyWord ownShared = TestWords.Shared(ownerId, visibleToChildren: true);
        SetupCandidates(ownShared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(ownerId, AgeGroup.Adult, "apple", "a fruit", null));

        result.Value.Should().Be(ownShared.Id);
        VerifyNoNewWord();
        _repository.Verify(
            r => r.LinkAsync(It.Is<UserVocabularyWord>(l => l.VocabularyWordId == ownShared.Id && l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_SystemDuplicate_LinksToSystemWordAsNonAuthor()
    {
        VocabularyWord system = TestWords.System();
        SetupCandidates(system);
        Guid childId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(childId, AgeGroup.Child, "apple", "a fruit", "I ate an apple."));

        result.Value.Should().Be(system.Id);
        VerifyNoNewWord();
        _repository.Verify(
            r => r.LinkAsync(It.Is<UserVocabularyWord>(l => l.UserId == childId && l.VocabularyWordId == system.Id && !l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_ChildAndChildVisibleSharedDuplicate_LinksToSharedWord()
    {
        VocabularyWord shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupCandidates(shared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Child, "apple", "a fruit", null));

        result.Value.Should().Be(shared.Id);
        VerifyNoNewWord();
        _repository.Verify(
            r => r.LinkAsync(It.Is<UserVocabularyWord>(l => l.VocabularyWordId == shared.Id && !l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_ChildAndNonChildVisibleSharedDuplicate_CreatesOwnWord()
    {
        VocabularyWord shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);
        SetupCandidates(shared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Child, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(shared.Id);
        _repository.Verify(r => r.LinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<VocabularyWord>(), It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_AdultAndNonChildVisibleSharedDuplicate_LinksToSharedWord()
    {
        VocabularyWord shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);
        SetupCandidates(shared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null));

        result.Value.Should().Be(shared.Id);
        VerifyNoNewWord();
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_OtherLearnersPrivateDuplicate_CreatesOwnWordWithoutLinking()
    {
        VocabularyWord othersPrivate = TestWords.Learner(Guid.NewGuid());
        VocabularyWord othersPending = TestWords.Pending(Guid.NewGuid());
        SetupCandidates(othersPrivate, othersPending);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(othersPrivate.Id).And.NotBe(othersPending.Id);
        _repository.Verify(r => r.LinkAsync(It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<VocabularyWord>(), It.IsAny<UserVocabularyWord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_EmptyWord_ReturnsValidationFailureWithoutCallingRepository()
    {
        AddPersonalVocabularyWordCommand command = new(Guid.NewGuid(), AgeGroup.Adult, string.Empty, "a fruit", null);

        Result<Guid> result = await CreateHandler().HandleAsync(command);

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Validation);
        _repository.Verify(r => r.FindByContentHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        VerifyNoNewWord();
    }
}
