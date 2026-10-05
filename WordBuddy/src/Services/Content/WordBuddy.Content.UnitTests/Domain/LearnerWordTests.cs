using FluentAssertions;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.UnitTests.Domain;

public class LearnerWordTests
{
    [Fact]
    public void LearnerWord_Constructor_SetsAddedByLearnerAndNoPersonalContext()
    {
        Guid senseId = Guid.NewGuid();

        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), senseId, isAuthor: true);

        link.SenseId.Should().Be(senseId);
        link.AddedBy.Should().Be(LearnerWordAddedBy.Learner);
        link.PersonalContext.Should().BeNull();
    }

    [Fact]
    public void LearnerWord_ConstructorWithSense_SetsAddedByLearnerAndNoPersonalContext()
    {
        Sense sense = TestWords.Learner(Guid.NewGuid());

        LearnerWord link = new(Guid.NewGuid(), Guid.NewGuid(), sense, isAuthor: false);

        link.SenseId.Should().Be(sense.Id);
        link.Sense.Should().BeSameAs(sense);
        link.AddedBy.Should().Be(LearnerWordAddedBy.Learner);
        link.PersonalContext.Should().BeNull();
    }
}
