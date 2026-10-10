# Content — database diagram

Database `WordBuddyContent` · source: `ContentDbContextModelSnapshot.cs` ·
last migration: `20261009153814_AddLearnerGroups`

```mermaid
erDiagram
    Lessons ||--o{ GrammarRules : "LessonId (cascade)"
    Lessons ||--o{ DailyPhrases : "LessonId (cascade)"
    Lessons ||--o{ LessonSenses : "LessonId (cascade)"
    Lexemes ||--o{ Senses : "LexemeId (no action)"
    Senses ||--o{ LessonSenses : "SenseId (no action)"
    Senses ||--o{ LearnerWords : "SenseId (cascade)"
    Senses ||--o{ SenseTranslations : "SenseId (cascade)"
    MediaAssets |o--o{ Senses : "AudioAssetId / ImageAssetId (no action)"
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
        guid ImageAssetId FK "nullable"
        string Source "System | Learner, max 20"
        guid OwnerUserId "Identity user or SystemOwner"
        string OwnerAgeGroup "nullable, max 20"
        string ShareStatus "Private | PendingReview | Shared | Rejected"
        bool VisibleToChildren
        datetime CreatedAtUtc
        datetime ModeratedAtUtc "nullable"
        guid ModeratedByUserId "nullable"
        string Origin "Manual | AutoFill, max 20, default Manual"
        string Examples "JSON string list (0-3), default []"
        string Collocations "JSON string list, default []"
        string Synonyms "JSON string list, default []"
        string Antonyms "JSON string list, default []"
        string TopicTags "JSON string list, default []"
        string RegisterNote "nullable, max 200"
        bool ChildSuitableHint "nullable; auto-fill hint for approvers"
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
        guid AddedByUserId "nullable; the supporter of a group assignment (Identity user); null = the learner"
        string PersonalContext "nullable, max 500"
        bool RequiresChildApproval "default false; child linked an unapproved auto-fill sense"
        datetime ChildApprovedAtUtc "nullable; supporter approval"
        guid ChildApprovedByUserId "nullable; supporter (Identity user)"
    }
    LessonSenses {
        guid LessonId PK, FK
        guid SenseId PK, FK
        int SortOrder
    }
    SupportLinkProjections {
        guid LinkId PK "Identity SupportLinks.Id"
        guid LearnerId "Identity user"
        guid SupporterId "Identity user"
        bool IsActive
        datetime UpdatedAtUtc "time of last applied event"
    }
    LearnerGroupMemberProjections {
        guid GroupId PK "Identity LearnerGroups.Id"
        guid LearnerId PK "Identity user"
        guid OwnerId "Identity user; the group owner"
        bool IsActive
        datetime UpdatedAtUtc "time of last applied event"
    }
    GroupWordAssignments {
        guid Id PK
        guid GroupId "Identity LearnerGroups.Id, no FK"
        guid SenseId "Senses.Id, no FK"
        guid AssignedBy "Identity user; the supporter"
        datetime AssignedAtUtc
    }
```

| Index | Columns | Unique / filter |
| --- | --- | --- |
| `UX_Lexemes_NormalizedLemma_PartOfSpeech` | `NormalizedLemma`, `PartOfSpeech` | unique, **no filter** (one `NULL` POS per lemma) |
| `IX_Lexemes_UkAudioAssetId`, `IX_Lexemes_UsAudioAssetId` | asset ids | — |
| `UX_Senses_ContentHash_OwnerUserId_Learner` | `ContentHash`, `OwnerUserId` | unique, `[Source] = 'Learner'` |
| `IX_Senses_LexemeId_Origin` | `LexemeId`, `Origin` | — (replaces `IX_Senses_LexemeId`) |
| `IX_Senses_*` | `ContentHash`; `OwnerUserId`; `ShareStatus`; `AudioAssetId`; `ImageAssetId` | — |
| `UX_SenseTranslations_SenseId_Locale` | `SenseId`, `Locale` | unique |
| `IX_LearnerWords_UserId_SenseId` | `UserId`, `SenseId` | unique |
| `IX_LearnerWords_SenseId` | `SenseId` | — |
| `IX_LessonSenses_SenseId` | `SenseId` | — |
| `IX_GrammarRules_LessonId`, `IX_DailyPhrases_LessonId` | `LessonId` | — |
| `IX_DailyPhrases_AudioAssetId`, `IX_DailyPhrases_VideoAssetId` | asset ids | — |
| `IX_SupportLinkProjections_LearnerId_IsActive` | `LearnerId`, `IsActive` | — |
| `IX_SupportLinkProjections_SupporterId_LearnerId` | `SupporterId`, `LearnerId` | — |
| `PK_LearnerGroupMemberProjections` | `GroupId`, `LearnerId` | yes (composite key) |
| `IX_GroupWordAssignments_GroupId` | `GroupId` | — |

- `UserId`, `OwnerUserId`, `ModeratedByUserId` are Identity ids — no FK.
- `Senses.Id` = the former `VocabularyWords.Id` (kept by the rename). Progress references it (`VocabularyRecallStats.VocabularyWordId`); sense audio is `vocab-{id}-{locale}.mp3`, so sense ids must stay stable.
- Lexeme audio is `lexeme-{lexemeId}-{locale}.mp3` (`en-GB` / `en-US`), written by auto-fill (WB-25) under `audio/`.
- Auto-fill senses (WB-25): `Origin = AutoFill`, `Source = System`, `ShareStatus = Shared`, `VisibleToChildren = false` until an admin approves. A supporter approval sets `LearnerWords.ChildApprovedAtUtc` for one child only. Existing rows: `Origin = Manual`, lists `[]`.
- Feature rules for these tables: `docs/features/vocabulary.md`.
- `LearnerWords` inserts/deletes write `OutboxMessage` rows in the same transaction (events `LearnerWordAdded` / `LearnerWordRemoved`, ids and timestamps only). Content also consumes the support-link events, so `InboxState` now holds their dedupe rows.
- `SupportLinkProjections` (WB-24) is filled only by Identity events `SupportLinkActivated` / `SupportLinkRevoked` (upsert by `LinkId`, older events skipped). It backs the `ChildHasSupporter` and `CanSupportLearner` policies.
- `LearnerGroupMemberProjections` (WB-27) is filled only by the Identity events `LearnerGroupMemberActivated` / `LearnerGroupMemberRemoved` / `LearnerGroupDeleted` (upsert by group + learner, older events skipped). A group has no row of its own: the owner is read from its member rows. `GroupWordAssignments` is the history of `AssignWordsToGroup`.

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
