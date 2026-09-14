# Diagram

## 1. Component Flow

```mermaid
graph TB
    subgraph CLIENTS["Clients"]
        CLI["Browser / Mobile App"]
    end

    subgraph EXT["External Services"]
        AUTH0["Auth0\n(Identity Provider · RS256 JWKS)"]
        SQL[("SQL Server 2022\nWordBuddy DB")]
        FILES[("File Storage\nLocal / Blob")]
    end

    subgraph API["Presentation — WordBuddy.API"]
        direction TB
        PIPE["Middleware Pipeline\nSerilog Request Logging\n→ CORS\n→ Auth0 JWT Bearer\n→ Authorization"]
        CTR["Controllers\nLessonsController\nProgressController\nMediaController\nAuthController"]
        SWAGGER["Swagger UI  (Development)"]
    end

    subgraph APP["Application — WordBuddy.Application"]
        direction TB
        IFACES["CQRS Interfaces\nICommandHandler&lt;TCommand&gt;\nICommandHandler&lt;TCommand, TResult&gt;\nIQueryHandler&lt;TQuery, TResult&gt;"]

        subgraph LESSONS_F["Lessons"]
            LH1["GetLessonsQueryHandler"]
            LH2["GetLessonDetailQueryHandler"]
            LH3["CreateLessonCommandHandler"]
        end
        subgraph PROGRESS_F["Progress"]
            PH1["RecordProgressCommandHandler"]
            PH2["GetUserProgressQueryHandler"]
        end
        subgraph MEDIA_F["Media"]
            MH1["UploadMediaCommandHandler"]
            MH2["GetMediaAssetQueryHandler"]
        end

        VAL["FluentValidation\n(one Validator per handler)"]
        RIFACES["Repository Interfaces\nILessonRepository\nIVocabularyItemRepository\nIGrammarRuleRepository\nIDailyPhraseRepository\nIMediaAssetRepository\nILearnerProgressRepository"]
        SIFACES["Service Interfaces\nIAuthService · IFileStorageService"]
    end

    subgraph DOMAIN["Domain — WordBuddy.Domain"]
        ENT["Entities\nLesson · VocabularyItem · GrammarRule\nDailyPhrase · MediaAsset · User · LearnerProgress"]
        CMN["Result&lt;T&gt; · Error\n(Code + Description)"]
        ENUMS["Enums\nLevel · AgeGroup · LessonType\nMediaAssetType · TargetAgeGroup"]
    end

    subgraph INFRA["Infrastructure — WordBuddy.Infrastructure"]
        direction TB
        REPOS["EF Core Repositories\nLessonRepository\nVocabularyItemRepository\nGrammarRuleRepository\nDailyPhraseRepository\nMediaAssetRepository\nLearnerProgressRepository"]
        CTX["WordBuddyDbContext\nEF Core 8 · Fluent API configs\nNoTracking default"]
        ASVC["AuthService  (BCrypt)"]
        FSVC["LocalFileStorageService"]
        SEED["DataSeeder  (dev startup)"]
    end

    CLI -->|"HTTPS · Bearer JWT"| PIPE
    AUTH0 -.->|"JWKS public keys"| PIPE
    PIPE --> CTR
    CTR -.-> SWAGGER
    CTR --> IFACES
    IFACES --> LH1 & LH2 & LH3 & PH1 & PH2 & MH1 & MH2
    LH1 & LH2 & LH3 & PH1 & PH2 & MH1 & MH2 --> VAL
    LH1 & LH2 & LH3 & PH1 & PH2 & MH1 & MH2 --> RIFACES
    MH1 --> SIFACES
    RIFACES --> REPOS
    SIFACES --> ASVC & FSVC
    REPOS --> CTX --> SQL
    FSVC --> FILES
    SEED -.->|"dev startup"| CTX
```

## 2. Workflow — CQRS Request Processing

