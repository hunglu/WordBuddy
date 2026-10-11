namespace WordBuddy.Progress.Domain.Challenges;

/// <summary>Who a challenge is for. Audiences are never mixed: a child plays only against children.</summary>
public enum ChallengeAudience
{
    /// <summary>Adult learners.</summary>
    Adult,

    /// <summary>Child learners (after Guardian consent).</summary>
    Child
}

/// <summary>Maps <see cref="AgeGroup"/> to <see cref="ChallengeAudience"/>.</summary>
public static class ChallengeAudiences
{
    /// <summary>The audience of an account with this age group.</summary>
    public static ChallengeAudience From(AgeGroup ageGroup) =>
        ageGroup == AgeGroup.Child ? ChallengeAudience.Child : ChallengeAudience.Adult;
}
