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
    private readonly Lexeme _lexeme = Lexeme.Create(Guid.NewGuid(), "apple").Value;

    public AddPersonalVocabularyWordCommandHandlerTests()
    {
        _repository
            .Setup(r => r.GetOrCreateLexemeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result.Success(_lexeme));
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Sense>(), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Sense word, LearnerWord _, CancellationToken _) => Result.Success(word.Id));
        _repository
            .Setup(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());
        _repository
            .Setup(r => r.GetLinkAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWord>(Error.NotFound("PersonalVocabularyWord.NotFound", "not found")));
    }

    private AddPersonalVocabularyWordCommandHandler CreateHandler() =>
        new(_repository.Object, _validator, _logger.Object);

    private void SetupCandidates(params Sense[] candidates) =>
        _repository
            .Setup(r => r.FindByContentHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>(candidates));

    /// <summary>No new sense and no lexeme lookup: dedupe paths never touch lexemes.</summary>
    private void VerifyNoNewWord()
    {
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Sense>(), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _repository.Verify(
            r => r.GetOrCreateLexemeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_NoDuplicate_CreatesSenseWithLexemeFromRepository()
    {
        SetupCandidates();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Adult, " apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(r => r.GetOrCreateLexemeAsync(" apple", It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(
            r => r.AddAsync(It.Is<Sense>(w => w.LexemeId == _lexeme.Id), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_LexemeFailure_ReturnsErrorWithoutAdding()
    {
        SetupCandidates();
        Error error = Error.Conflict("PersonalVocabularyWord.ConcurrentAdd", "try again");
        _repository
            .Setup(r => r.GetOrCreateLexemeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Lexeme>(error));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(error);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Sense>(), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_NoDuplicate_CreatesLearnerWordWithAuthorLink()
    {
        SetupCandidates();
        Guid ownerId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(ownerId, AgeGroup.Adult, "apple", "a fruit", "I ate an apple."));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(
            r => r.AddAsync(
                It.Is<Sense>(w =>
                    w.Id == result.Value && w.Word == "apple" && w.Source == VocabularySource.Learner &&
                    w.OwnerUserId == ownerId && w.ShareStatus == VocabularyShareStatus.Private),
                It.Is<LearnerWord>(l => l.UserId == ownerId && l.SenseId == result.Value && l.IsAuthor),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_ConcurrentIdenticalAddWon_ReturnsExistingId()
    {
        SetupCandidates();
        Guid existingId = Guid.NewGuid();
        _repository
            .Setup(r => r.AddAsync(It.IsAny<Sense>(), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(existingId));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Child, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(existingId);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_OwnDuplicateAlreadyLinked_ReturnsExistingIdWithoutWriting()
    {
        Guid ownerId = Guid.NewGuid();
        Sense own = TestWords.Learner(ownerId);
        SetupCandidates(own);
        _repository
            .Setup(r => r.GetLinkAsync(ownerId, own.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(new LearnerWord(Guid.NewGuid(), ownerId, own, isAuthor: true)));

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(ownerId, AgeGroup.Adult, "apple", "a fruit", null));

        result.Value.Should().Be(own.Id);
        VerifyNoNewWord();
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_OwnUnlinkedDuplicate_RelinksAsAuthor()
    {
        Guid ownerId = Guid.NewGuid();
        Sense ownShared = TestWords.Shared(ownerId, visibleToChildren: true);
        SetupCandidates(ownShared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(ownerId, AgeGroup.Adult, "apple", "a fruit", null));

        result.Value.Should().Be(ownShared.Id);
        VerifyNoNewWord();
        _repository.Verify(
            r => r.LinkAsync(It.Is<LearnerWord>(l => l.SenseId == ownShared.Id && l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_SystemDuplicate_LinksToSystemWordAsNonAuthor()
    {
        Sense system = TestWords.System();
        SetupCandidates(system);
        Guid childId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(childId, AgeGroup.Child, "apple", "a fruit", "I ate an apple."));

        result.Value.Should().Be(system.Id);
        VerifyNoNewWord();
        _repository.Verify(
            r => r.LinkAsync(It.Is<LearnerWord>(l => l.UserId == childId && l.SenseId == system.Id && !l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_ChildAndChildVisibleSharedDuplicate_LinksToSharedWord()
    {
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true);
        SetupCandidates(shared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Child, "apple", "a fruit", null));

        result.Value.Should().Be(shared.Id);
        VerifyNoNewWord();
        _repository.Verify(
            r => r.LinkAsync(It.Is<LearnerWord>(l => l.SenseId == shared.Id && !l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_ChildAndNonChildVisibleSharedDuplicate_CreatesOwnWord()
    {
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);
        SetupCandidates(shared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Child, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(shared.Id);
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Sense>(), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_AdultAndNonChildVisibleSharedDuplicate_LinksToSharedWord()
    {
        Sense shared = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false);
        SetupCandidates(shared);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null));

        result.Value.Should().Be(shared.Id);
        VerifyNoNewWord();
    }

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_OtherLearnersPrivateDuplicate_CreatesOwnWordWithoutLinking()
    {
        Sense othersPrivate = TestWords.Learner(Guid.NewGuid());
        Sense othersPending = TestWords.Pending(Guid.NewGuid());
        SetupCandidates(othersPrivate, othersPending);

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(Guid.NewGuid(), AgeGroup.Adult, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(othersPrivate.Id).And.NotBe(othersPending.Id);
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Sense>(), It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()),
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

    [Fact]
    public async Task AddPersonalVocabularyWordCommandHandler_HandleAsync_SkipsInvisibleSystemDuplicateForChild()
    {
        Sense transferred = TestWords.Transferred(visibleToChildren: false);
        SetupCandidates(transferred);
        Guid childId = Guid.NewGuid();

        Result<Guid> result = await CreateHandler().HandleAsync(new AddPersonalVocabularyWordCommand(childId, AgeGroup.Child, "apple", "a fruit", null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(transferred.Id);
        _repository.Verify(r => r.LinkAsync(It.IsAny<LearnerWord>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(
            r => r.AddAsync(It.IsAny<Sense>(), It.Is<LearnerWord>(l => l.UserId == childId && l.IsAuthor), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
