# Identity — database diagram

Database `WordBuddyIdentity` · source: `IdentityDbContextModelSnapshot.cs` ·
last migration: `20261008021803_AddSupportLinks`

```mermaid
erDiagram
    SupportLinks ||--o{ UnlinkRequests : "LinkId (restrict)"

    Users {
        guid Id PK
        string Email UK "max 256"
        string DisplayName "max 100"
        string PasswordHash "max 200"
        string AgeGroup "Child | Adult, max 20"
        bool IsAdmin
        string Alias UK "nullable, max 20; unique when set"
        string AvatarId "nullable, max 30; fixed catalog"
    }
    SupportLinks {
        guid Id PK
        guid LearnerId "Users.Id, no FK"
        guid SupporterId "Users.Id, no FK; always Adult"
        bool IsPrimary "child only: first linked adult"
        string Relationship "nullable, Parent | Teacher | Partner | Other, max 20"
        string Status "PendingPrimaryApproval | Active | Revoked, max 30"
        guid GrantedBy
        datetime CreatedAtUtc
        datetime UpdatedAtUtc
    }
    SupportLinkInvitations {
        guid Id PK
        guid CreatedById "Users.Id, no FK"
        string CreatorSide "Learner | Supporter, max 20"
        string Relationship "nullable, max 20"
        string CodeHash UK "SHA-256 hex, max 64"
        string TokenHash UK "SHA-256 hex, max 64"
        string Status "Pending | Accepted | Cancelled, max 20"
        datetime CreatedAtUtc
        datetime ExpiresAtUtc "created + 7 days"
        guid AcceptedById "nullable"
        guid LinkId "nullable"
    }
    UnlinkRequests {
        guid Id PK
        guid LinkId FK
        guid RequestedById
        string RequestedBySide "Learner | Supporter, max 20"
        datetime RequestedAtUtc
        string Status "Pending | OverrideRequested | Confirmed | Declined | Cancelled | CompletedByAdmin | RejectedByAdmin, max 30"
        datetime EscalatedAtUtc "nullable"
        guid ResolvedById "nullable"
        datetime ResolvedAtUtc "nullable"
    }
    SupportLinkAuditEntries {
        guid Id PK
        guid LinkId
        guid ActorId
        string Action "max 40"
        datetime AtUtc
        string Reason "nullable, max 500; admin actions"
    }
```

| Index | Columns | Unique / filter |
| --- | --- | --- |
| `IX_Users_Email` | `Email` | yes |
| `IX_Users_Alias` | `Alias` | yes, `[Alias] IS NOT NULL` |
| `IX_SupportLinks_LearnerId_ActivePrimary` | `LearnerId` | yes, `[IsPrimary] = 1 AND [Status] = 'Active'` (one active Primary per learner) |
| `IX_SupportLinks_LearnerId`, `IX_SupportLinks_SupporterId` | one column each | — |
| `IX_SupportLinkInvitations_CodeHash`, `IX_SupportLinkInvitations_TokenHash` | one column each | yes |
| `IX_SupportLinkInvitations_CreatedById` | `CreatedById` | — |
| `IX_UnlinkRequests_LinkId_Open` | `LinkId` | yes, `[Status] IN ('Pending', 'OverrideRequested')` (one open request per link) |
| `IX_UnlinkRequests_Status` | `Status` | — |
| `IX_SupportLinkAuditEntries_LinkId` | `LinkId` | — |

- `Users.Id` is referenced by Content and Progress as `UserId` / `LearnerId` / `SupporterId` (no FK — see [README](README.md)).
- `SupportLinkAuditEntries` is insert-only. It holds ids, action, time and reason — no names, emails or child data.
- Activate/revoke writes `OutboxMessage` rows (`SupportLinkActivated` / `SupportLinkRevoked`, ids and time only) in the same transaction. Identity only publishes; `InboxState` stays empty.
- No refresh-token table exists in the current model.
- Feature rules: `docs/features/learner-support-links.md`.

## MassTransit messaging tables (WB-24)

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
