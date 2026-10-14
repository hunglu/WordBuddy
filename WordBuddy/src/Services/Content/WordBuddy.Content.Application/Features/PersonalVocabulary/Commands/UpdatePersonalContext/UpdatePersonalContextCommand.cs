using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.UpdatePersonalContext;

/// <summary>Sets, edits or clears the caller's private note on a word in their list.
/// <paramref name="UserId"/> comes from the JWT. A <see langword="null"/> or blank note clears it.</summary>
public sealed record UpdatePersonalContextCommand(Guid SenseId, Guid UserId, string? PersonalContext) : ICommand;
