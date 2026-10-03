# Identity — database diagram

Database `WordBuddyIdentity` · source: `IdentityDbContextModelSnapshot.cs`

```mermaid
erDiagram
    Users {
        guid Id PK
        string Email UK "max 256"
        string DisplayName "max 100"
        string PasswordHash "max 200"
        string AgeGroup "Child | Adult, max 20"
        bool IsAdmin
    }
```

| Index | Columns | Unique |
| --- | --- | --- |
| `IX_Users_Email` | `Email` | yes |

- `Users.Id` is referenced by Content and Progress as `UserId` (no FK — see [README](README.md)).
- No refresh-token table exists in the current model.
