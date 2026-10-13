using FluentAssertions;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Domain;

public class LessonTests
{
    private static Lesson NewLesson(LessonType type = LessonType.Vocabulary, bool isPublished = true) =>
        new(Guid.NewGuid(), "Animals", "Common animals.", type, Level.Beginner, AgeGroup.Child, isPublished);

    [Fact]
    public void Lesson_AddVocabularyWord_CreatesLessonSenseLinksInSortOrder()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        Sense bird = TestWords.System("Bird");

        lesson.AddVocabularyWord(dog);
        lesson.AddVocabularyWord(cat);
        lesson.AddVocabularyWord(bird);

        lesson.VocabularyWords.Select(l => l.SortOrder).Should().Equal(0, 1, 2);
        lesson.VocabularyWords.Select(l => l.SenseId).Should().Equal(dog.Id, cat.Id, bird.Id);
        lesson.VocabularyWords.Should().AllSatisfy(l => l.LessonId.Should().Be(lesson.Id));
        lesson.VocabularyWords[0].Sense.Should().BeSameAs(dog);
    }

    [Fact]
    public void Lesson_UpdateDetails_ChangesEditableFieldsAndKeepsType()
    {
        Lesson lesson = NewLesson();

        Result result = lesson.UpdateDetails("Pets", "Pets at home.", Level.Intermediate, AgeGroup.Adult);

        result.IsSuccess.Should().BeTrue();
        lesson.Title.Should().Be("Pets");
        lesson.Description.Should().Be("Pets at home.");
        lesson.Level.Should().Be(Level.Intermediate);
        lesson.TargetAgeGroup.Should().Be(AgeGroup.Adult);
        lesson.Type.Should().Be(LessonType.Vocabulary);
    }

    [Fact]
    public void Lesson_PublishAndUnpublish_AreIdempotent()
    {
        Lesson lesson = NewLesson(isPublished: false);

        lesson.Publish().IsSuccess.Should().BeTrue();
        lesson.Publish().IsSuccess.Should().BeTrue();
        lesson.IsPublished.Should().BeTrue();

        lesson.Unpublish().IsSuccess.Should().BeTrue();
        lesson.Unpublish().IsSuccess.Should().BeTrue();
        lesson.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Lesson_AttachSense_AppendsAtEnd()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");

        lesson.AttachSense(dog).IsSuccess.Should().BeTrue();
        lesson.AttachSense(cat).IsSuccess.Should().BeTrue();

        lesson.VocabularyWords.Select(l => l.SenseId).Should().Equal(dog.Id, cat.Id);
        lesson.VocabularyWords.Select(l => l.SortOrder).Should().Equal(0, 1);
    }

    [Fact]
    public void Lesson_AttachSense_Duplicate_FailsWithConflict()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        lesson.AttachSense(dog);

        Result result = lesson.AttachSense(dog);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lesson.SenseAlreadyAttached");
        result.Error.Type.Should().Be(ErrorType.Conflict);
        lesson.VocabularyWords.Should().HaveCount(1);
    }

    [Theory]
    [InlineData(LessonType.Grammar)]
    [InlineData(LessonType.DailyPhrase)]
    public void Lesson_AttachSense_NonVocabularyLesson_Fails(LessonType type)
    {
        Lesson lesson = NewLesson(type);

        Result result = lesson.AttachSense(TestWords.System("Dog"));

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lesson.NotVocabulary");
        result.Error.Type.Should().Be(ErrorType.Validation);
    }

    [Fact]
    public void Lesson_DetachSense_RenumbersRemainingLinks()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        Sense bird = TestWords.System("Bird");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);
        lesson.AttachSense(bird);

        Result result = lesson.DetachSense(cat.Id);

        result.IsSuccess.Should().BeTrue();
        lesson.VocabularyWords.OrderBy(l => l.SortOrder).Select(l => l.SenseId).Should().Equal(dog.Id, bird.Id);
        lesson.VocabularyWords.Select(l => l.SortOrder).OrderBy(o => o).Should().Equal(0, 1);
    }

    [Fact]
    public void Lesson_DetachSense_NotAttached_FailsWithNotFound()
    {
        Lesson lesson = NewLesson();

        Result result = lesson.DetachSense(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lesson.SenseNotAttached");
        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public void Lesson_ReorderSenses_SetsNewOrder()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        Sense bird = TestWords.System("Bird");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);
        lesson.AttachSense(bird);

        Result result = lesson.ReorderSenses([bird.Id, dog.Id, cat.Id]);

        result.IsSuccess.Should().BeTrue();
        lesson.VocabularyWords.OrderBy(l => l.SortOrder).Select(l => l.SenseId).Should().Equal(bird.Id, dog.Id, cat.Id);
        lesson.VocabularyWords.Select(l => l.SortOrder).OrderBy(o => o).Should().Equal(0, 1, 2);
    }

    [Fact]
    public void Lesson_ReorderSenses_MissingSense_FailsAndKeepsOrder()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);

        Result result = lesson.ReorderSenses([cat.Id]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Lesson.InvalidOrder");
        lesson.VocabularyWords.OrderBy(l => l.SortOrder).Select(l => l.SenseId).Should().Equal(dog.Id, cat.Id);
    }

    [Fact]
    public void Lesson_ReorderSenses_UnknownOrDuplicateSense_Fails()
    {
        Lesson lesson = NewLesson();
        Sense dog = TestWords.System("Dog");
        Sense cat = TestWords.System("Cat");
        lesson.AttachSense(dog);
        lesson.AttachSense(cat);

        lesson.ReorderSenses([dog.Id, Guid.NewGuid()]).Error.Code.Should().Be("Lesson.InvalidOrder");
        lesson.ReorderSenses([dog.Id, dog.Id]).Error.Code.Should().Be("Lesson.InvalidOrder");
    }

    [Fact]
    public void Sense_IsHiddenFromChildrenInLessons_OnlyForSharedNotChildSafe()
    {
        TestWords.System().IsHiddenFromChildrenInLessons.Should().BeFalse();
        TestWords.Shared(Guid.NewGuid(), visibleToChildren: true).IsHiddenFromChildrenInLessons.Should().BeFalse();
        TestWords.Shared(Guid.NewGuid(), visibleToChildren: false).IsHiddenFromChildrenInLessons.Should().BeTrue();
    }
}
