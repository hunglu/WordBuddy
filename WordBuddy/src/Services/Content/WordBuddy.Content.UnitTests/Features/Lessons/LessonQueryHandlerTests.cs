using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Content.Application.Features.Lessons.Queries.SearchLessonSenses;
using WordBuddy.Content.Application.Interfaces;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Features.Lessons;

/// <summary>Lesson read handlers (WB-47): admin list / detail / sense search, and the child
/// versus adult rules of the learner list and detail.</summary>
public class LessonQueryHandlerTests
{
    private readonly Mock<ILessonRepository> _lessons = new();
    private readonly Mock<IVocabularyWordRepository> _senses = new();

    private static Lesson NewLesson(AgeGroup target, bool isPublished, params Sense[] senses)
    {
        Lesson lesson = new(Guid.NewGuid(), "Animals", "Common animals.", LessonType.Vocabulary, Level.Beginner, target, isPublished);
        foreach (Sense sense in senses)
        {
            lesson.AttachSense(sense);
        }

        return lesson;
    }

    private void SetupDetail(Lesson lesson) =>
        _lessons.Setup(r => r.GetByIdWithDetailsAsync(lesson.Id, It.IsAny<CancellationToken>())).ReturnsAsync(Result.Success(lesson));

    // ---- Admin list

