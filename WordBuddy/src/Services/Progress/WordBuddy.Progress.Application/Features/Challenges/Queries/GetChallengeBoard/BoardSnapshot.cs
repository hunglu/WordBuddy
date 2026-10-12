namespace WordBuddy.Progress.Application.Features.Challenges.Queries.GetChallengeBoard;

/// <summary>The cached, unfiltered ranking of one challenge. Internal: it holds user ids and is never returned as is.</summary>
internal sealed record BoardSnapshot(int ParticipantCount, List<BoardSnapshotRow> Rows);

/// <summary>One participant in a <see cref="BoardSnapshot"/>, already in board order.</summary>
internal sealed record BoardSnapshotRow(Guid UserId, int? Rank, int Score, bool TargetReached, string Name, string? AvatarId);
