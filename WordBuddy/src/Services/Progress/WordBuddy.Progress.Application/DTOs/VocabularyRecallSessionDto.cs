namespace WordBuddy.Progress.Application.DTOs;

public sealed record VocabularyRecallSessionDto(Guid Id, DateTime CheckedAtUtc, int WordsChecked, int WordsKnown);
