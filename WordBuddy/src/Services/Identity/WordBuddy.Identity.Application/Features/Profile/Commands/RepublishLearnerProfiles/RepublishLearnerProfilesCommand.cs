using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.RepublishLearnerProfiles;

/// <summary>Admin backfill: publishes <c>LearnerProfileUpdated</c> for every existing user. Safe to rerun.</summary>
public sealed record RepublishLearnerProfilesCommand : ICommand<int>;
