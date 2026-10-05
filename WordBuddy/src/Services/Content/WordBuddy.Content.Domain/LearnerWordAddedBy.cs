namespace WordBuddy.Content.Domain;

/// <summary>Who put a <see cref="LearnerWord"/> on a learner's list. Stored as a string.</summary>
public enum LearnerWordAddedBy
{
    Learner,
    Supporter,
    List
}