    [Fact]
    public async Task GetAdminLessonsQueryHandler_HandleAsync_ReturnsDraftsAndPublishedWithSenseCount()
    {
        Lesson draft = NewLesson(AgeGroup.Child, isPublished: false, TestWords.System("Dog"), TestWords.System("Cat"));
        Lesson published = NewLesson(AgeGroup.Adult, isPublished: true);
        _lessons.Setup(r => r.ListAllAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Lesson>>([draft, published]));

        Result<IReadOnlyList<AdminLessonDto>> result = await new GetAdminLessonsQueryHandler(
            _lessons.Object, NullLogger<GetAdminLessonsQueryHandler>.Instance)
            .HandleAsync(new GetAdminLessonsQuery(null, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Single(l => l.Id == draft.Id).IsPublished.Should().BeFalse();
        result.Value.Single(l => l.Id == draft.Id).SenseCount.Should().Be(2);
        result.Value.Single(l => l.Id == published.Id).IsPublished.Should().BeTrue();
    }

    [Fact]
    public async Task GetAdminLessonsQueryHandler_HandleAsync_PassesFiltersToRepository()
    {
        _lessons.Setup(r => r.ListAllAsync(LessonType.Grammar, Level.Advanced, false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Lesson>>([]));

        Result<IReadOnlyList<AdminLessonDto>> result = await new GetAdminLessonsQueryHandler(
            _lessons.Object, NullLogger<GetAdminLessonsQueryHandler>.Instance)
            .HandleAsync(new GetAdminLessonsQuery(LessonType.Grammar, Level.Advanced, false));

        result.IsSuccess.Should().BeTrue();
        _lessons.Verify(r => r.ListAllAsync(LessonType.Grammar, Level.Advanced, false, It.IsAny<CancellationToken>()), Times.Once);
    }

    // ---- Admin detail

    [Fact]
    public async Task GetAdminLessonDetailQueryHandler_HandleAsync_DraftWithHiddenSense_ReturnsAllSensesWithMetadata()
    {
        Sense system = TestWords.System("Dog");
        Sense hidden = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false, word: "beer");
        Lesson lesson = NewLesson(AgeGroup.Child, isPublished: false, system, hidden);
        SetupDetail(lesson);

        Result<AdminLessonDetailDto> result = await new GetAdminLessonDetailQueryHandler(
            _lessons.Object, NullLogger<GetAdminLessonDetailQueryHandler>.Instance)
            .HandleAsync(new GetAdminLessonDetailQuery(lesson.Id));

        result.IsSuccess.Should().BeTrue();
        result.Value.IsPublished.Should().BeFalse();
        result.Value.Senses.Select(s => s.SortOrder).Should().Equal(0, 1);
        result.Value.Senses[0].VisibleToChildren.Should().BeTrue();
        result.Value.Senses[0].Source.Should().Be(VocabularySource.System);
        result.Value.Senses[1].VisibleToChildren.Should().BeFalse();
        result.Value.Senses[1].ShareStatus.Should().Be(VocabularyShareStatus.Shared);
    }

    [Fact]
    public async Task GetAdminLessonDetailQueryHandler_HandleAsync_UnknownLesson_ReturnsNotFound()
    {
        Guid id = Guid.NewGuid();
        _lessons.Setup(r => r.GetByIdWithDetailsAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure<Lesson>(Error.NotFound("Lesson.NotFound", "missing")));

        Result<AdminLessonDetailDto> result = await new GetAdminLessonDetailQueryHandler(
            _lessons.Object, NullLogger<GetAdminLessonDetailQueryHandler>.Instance)
            .HandleAsync(new GetAdminLessonDetailQuery(id));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    // ---- Sense search

    [Fact]
    public async Task SearchLessonSensesQueryHandler_HandleAsync_ReturnsAttachableSensesWithChildFlag()
    {
        Sense system = TestWords.System("Dog");
        Sense hidden = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false, word: "beer");
        _senses.Setup(r => r.SearchAttachableAsync("d", SearchLessonSensesQueryHandler.MaxResults, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Sense>>([system, hidden]));

        Result<IReadOnlyList<AdminSenseSearchResultDto>> result = await new SearchLessonSensesQueryHandler(
            _senses.Object, new SearchLessonSensesQueryValidator(), NullLogger<SearchLessonSensesQueryHandler>.Instance)
            .HandleAsync(new SearchLessonSensesQuery(" d "));

        result.IsSuccess.Should().BeTrue();
        result.Value.Select(s => s.VisibleToChildren).Should().Equal(true, false);
    }

    [Fact]
    public async Task SearchLessonSensesQueryHandler_HandleAsync_TooLongSearch_ReturnsValidationError()
    {
        Result<IReadOnlyList<AdminSenseSearchResultDto>> result = await new SearchLessonSensesQueryHandler(
            _senses.Object, new SearchLessonSensesQueryValidator(), NullLogger<SearchLessonSensesQueryHandler>.Instance)
            .HandleAsync(new SearchLessonSensesQuery(new string('a', 101)));

        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    // ---- Learner detail: child versus adult

    private GetLessonDetailQueryHandler DetailHandler() =>
        new(_lessons.Object, NullLogger<GetLessonDetailQueryHandler>.Instance);

    [Fact]
    public async Task GetLessonDetailQueryHandler_HandleAsync_ChildOnHiddenSharedSense_DoesNotSeeIt()
    {
        Sense system = TestWords.System("Dog");
        Sense hidden = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false, word: "beer");
        Sense safe = TestWords.Shared(Guid.NewGuid(), visibleToChildren: true, word: "milk");
        Lesson lesson = NewLesson(AgeGroup.Child, isPublished: true, system, hidden, safe);
        SetupDetail(lesson);

        Result<LessonDetailDto> result = await DetailHandler().HandleAsync(new GetLessonDetailQuery(lesson.Id, AgeGroup.Child));

        result.IsSuccess.Should().BeTrue();
        result.Value.VocabularyItems.Select(v => v.Word).Should().Equal("Dog", "milk");
    }

    [Fact]
    public async Task GetLessonDetailQueryHandler_HandleAsync_AdultOnHiddenSharedSense_SeesAll()
    {
        Sense hidden = TestWords.Shared(Guid.NewGuid(), visibleToChildren: false, word: "beer");
        Lesson lesson = NewLesson(AgeGroup.Child, isPublished: true, TestWords.System("Dog"), hidden);
        SetupDetail(lesson);

        Result<LessonDetailDto> result = await DetailHandler().HandleAsync(new GetLessonDetailQuery(lesson.Id, AgeGroup.Adult));

        result.Value.VocabularyItems.Select(v => v.Word).Should().Equal("Dog", "beer");
    }

    [Theory]
    [InlineData(AgeGroup.Child)]
    [InlineData(AgeGroup.Adult)]
    public async Task GetLessonDetailQueryHandler_HandleAsync_Draft_ReturnsNotFound(AgeGroup caller)
    {
        Lesson lesson = NewLesson(AgeGroup.Child, isPublished: false);
        SetupDetail(lesson);

        Result<LessonDetailDto> result = await DetailHandler().HandleAsync(new GetLessonDetailQuery(lesson.Id, caller));

        result.Error.Code.Should().Be("Lesson.NotFound");
    }

    [Fact]
    public async Task GetLessonDetailQueryHandler_HandleAsync_ChildOnAdultLesson_ReturnsNotFound()
    {
        Lesson lesson = NewLesson(AgeGroup.Adult, isPublished: true);
        SetupDetail(lesson);

        Result<LessonDetailDto> result = await DetailHandler().HandleAsync(new GetLessonDetailQuery(lesson.Id, AgeGroup.Child));

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task GetLessonDetailQueryHandler_HandleAsync_AdultOnAdultLesson_ReturnsLesson()
    {
        Lesson lesson = NewLesson(AgeGroup.Adult, isPublished: true);
        SetupDetail(lesson);

        Result<LessonDetailDto> result = await DetailHandler().HandleAsync(new GetLessonDetailQuery(lesson.Id, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
    }

    // ---- Learner list: child versus adult

    [Fact]
    public async Task GetLessonsQueryHandler_HandleAsync_Child_AsksForChildTargetedLessonsOnly()
    {
        _lessons.Setup(r => r.GetPublishedAsync(null, null, AgeGroup.Child, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Lesson>>([]));

        Result<IReadOnlyList<LessonDto>> result = await new GetLessonsQueryHandler(
            _lessons.Object, NullLogger<GetLessonsQueryHandler>.Instance)
            .HandleAsync(new GetLessonsQuery(null, null, AgeGroup.Child));

        result.IsSuccess.Should().BeTrue();
        _lessons.Verify(r => r.GetPublishedAsync(null, null, AgeGroup.Child, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetLessonsQueryHandler_HandleAsync_Adult_AppliesNoAgeFilter()
    {
        _lessons.Setup(r => r.GetPublishedAsync(null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success<IReadOnlyList<Lesson>>([]));

        Result<IReadOnlyList<LessonDto>> result = await new GetLessonsQueryHandler(
            _lessons.Object, NullLogger<GetLessonsQueryHandler>.Instance)
            .HandleAsync(new GetLessonsQuery(null, null, AgeGroup.Adult));

        result.IsSuccess.Should().BeTrue();
        _lessons.Verify(r => r.GetPublishedAsync(null, null, null, It.IsAny<CancellationToken>()), Times.Once);
    }
}
