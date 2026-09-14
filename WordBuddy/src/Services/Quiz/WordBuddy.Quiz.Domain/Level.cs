namespace WordBuddy.Quiz.Domain;

/// <summary>Duplicated from Content's own <c>Level</c> by design — services don't share
/// project references, so each service that needs this concept defines its own copy.</summary>
public enum Level
{
    Beginner,
    Intermediate,
    Advanced
}
