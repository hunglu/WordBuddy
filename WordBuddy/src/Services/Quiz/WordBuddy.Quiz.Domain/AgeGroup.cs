namespace WordBuddy.Quiz.Domain;

/// <summary>Duplicated from Identity's own <c>AgeGroup</c> by design — services don't share
/// project references, so each service that needs this concept defines its own copy.</summary>
public enum AgeGroup
{
    Child,
    Adult
}
