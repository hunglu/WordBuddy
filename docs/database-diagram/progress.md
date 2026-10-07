# Progress — database diagram

Database `WordBuddyProgress` · source: `ProgressDbContextModelSnapshot.cs` ·
last migration: `20261007032854_AddVocabularySrs`

The seven domain tables have no foreign keys between them; every id column points to another service.

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
    LearnerWordMemberships {
        guid Id PK
        guid UserId "Identity user"
        guid SenseId "Content sense"
        guid AddedBy "Identity user; empty if only a removal was seen"
        datetime AddedAtUtc
        bool IsActive "false = removed, row kept"
        datetime LastEventAtUtc "older events ignored"
    }
    VocabularyRecallSessions {
        guid Id PK
        guid UserId "Identity user"
        datetime CheckedAtUtc
        int WordsChecked
        int WordsKnown
    }
    LearnerWordStates {
        guid Id PK
        guid UserId "Identity user"
        guid SenseId "Content sense"
        string Status "New | Learning | Review | Mastered | Leech, max 20"
        float Stability "FSRS, days"
        float Difficulty "FSRS, 1-10"
        datetime DueAtUtc
        int Reps
        int Lapses
        string FsrsPhase "Learning | Review | Relearning, max 20"
        int FsrsStep "nullable; null in Review"
        datetime LastReviewedAtUtc "nullable"
        datetime FirstReviewedAtUtc "nullable"
        bool IsActive "follows membership"
    }
    ReviewLogs {
        guid Id PK
        guid UserId "Identity user"
        guid SenseId "Content sense"
        guid SessionId
        datetime OccurredAtUtc "server UTC"
        string ExerciseType "max 30"
        string Skill "max 30"
        bool IsCorrect
        int ResponseMs
        bool HintUsed
        bool IsDue "server-derived"
        int AttemptNo "server-derived"
        string Rating "Again | Hard | Good | Easy, max 10"
    }
    VocabularyLearnerSettings {
        guid UserId PK "Identity user"
        int NewWordsPerDay "nullable, 0-50; null = backlog rule"
    }
```

| Index | Columns | Unique |
| --- | --- | --- |
| `IX_LearnerProgressEntries_UserId_LessonId` | `UserId`, `LessonId` | yes |
| `IX_VocabularyRecallStats_UserId_VocabularyWordId` | `UserId`, `VocabularyWordId` | yes |
| `IX_VocabularyRecallSessions_UserId_CheckedAtUtc` | `UserId`, `CheckedAtUtc` | — |
| `IX_LearnerWordMemberships_UserId_SenseId` | `UserId`, `SenseId` | yes (natural-key dedupe) |
| `IX_LearnerWordStates_UserId_SenseId` | `UserId`, `SenseId` | yes |
| `IX_LearnerWordStates_UserId_IsActive_DueAtUtc` | `UserId`, `IsActive`, `DueAtUtc` | — |
| `IX_ReviewLogs_UserId_OccurredAtUtc` | `UserId`, `OccurredAtUtc` | — |
| `IX_ReviewLogs_UserId_SenseId` | `UserId`, `SenseId` | — |

- `Word` is copied from Content at submit time, so recall history survives a deleted word.
- `LearnerWordMemberships` is filled only by Content events (WB-21). No public endpoint reads it yet.
- `LearnerWordStates` (WB-22) is created and (de)activated in the same save as its membership.
- `ReviewLogs` (WB-22) is insert-only: no update or delete path exists.

## MassTransit messaging tables (WB-21)

Schema owned by MassTransit (`AddWordBuddyMessagingEntities`). Do not change by hand.

```mermaid
erDiagram
    OutboxState ||--o{ OutboxMessage : "OutboxId (no action)"
    InboxState ||--o{ OutboxMessage : "InboxMessageId, InboxConsumerId (no action)"
    InboxState {
        long Id PK "identity"
        guid MessageId UK "AK with ConsumerId"
        guid ConsumerId UK
        guid LockId
        bytes RowVersion "rowversion"
        datetime Received
        int ReceiveCount
        datetime ExpirationTime "nullable"
        datetime Consumed "nullable"
        datetime Delivered "nullable"
        long LastSequenceNumber "nullable"
    }
    OutboxState {
        guid OutboxId PK
        guid LockId
        bytes RowVersion "rowversion"
        datetime Created
        datetime Delivered "nullable"
        long LastSequenceNumber "nullable"
    }
    OutboxMessage {
        long SequenceNumber PK "identity"
        datetime EnqueueTime "nullable"
        datetime SentTime
        string Headers "nullable"
        string Properties "nullable"
        guid InboxMessageId FK "nullable"
        guid InboxConsumerId FK "nullable"
        guid OutboxId FK "nullable"
        guid MessageId
        string ContentType "max 256"
        string MessageType
        string Body
        guid ConversationId "nullable"
        guid CorrelationId "nullable"
        guid InitiatorId "nullable"
        guid RequestId "nullable"
        string SourceAddress "nullable, max 256"
        string DestinationAddress "nullable, max 256"
        string ResponseAddress "nullable, max 256"
        string FaultAddress "nullable, max 256"
        datetime ExpirationTime "nullable"
    }
```

| Index | Columns | Unique |
| --- | --- | --- |
| `AK_InboxState_MessageId_ConsumerId` | `MessageId`, `ConsumerId` | yes (inbox dedupe) |
| `IX_InboxState_Delivered` | `Delivered` | — |
| `IX_OutboxState_Created` | `Created` | — |
| `IX_OutboxMessage_EnqueueTime`, `IX_OutboxMessage_ExpirationTime` | one column each | — |
| `IX_OutboxMessage_OutboxId_SequenceNumber` | `OutboxId`, `SequenceNumber` | yes, `OutboxId IS NOT NULL` |
| `IX_OutboxMessage_InboxMessageId_InboxConsumerId_SequenceNumber` | 3 columns | yes, both inbox ids not null |
