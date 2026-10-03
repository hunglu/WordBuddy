# Progress — database diagram

Database `WordBuddyProgress` · source: `ProgressDbContextModelSnapshot.cs`

The three tables have no foreign keys between them; every id column points to another service.

```mermaid
erDiagram
    LearnerProgressEntries {
        guid Id PK
        guid UserId "Identity user"
        guid LessonId "Content lesson"
        bool IsCompleted
        int ScorePercent "nullable"
        datetime CompletedAtUtc "nullable"
    }
    VocabularyRecallStats {
        guid Id PK
        guid UserId "Identity user"
        guid VocabularyWordId "Content word"
        string Word "copied at submit, max 200"
        string Status "Known | Learning, max 20"
        int TimesChecked
        int TimesKnown
        datetime LastCheckedAtUtc
    }
    VocabularyRecallSessions {
        guid Id PK
        guid UserId "Identity user"
        datetime CheckedAtUtc
        int WordsChecked
        int WordsKnown
    }
```

| Index | Columns | Unique |
| --- | --- | --- |
| `IX_LearnerProgressEntries_UserId_LessonId` | `UserId`, `LessonId` | yes |
| `IX_VocabularyRecallStats_UserId_VocabularyWordId` | `UserId`, `VocabularyWordId` | yes |
| `IX_VocabularyRecallSessions_UserId_CheckedAtUtc` | `UserId`, `CheckedAtUtc` | — |

- `Word` is copied from Content at submit time, so recall history survives a deleted word.
