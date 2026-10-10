namespace WordBuddy.Content.Application.DTOs;

/// <summary>Counts of one group word assignment. Each count is per (member, sense) pair.</summary>
/// <param name="Added">New links created.</param>
/// <param name="AlreadyHad">The member already had the sense on their list.</param>
/// <param name="SkippedForChildren">Not given because the sense is not cleared for children and cannot be approved by the assigning supporter.</param>
public sealed record GroupWordAssignmentResultDto(int Added, int AlreadyHad, int SkippedForChildren);

/// <summary>One row of the assignment history of a group.</summary>
public sealed record GroupWordAssignmentDto(Guid Id, Guid SenseId, string Word, string Definition, DateTime AssignedAtUtc);
