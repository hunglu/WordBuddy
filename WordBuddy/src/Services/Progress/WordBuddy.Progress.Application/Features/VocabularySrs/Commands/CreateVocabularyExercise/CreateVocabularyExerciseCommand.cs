using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.DTOs;

namespace WordBuddy.Progress.Application.Features.VocabularySrs.Commands.CreateVocabularyExercise;

/// <summary>Builds the next exercise for one word of the open session. <paramref name="UserId"/> comes from the JWT.</summary>
public sealed record CreateVocabularyExerciseCommand(Guid UserId, Guid SessionId, Guid SenseId) : ICommand<VocabularyExerciseDto>;
