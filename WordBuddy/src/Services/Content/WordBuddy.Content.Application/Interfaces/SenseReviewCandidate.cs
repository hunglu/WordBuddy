using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Interfaces;

/// <summary>A sense loaded for review (audio and image included), with the caller's own link to it
/// when one exists.</summary>
public sealed record SenseReviewCandidate(Sense Sense, LearnerWord? CallerLink);
