# Content — database diagram

Database `WordBuddyContent` · source: `ContentDbContextModelSnapshot.cs` ·
last migration: `20261005023005_SplitVocabularyIntoLexemesAndSenses`

```mermaid
erDiagram
    Lessons ||--o{ GrammarRules : "LessonId (cascade)"
    Lessons ||--o{ DailyPhrases : "LessonId (cascade)"
    Lessons ||--o{ LessonSenses : "LessonId (cascade)"
    Lexemes ||--o{ Senses : "LexemeId (no action)"
    Senses ||--o{ LessonSenses : "SenseId (no action)"
    Senses ||--o{ LearnerWords : "SenseId (cascade)"
    Senses ||--o{ SenseTranslations : "SenseId (cascade)"
    MediaAssets |o--o{ Senses : "AudioAssetId (no action)"
    MediaAssets |o--o{ Lexemes : "UkAudioAssetId / UsAudioAssetId (no action)"
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
    Lexemes {
        guid Id PK
        string Lemma "max 200"
        string NormalizedLemma "max 200"
        string PartOfSpeech "nullable, max 20; NULL = not known yet"
        string IpaUk "nullable, max 100"
        string IpaUs "nullable, max 100"
        guid UkAudioAssetId FK "nullable"
        guid UsAudioAssetId FK "nullable"
        string Syllables "nullable, max 200"
        string WordForms "JSON string list"
        string CefrLevel "nullable, A1-C2"
        int FrequencyRank "nullable"
        datetime CreatedAtUtc
    }
    Senses {
        guid Id PK "former VocabularyWords.Id"
        guid LexemeId FK
        string Word "max 200"
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
    SenseTranslations {
        guid Id PK
        guid SenseId FK
        string Locale "BCP-47, max 35"
        string Text "max 500"
    }
    LearnerWords {
        guid Id PK
        guid UserId "Identity user"
        guid SenseId FK
        datetime AddedAtUtc
        bool IsAuthor
        string AddedBy "Learner | Supporter | List, max 20"
        string PersonalContext "nullable, max 500"
    }
    LessonSenses {
        guid LessonId PK, FK
        guid SenseId PK, FK
        int SortOrder
    }
```

| Index | Columns | Unique / filter |
| --- | --- | --- |
| `UX_Lexemes_NormalizedLemma_PartOfSpeech` | `NormalizedLemma`, `PartOfSpeech` | unique, **no filter** (one `NULL` POS per lemma) |
| `IX_Lexemes_UkAudioAssetId`, `IX_Lexemes_UsAudioAssetId` | asset ids | — |
| `UX_Senses_ContentHash_OwnerUserId_Learner` | `ContentHash`, `OwnerUserId` | unique, `[Source] = 'Learner'` |
| `IX_Senses_*` | `ContentHash`; `LexemeId`; `OwnerUserId`; `ShareStatus`; `AudioAssetId` | — |
| `UX_SenseTranslations_SenseId_Locale` | `SenseId`, `Locale` | unique |
| `IX_LearnerWords_UserId_SenseId` | `UserId`, `SenseId` | unique |
| `IX_LearnerWords_SenseId` | `SenseId` | — |
| `IX_LessonSenses_SenseId` | `SenseId` | — |
| `IX_GrammarRules_LessonId`, `IX_DailyPhrases_LessonId` | `LessonId` | — |
| `IX_DailyPhrases_AudioAssetId`, `IX_DailyPhrases_VideoAssetId` | asset ids | — |

- `UserId`, `OwnerUserId`, `ModeratedByUserId` are Identity ids — no FK.
- `Senses.Id` = the former `VocabularyWords.Id` (kept by the rename). Progress references it (`VocabularyRecallStats.VocabularyWordId`); sense audio is `vocab-{id}-{locale}.mp3`, so sense ids must stay stable.
- Lexeme audio is `lexeme-{lexemeId}-{locale}.mp3` (`en-GB` / `en-US`); not written yet.
- Feature rules for these tables: `docs/features/vocabulary.md`.
