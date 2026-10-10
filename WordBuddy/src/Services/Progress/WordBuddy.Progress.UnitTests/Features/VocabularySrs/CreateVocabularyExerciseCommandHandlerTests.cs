using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Application.Features.VocabularySrs.Commands.CreateVocabularyExercise;
using WordBuddy.Progress.Application.Interfaces;
using WordBuddy.Progress.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Features.VocabularySrs;

public class CreateVocabularyExerciseCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 9, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IVocabularySessionIssueRepository> _sessions = new();
    private readonly Mock<ILearnerWordStateRepository> _states = new();
    private readonly Mock<IReviewLogRepository> _reviewLogs = new();
    private readonly Mock<IVocabularyExerciseRepository> _exercises = new();
    private readonly Mock<IContentSenseClient> _content = new();
    private readonly List<VocabularyExercise> _added = [];
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly List<ContentSenseDto> _senses = [];
    private VocabularySessionIssue? _session;

    public CreateVocabularyExerciseCommandHandlerTests()
    {
        _exercises
            .Setup(r => r.AddAsync(It.IsAny<VocabularyExercise>(), It.IsAny<CancellationToken>()))
            .Callback<VocabularyExercise, CancellationToken>((e, _) => _added.Add(e))
            .ReturnsAsync(Result.Success());
        _states.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success());
        _reviewLogs
            .Setup(r => r.CountCorrectAsync(_userId, _sessionId, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(0));
    }

    private CreateVocabularyExerciseCommandHandler CreateHandler() => new(
        _sessions.Object,
        _states.Object,
        _reviewLogs.Object,
        _exercises.Object,
        _content.Object,
        new FixedTimeProvider(Now),
        new CreateVocabularyExerciseCommandValidator(),
        Mock.Of<ILogger<CreateVocabularyExerciseCommandHandler>>());

    /// <summary>Opens a session with <paramref name="count"/> words, all in the learner's list with the given status.</summary>
    private void GivenOpenSession(int count, bool withMedia = true, WordStatus status = WordStatus.New)
    {
        List<(Guid SenseId, bool IsNew)> items = [];
        for (int i = 0; i < count; i++)
        {
            Guid senseId = Guid.NewGuid();
            items.Add((senseId, true));
            _senses.Add(new ContentSenseDto(
                senseId, $"word{i}", $"meaning {i}", withMedia ? $"https://img/{i}.png" : null, withMedia ? $"https://aud/{i}.mp3" : null, null));

            LearnerWordState state = LearnerWordState.CreateNew(Guid.NewGuid(), _userId, senseId, Now.AddDays(-1));
            if (status != WordStatus.New)
            {
                VocabularySchedulingOptions options = new();
                for (int r = 0; r < 6; r++)
                {
                    state.ApplyReview(FsrsRating.Easy, Now.AddDays(-60 + r), new FsrsScheduler(options), options);
                }
            }

            _states.Setup(s => s.GetTrackedAsync(_userId, senseId, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(state));
        }

        _session = VocabularySessionIssue.Create(_sessionId, _userId, Now.AddMinutes(-1), items, 30, Now.AddHours(8));
        _sessions.Setup(r => r.GetOpenAsync(_userId, Now, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(_session));
        _content
            .Setup(c => c.GetSessionSensesAsync(_sessionId, It.IsAny<IReadOnlyCollection<Guid>>(), _session.ExpiresAtUtc, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => Result.Success<IReadOnlyList<ContentSenseDto>>(_senses.ToList()));
    }

    private CreateVocabularyExerciseCommand Command(int index = 0) => new(_userId, _sessionId, _senses[index].SenseId);

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_NewWordWithFourSensesGetsPictureChoice()
    {
        GivenOpenSession(5);

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(Command());

        result.IsSuccess.Should().BeTrue();
        VocabularyExerciseDto dto = result.Value;
        dto.ExerciseType.Should().Be(ExerciseType.PictureChoice);
        dto.Skill.Should().Be(VocabularySkill.Meaning);
        dto.Prompt.ImageUrl.Should().Be("https://img/0.png");
        dto.Prompt.AudioUrl.Should().BeNull();
        dto.Options.Should().HaveCount(4);
        dto.Options.Select(o => o.Text).Should().OnlyHaveUniqueItems().And.Contain("word0");
        _added.Should().ContainSingle();
        _states.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_StoresExpectedKeyOfTheRightOption()
    {
        GivenOpenSession(5);

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        VocabularyExercise stored = _added.Single();
        stored.ExerciseId.Should().Be(dto.ExerciseId);
        stored.IssuedAtUtc.Should().Be(Now);
        stored.Options[stored.ExpectedAnswer].Should().Be(_senses[0].SenseId);
        dto.Options.Single(o => o.Key == stored.ExpectedAnswer).Text.Should().Be("word0");
        stored.CorrectWord.Should().Be("word0");
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_ReplyNeverExposesSenseIdsOrTheAnswer()
    {
        GivenOpenSession(5);

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        string json = JsonSerializer.Serialize(dto);
        foreach (ContentSenseDto sense in _senses)
        {
            json.Should().NotContain(sense.SenseId.ToString());
        }

        VocabularyExercise stored = _added.Single();
        dto.Options.Select(o => o.Key).Should().BeEquivalentTo(stored.Options.Keys);
        dto.Options.Select(o => o.Key).Should().NotContain(_senses.Select(s => s.SenseId.ToString()));
        json.Should().NotContain("ExpectedAnswer");
        json.Should().NotContain("IsCorrect", because: "the correct flag is not part of the prompt");
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_TypingPromptHasNoWordOnlyFirstLetter()
    {
        GivenOpenSession(5, status: WordStatus.Review);

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        dto.ExerciseType.Should().Be(ExerciseType.Typing);
        dto.Skill.Should().Be(VocabularySkill.Spelling);
        dto.Options.Should().BeEmpty();
        dto.Prompt.Definition.Should().Be("meaning 0");
        dto.Prompt.HintFirstLetter.Should().Be("w");
        JsonSerializer.Serialize(dto).Should().NotContain("word0");
        _added.Single().ExpectedAnswer.Should().Be("word0");
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_FewerThanFourSensesGetsTyping()
    {
        GivenOpenSession(3);

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        dto.ExerciseType.Should().Be(ExerciseType.Typing);
        dto.Options.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_TooFewDifferentWordsFallsBackToTyping()
    {
        GivenOpenSession(5);
        // Three other senses share the word of another one: only 2 different distractor words remain.
        _senses[2] = _senses[2] with { Word = "word1" };
        _senses[3] = _senses[3] with { Word = "WORD1" };

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        dto.ExerciseType.Should().Be(ExerciseType.Typing);
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_NoImageGetsListeningChoice()
    {
        GivenOpenSession(5);
        _senses[0] = _senses[0] with { ImageUrl = null };

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        dto.ExerciseType.Should().Be(ExerciseType.ListeningChoice);
        dto.Prompt.AudioUrl.Should().Be("https://aud/0.mp3");
        dto.Prompt.ImageUrl.Should().BeNull();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_AfterOneCorrectAnswerGetsRecallLevel()
    {
        GivenOpenSession(5);
        _reviewLogs
            .Setup(r => r.CountCorrectAsync(_userId, _sessionId, _senses[0].SenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(1));

        VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;

        dto.ExerciseType.Should().Be(ExerciseType.ListeningChoice);
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_WordDoneForSessionReturnsConflict()
    {
        GivenOpenSession(5);
        _reviewLogs
            .Setup(r => r.CountCorrectAsync(_userId, _sessionId, _senses[0].SenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success(2));

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Exercise.SenseCompleted");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_NoOpenSessionIsNotFound()
    {
        GivenOpenSession(5);
        _sessions
            .Setup(r => r.GetOpenAsync(_userId, Now, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<VocabularySessionIssue>(Error.NotFound("VocabularySession.NoOpenSession", "none")));

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Exercise.SessionNotOpen");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _content.VerifyNoOtherCalls();
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_ForeignOrOldSessionIdIsNotFound()
    {
        GivenOpenSession(5);

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(
            new CreateVocabularyExerciseCommand(_userId, Guid.NewGuid(), _senses[0].SenseId));

        result.Error.Code.Should().Be("Exercise.SessionNotOpen");
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_SenseNotInSessionIsNotFound()
    {
        GivenOpenSession(5);

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(
            new CreateVocabularyExerciseCommand(_userId, _sessionId, Guid.NewGuid()));

        result.Error.Code.Should().Be("Exercise.SenseNotInSession");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_SenseMissingInContentIsNotFound()
    {
        GivenOpenSession(5);
        ContentSenseDto hidden = _senses[0];
        _senses.RemoveAt(0);

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(
            new CreateVocabularyExerciseCommand(_userId, _sessionId, hidden.SenseId));

        result.Error.Code.Should().Be("Exercise.SenseUnavailable");
        result.Error.Type.Should().Be(ErrorType.NotFound);
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_HiddenSensesAreNeverDistractors()
    {
        GivenOpenSession(6);
        // Content omitted the last sense (hidden for a child): it must not appear as an option.
        ContentSenseDto hidden = _senses[5];
        _senses.RemoveAt(5);

        for (int i = 0; i < 20; i++)
        {
            VocabularyExerciseDto dto = (await CreateHandler().HandleAsync(Command())).Value;
            dto.Options.Select(o => o.Text).Should().NotContain(hidden.Word);
        }
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_ContentDownReturnsUnavailable()
    {
        GivenOpenSession(5);
        _content
            .Setup(c => c.GetSessionSensesAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<IReadOnlyList<ContentSenseDto>>(Error.Failure("Content.Unavailable", "down")));

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Content.Unavailable");
        _added.Should().BeEmpty();
        _states.Verify(s => s.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_WordNotInListIsNotFound()
    {
        GivenOpenSession(5);
        _states
            .Setup(s => s.GetTrackedAsync(_userId, _senses[0].SenseId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<LearnerWordState>(Error.NotFound("LearnerWordState.NotFound", "none")));

        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(Command());

        result.Error.Code.Should().Be("Review.WordNotInList");
        _added.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateVocabularyExerciseCommandHandler_HandleAsync_EmptyIdsFailValidation()
    {
        Result<VocabularyExerciseDto> result = await CreateHandler().HandleAsync(
            new CreateVocabularyExerciseCommand(_userId, Guid.Empty, Guid.Empty));

        result.Error.Code.Should().Be("CreateVocabularyExercise.Validation");
        _sessions.VerifyNoOtherCalls();
    }
}
