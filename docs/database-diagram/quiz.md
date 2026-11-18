# Quiz — database diagram

Database `WordBuddyQuiz` · source: `SqlServerQuizDbContextModelSnapshot.cs`

> **PostgreSQL:** On PostgreSQL (`Database:Provider=Postgres`, ADR 0009) `QuizQuestions.Options` is `text` instead of `nvarchar(max)`. Logical schema, names and indexes are the same. Postgres migrations: `Migrations/Postgres/` (last: `InitialCreate`).

```mermaid
erDiagram
    Quizzes ||--o{ QuizQuestions : "QuizId (cascade)"

    Quizzes {
        guid Id PK
        guid LessonId "Content lesson"
        string Title "max 200"
        string Description "max 2000"
        string Level "max 20"
        string TargetAgeGroup "max 20"
    }
    QuizQuestions {
        guid Id PK
        guid QuizId FK
        string Type "QuizQuestionType, max 20"
        string Text "max 500"
        string Options "serialized list"
        int CorrectOptionIndex
        string Explanation "max 1000"
    }
```

| Index | Columns | Unique |
| --- | --- | --- |
| `IX_QuizQuestions_QuizId` | `QuizId` | — |

- `Quizzes.LessonId` is a Content lesson id — no FK.
