using WordBuddy.Progress.Domain;

namespace WordBuddy.Progress.Application.DTOs;

/// <summary>Outcome of one recorded answer.</summary>
/// <param name="Status">The word's status after the answer.</param>
/// <param name="DueAtUtc">Next due time (UTC).</param>
/// <param name="Rating">Server-derived rating.</param>
public sealed record VocabularyReviewResultDto(WordStatus Status, DateTime DueAtUtc, FsrsRating Rating);

/// <summary>One word in a session. Word text comes from Content.</summary>
/// <param name="SenseId">Content's sense id.</param>
/// <param name="Status">Current status.</param>
/// <param name="DueAtUtc">Due time (UTC).</param>
public sealed record VocabularySessionItemDto(Guid SenseId, WordStatus Status, DateTime DueAtUtc);

/// <summary>Today's session: due words first, then new words up to the cap.</summary>
/// <param name="SessionId">Id the client sends back with every answer.</param>
/// <param name="DueItems">Due words, earliest first.</param>
/// <param name="NewItems">New words, oldest added first.</param>
/// <param name="NewWordCap">Today's new-word cap.</param>
/// <param name="NewWordsIntroducedToday">New words already introduced today (client's day).</param>
public sealed record VocabularySessionDto(
    Guid SessionId,
    IReadOnlyList<VocabularySessionItemDto> DueItems,
    IReadOnlyList<VocabularySessionItemDto> NewItems,
    int NewWordCap,
    int NewWordsIntroducedToday);

/// <summary>One active word state of the caller.</summary>
/// <param name="SenseId">Content's sense id.</param>
/// <param name="Status">Current status.</param>
/// <param name="DueAtUtc">Due time (UTC).</param>
/// <param name="Reps">Scheduling reviews so far.</param>
/// <param name="Lapses">Lapses so far.</param>
public sealed record LearnerWordStateDto(Guid SenseId, WordStatus Status, DateTime DueAtUtc, int Reps, int Lapses);

/// <summary>The caller's vocabulary settings.</summary>
/// <param name="NewWordsPerDay">Own daily cap; <see langword="null"/> = backlog rule.</param>
/// <param name="SupporterNewWordCap">Cap set by a supporter; wins over the own cap when present.</param>
public sealed record VocabularySettingsDto(int? NewWordsPerDay, int? SupporterNewWordCap);

/// <summary>A learner vocabulary settings as seen by an active supporter.</summary>
/// <param name="LearnerId">The learner.</param>
/// <param name="NewWordsPerDay">The learner own cap.</param>
/// <param name="SupporterNewWordCap">The supporter cap in force, if any.</param>
/// <param name="SupporterCapSetBy">The supporter who set it.</param>
public sealed record LearnerVocabularySettingsDto(Guid LearnerId, int? NewWordsPerDay, int? SupporterNewWordCap, Guid? SupporterCapSetBy);