```mermaid
flowchart TD
    REQ([HTTP Request]) --> SRL[Serilog Request Logging]
    SRL --> CORS[CORS Middleware]
    CORS --> JWT[Auth0 JWT Middleware\nfetch JWKS · validate RS256 signature\ncheck audience + expiry]
    JWT -- "invalid / missing token" --> R401([401 Unauthorized])
    JWT -- valid --> AUTHZ[Authorization Middleware\npolicy evaluation]
    AUTHZ -- forbidden --> R403([403 Forbidden])
    AUTHZ -- allowed --> CTR[Controller\nparse & map → Command or Query]
    CTR --> HNDL[Handler\nICommandHandler / IQueryHandler]
    HNDL --> FV[FluentValidation\nvalidate all fields]
    FV -- invalid --> VFAIL["Result.Failure\nError.Validation(code, message)"]
    FV -- valid --> BRANCH{Command\nor Query?}

    BRANCH -- Query --> QREPO["Repository\nEF Core SELECT\nAsNoTracking"]
    QREPO --> QSQL[(SQL Server)]
    QSQL --> QRES["Result&lt;T&gt;.Success(value)"]

    BRANCH -- Command --> CREPO["Repository\nEF Core INSERT / UPDATE / DELETE\nSaveChangesAsync"]
    CREPO --> CSQL[(SQL Server)]
    CSQL --> CRES["Result.Success"]

    QRES --> EVAL{Result\nIsSuccess?}
    CRES --> EVAL
    VFAIL --> EVAL

    EVAL -- Success --> MAP["Map to HTTP response body"]
    EVAL -- "Error.NotFound" --> R404([404 Not Found])
    EVAL -- "Error.Conflict" --> R409([409 Conflict])
    EVAL -- "Error.Validation\nor Error.Failure" --> R400([400 Bad Request])

    MAP --> ROK([200 OK / 201 Created / 204 No Content])

    HNDL -. "unhandled exception" .-> GEX[Global Exception Middleware\nlog Error + CorrelationId\nreturn RFC 7807 ProblemDetails]
    GEX -.-> R500([500 Internal Server Error])

```

## 3. Sequence Diagram — GET /api/lessons/{id}

```mermaid
sequenceDiagram
    actor Client
    participant Auth0 as Auth0 JWKS
    participant MW as JWT Middleware
    participant LC as LessonsController
    participant GH as GetLessonDetail<br/>QueryHandler
    participant Val as Validator
    participant LR as LessonRepository
    participant VR as VocabularyItemRepository
    participant GR as GrammarRuleRepository
    participant DR as DailyPhraseRepository
    participant DB as SQL Server
    participant Log as Serilog

    Client->>MW: GET /api/lessons/{id}<br/>Authorization: Bearer &lt;token&gt;
    MW->>Auth0: fetch JWKS (cached)
    Auth0-->>MW: RS256 public keys
    MW->>MW: validate signature · audience · expiry · ClockSkew=0

    alt token invalid or absent
        MW-->>Client: 401 Unauthorized
    end

    MW->>LC: attach ClaimsPrincipal, forward
    LC->>GH: HandleAsync(GetLessonDetailQuery { LessonId })
    GH->>Log: LogInformation "GetLessonDetailQuery started"

    GH->>Val: ValidateAsync(query)
    alt LessonId is empty GUID
        Val-->>GH: IsValid = false
        GH-->>LC: Result.Failure(Error.Validation)
        LC-->>Client: 400 Bad Request
    end

    GH->>LR: GetByIdAsync(lessonId)
    LR->>Log: LogDebug "Fetching Lesson"
    LR->>DB: SELECT * FROM Lessons WHERE Id = @id
    DB-->>LR: Lesson row (or null)

    alt lesson not found
        LR-->>GH: Result.Failure(Error.NotFound)
        GH-->>LC: propagate failure
        LC-->>Client: 404 Not Found
    end

    LR-->>GH: Result.Success(lesson)

    note over GH,DR: Three Task&lt;Result&gt; variables started before any await — true concurrency

    par Concurrent content fetch
        GH->>VR: GetByLessonIdAsync(lessonId)
        VR->>DB: SELECT * FROM VocabularyItems<br/>WHERE LessonId = @id
        DB-->>VR: rows
        VR-->>GH: Result.Success(items)
    and
        GH->>GR: GetByLessonIdAsync(lessonId)
        GR->>DB: SELECT * FROM GrammarRules<br/>WHERE LessonId = @id<br/>ORDER BY OrderIndex
        DB-->>GR: ordered rows
        GR-->>GH: Result.Success(rules)
    and
        GH->>DR: GetByLessonIdAsync(lessonId)
        DR->>DB: SELECT * FROM DailyPhrases<br/>WHERE LessonId = @id
        DB-->>DR: rows
        DR-->>GH: Result.Success(phrases)
    end

    GH->>GH: assemble LessonDetailDto<br/>(lesson + vocab + grammar + phrases → DTOs)
    GH->>Log: LogInformation "GetLessonDetailQuery succeeded"
    GH-->>LC: Result.Success(LessonDetailDto)
    LC-->>Client: 200 OK<br/>{ id, title, level, vocabularyItems[], grammarRules[], dailyPhrases[] }

```

```mermaid
Key design decisions visible in the diagrams:

Observation Where it shows
Dependency inversion — controllers never touch Infrastructure Component Flow: arrow from Controller → ICommandHandler/IQueryHandler, not concrete classes
Domain has zero external dependencies Component Flow: Result<T> / Error flow up through all layers but nothing flows into Domain
Concurrent DB reads, not sequential Sequence: par block for VocabularyItem, GrammarRule, DailyPhrase
Auth0 is out-of-band for JWKS only Sequence: Auth0 only appears during JWT validation, not during business logic
All failures return Result<T>, never throw Workflow: every sad path exits via Result.Failure before touching the DB
```


```mermaid
```


```mermaid
```


