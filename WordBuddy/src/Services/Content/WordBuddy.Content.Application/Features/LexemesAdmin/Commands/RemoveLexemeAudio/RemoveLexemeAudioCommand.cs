using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Commands.RemoveLexemeAudio;

/// <summary>Removes the UK or US pronunciation of a lexeme. The file and the asset row go too, unless
/// something else still uses them. Removing audio that is not there succeeds.</summary>
public sealed record RemoveLexemeAudioCommand(Guid LexemeId, string Locale) : ICommand;
