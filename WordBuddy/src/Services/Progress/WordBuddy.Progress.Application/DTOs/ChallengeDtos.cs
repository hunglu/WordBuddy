using WordBuddy.Progress.Domain.Challenges;

namespace WordBuddy.Progress.Application.DTOs;

/// <summary>A challenge as the caller sees it. Holds no user ids.</summary>
/// <param name="Id">Challenge id.</param>
/// <param name="TemplateKey">Template key.</param>
/// <param name="Name">Display name of the template.</param>
/// <param name="Audience">Adult or Child.</param>
/// <param name="Kind">Ranked or Target.</param>
/// <param name="Metric">What is counted.</param>
/// <param name="Target">Target to reach (target challenges), else null.</param>
/// <param name="Status">Open or Running.</param>
/// <param name="OpensAtUtc">Joining opens.</param>
/// <param name="StartsAtUtc">Counting starts.</param>
/// <param name="EndsAtUtc">Counting ends.</param>
/// <param name="Capacity">Most participants.</param>
/// <param name="ParticipantCount">Participants now.</param>
/// <param name="Joined">Whether the caller joined.</param>
/// <param name="MyScore">The caller score, when joined.</param>
/// <param name="MyRank">The caller rank, when joined and projected.</param>
public sealed record ChallengeDto(
    Guid Id,
    string TemplateKey,
    string Name,
    ChallengeAudience Audience,
    ChallengeKind Kind,
    ChallengeMetric Metric,
    int? Target,
    ChallengeStatus Status,
    DateTime OpensAtUtc,
    DateTime StartsAtUtc,
    DateTime EndsAtUtc,
    int Capacity,
    int ParticipantCount,
    bool Joined,
    int? MyScore,
    int? MyRank);

/// <summary>One board row. Holds no user id. For a child viewer only the caller row has a score.</summary>
/// <param name="Rank">Dense rank (1 is best), or null before the first projection.</param>
/// <param name="Name">Alias, else display name (adult) or a neutral label.</param>
/// <param name="AvatarId">Avatar id from the fixed catalog.</param>
/// <param name="IsMe">Whether this row is the caller.</param>
/// <param name="Score">Score; for a child viewer only on the caller row.</param>
/// <param name="TargetReached">Whether the target is reached; for a child viewer only on the caller row.</param>
public sealed record ChallengeBoardEntryDto(int? Rank, string Name, string? AvatarId, bool IsMe, int? Score, bool? TargetReached);

/// <summary>A challenge board. Adult viewer: the full ranking. Child viewer: the caller row and the top 5.</summary>
/// <param name="ChallengeId">Challenge id.</param>
/// <param name="Name">Display name of the template.</param>
/// <param name="Status">Challenge status.</param>
/// <param name="Kind">Ranked or Target.</param>
/// <param name="Metric">What is counted.</param>
/// <param name="Target">Target (target challenges), else null.</param>
/// <param name="ParticipantCount">All participants.</param>
/// <param name="MyRank">The caller rank, or null.</param>
/// <param name="MyScore">The caller score, or null.</param>
/// <param name="Entries">Board rows in rank order.</param>
public sealed record ChallengeBoardDto(
    Guid ChallengeId,
    string Name,
    ChallengeStatus Status,
    ChallengeKind Kind,
    ChallengeMetric Metric,
    int? Target,
    int ParticipantCount,
    int? MyRank,
    int? MyScore,
    IReadOnlyList<ChallengeBoardEntryDto> Entries);
