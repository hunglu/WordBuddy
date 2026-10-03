# Content — database diagram

Database `WordBuddyContent` · source: `ContentDbContextModelSnapshot.cs` ·
last migration: `20261002174303_DropVocabularyWordIdRemaps`

```mermaid
erDiagram
    Lessons ||--o{ GrammarRules : "LessonId (cascade)"
    Lessons ||--o{ DailyPhrases : "LessonId (cascade)"
    Lessons ||--o{ LessonVocabularyWords : "LessonId (cascade)"
    VocabularyWords ||--o{ LessonVocabularyWords : "VocabularyWordId (no action)"
    VocabularyWords ||--o{ UserVocabularyWords : "VocabularyWordId (cascade)"
    MediaAssets |o--o{ VocabularyWords : "AudioAssetId (no action)"
    MediaAssets |o--o{ DailyPhrases : "AudioAssetId / VideoAssetId (no action)"

    Lessons {
        guid Id PK
        string Title "max 200"
        string Description "max 2000"
        string Type "LessonType, max 20"
        string Level "Level, max 20"
        string TargetAgeGroup "max 20"
        bool IsPublished
    }
    GrammarRules {
        guid Id PK
        guid LessonId FK
        string Title "max 200"
        string Explanation "max 2000"
        string Examples "serialized list"
    }
    DailyPhrases {
        guid Id PK
        guid LessonId FK
        string Phrase "max 500"
        string Translation "max 500"
        guid AudioAssetId FK "nullable"
        guid VideoAssetId FK "nullable"
    }
    MediaAssets {
        guid Id PK
        string Type "MediaAssetType, max 20"
        string Url "max 1000"
    }
    VocabularyWords {
        guid Id PK
        string Word "max 200"
        string NormalizedWord "max 200"
        string Definition "max 2000"
        string Example "nullable, max 500"
        string ContentHash "word + definition + example, max 64"
        guid AudioAssetId FK "nullable"
        string Source "System | Learner, max 20"
        guid OwnerUserId "Identity user or SystemOwner"
        string OwnerAgeGroup "nullable, max 20"
        string ShareStatus "Private | PendingReview | Shared | Rejected"
        bool VisibleToChildren
        datetime CreatedAtUtc
        datetime ModeratedAtUtc "nullable"
        guid ModeratedByUserId "nullable"
    }
    UserVocabularyWords {
        guid Id PK
        guid UserId "Identity user"
        guid VocabularyWordId FK
        datetime AddedAtUtc
        bool IsAuthor
    }
    LessonVocabularyWords {
        guid LessonId PK, FK
        guid VocabularyWordId PK, FK
        int SortOrder
    }
```

| Index | Columns | Unique / filter |
| --- | --- | --- |
| `IX_VocabularyWords_ContentHash_OwnerUserId` | `ContentHash`, `OwnerUserId` | unique, `[Source] = 'Learner'` |
| `IX_VocabularyWords_*` | `ContentHash`; `NormalizedWord`; `OwnerUserId`; `ShareStatus`; `AudioAssetId` | — |
| `IX_UserVocabularyWords_UserId_VocabularyWordId` | `UserId`, `VocabularyWordId` | unique |
| `IX_UserVocabularyWords_VocabularyWordId` | `VocabularyWordId` | — |
| `IX_LessonVocabularyWords_VocabularyWordId` | `VocabularyWordId` | — |
| `IX_GrammarRules_LessonId`, `IX_DailyPhrases_LessonId` | `LessonId` | — |
| `IX_DailyPhrases_AudioAssetId`, `IX_DailyPhrases_VideoAssetId` | asset ids | — |

- `UserId`, `OwnerUserId`, `ModeratedByUserId` are Identity ids — no FK.
- `VocabularyWords.Id` is referenced by Progress (`VocabularyRecallStats.VocabularyWordId`); audio files are named `vocab-{id}-{locale}.mp3`, so word ids must stay stable.
- Feature rules for these tables: `docs/features/vocabulary.md`.
