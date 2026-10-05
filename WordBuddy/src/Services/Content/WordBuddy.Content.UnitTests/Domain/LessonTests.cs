using FluentAssertions;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests.Domain;

public class LessonTests
{
    [Fact]
    public void Lesson_AddVocabularyWord_CreatesLessonSenseLinksInSortOrder()
    {
        Lesson lesson = new(Guid.NewGuid(), "Animals", "Common animals.", LessonType.Vocabulary, Level.Beginner, AgeGroup.Child);
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
}
