using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Domain.Entities;
using WordBuddy.Domain.Enums;
using WordBuddy.Infrastructure.Persistence;

namespace WordBuddy.Infrastructure.Seeding;

/// <summary>Seeds the database with development sample data. Invoked only in the Development environment.</summary>
public static class DataSeeder
{
    /// <summary>
    /// Applies any pending migrations, then seeds users and lesson content if the tables are empty.
    /// Safe to call on every startup — all seeds are guarded by <c>AnyAsync</c> checks.
    /// </summary>
    public static async Task MigrateAndSeedAsync(IServiceProvider services, CancellationToken ct = default)
    {
        WordBuddyDbContext context = services.GetRequiredService<WordBuddyDbContext>();
        await context.Database.MigrateAsync(ct);
        await SeedUsersAsync(context, ct);
        await SeedLessonsAsync(context, ct);
    }

    private static async Task SeedUsersAsync(WordBuddyDbContext context, CancellationToken ct)
    {
        if (await context.Users.AnyAsync(ct)) return;

        DateTime now = DateTime.UtcNow;
        User admin = new(
            Guid.NewGuid(),
            "admin@wordbuddy.com",
            BCrypt.Net.BCrypt.HashPassword("Admin@123", workFactor: 12),
            "Admin",
            AgeGroup.Adult,
            Level.Advanced,
            createdAt: now,
            updatedAt: now);

        await context.Users.AddAsync(admin, ct);
        await context.SaveChangesAsync(ct);
    }

    private static async Task SeedLessonsAsync(WordBuddyDbContext context, CancellationToken ct)
    {
        if (await context.Lessons.AnyAsync(ct)) return;

        Guid vocabLessonId   = Guid.NewGuid();
        Guid grammarLessonId = Guid.NewGuid();
        Guid phraseLessonId  = Guid.NewGuid();
        DateTime now = DateTime.UtcNow;

        Lesson[] lessons =
        [
            new(vocabLessonId,
                "Animals",
                "Learn common English animal vocabulary.",
                LessonType.Vocabulary,
                Level.Beginner,
                TargetAgeGroup.Both,
                isPublished: true,
                orderIndex: 1,
                createdAt: now),

            new(grammarLessonId,
                "Present Simple",
                "Use the present simple tense correctly.",
                LessonType.Grammar,
                Level.Beginner,
                TargetAgeGroup.Adult,
                isPublished: true,
                orderIndex: 2,
                createdAt: now),

            new(phraseLessonId,
                "Daily Greetings",
                "Common English greetings used every day.",
                LessonType.DailyPhrase,
                Level.Beginner,
                TargetAgeGroup.Child,
                isPublished: true,
                orderIndex: 3,
                createdAt: now),
        ];

        await context.Lessons.AddRangeAsync(lessons, ct);
        await context.SaveChangesAsync(ct);

        // VocabularyItems for "Animals" — saved after lessons so the FK is satisfied.
        VocabularyItem[] vocabItems =
        [
            new(Guid.NewGuid(), vocabLessonId,
                "Cat",
                "A small domesticated carnivorous mammal.",
                "The cat sat on the mat.",
                "/kæt/", null, null),

            new(Guid.NewGuid(), vocabLessonId,
                "Dog",
                "A domesticated carnivorous mammal kept as a pet.",
                "I have a pet dog named Max.",
                "/dɒɡ/", null, null),

            new(Guid.NewGuid(), vocabLessonId,
                "Elephant",
                "A very large mammal with a long trunk and tusks.",
                "The elephant sprayed water with its trunk.",
                "/ˈɛlɪf(ə)nt/", null, null),

            new(Guid.NewGuid(), vocabLessonId,
                "Bird",
                "A warm-blooded vertebrate with feathers, wings, and a beak.",
                "A small bird sang sweetly in the tree.",
                "/bɜːd/", null, null),
        ];

        // GrammarRules for "Present Simple"
        GrammarRule[] grammarRules =
        [
            new(Guid.NewGuid(), grammarLessonId,
                "Affirmative",
                "Subject + verb (add -s/-es for third person singular).",
                orderIndex: 1,
                examples: new List<string>
                {
                    "I eat breakfast every morning.",
                    "She eats breakfast at seven.",
                    "They play football on weekends.",
                }),

            new(Guid.NewGuid(), grammarLessonId,
                "Negative",
                "Subject + do not / does not + base verb.",
                orderIndex: 2,
                examples: new List<string>
                {
                    "I do not like coffee.",
                    "He does not speak French.",
                }),

            new(Guid.NewGuid(), grammarLessonId,
                "Question",
                "Do / Does + subject + base verb?",
                orderIndex: 3,
                examples: new List<string>
                {
                    "Do you speak English?",
                    "Does she work here?",
                    "Do they play chess?",
                }),
        ];

        // DailyPhrases for "Daily Greetings"
        DailyPhrase[] dailyPhrases =
        [
            new(Guid.NewGuid(), phraseLessonId,
                "Good morning!",
                "A greeting used when meeting someone in the morning.",
                "Used when greeting someone before noon.",
                null, null),

            new(Guid.NewGuid(), phraseLessonId,
                "How are you?",
                "A polite inquiry about someone's wellbeing.",
                "Used as a greeting when you meet someone you know.",
                null, null),

            new(Guid.NewGuid(), phraseLessonId,
                "Nice to meet you.",
                "Expressing pleasure at meeting someone for the first time.",
                "Used when being introduced to someone new.",
                null, null),

            new(Guid.NewGuid(), phraseLessonId,
                "See you later!",
                "A casual way to say goodbye.",
                "Used when parting from someone you expect to see again.",
                null, null),
        ];

        await context.VocabularyItems.AddRangeAsync(vocabItems, ct);
        await context.GrammarRules.AddRangeAsync(grammarRules, ct);
        await context.DailyPhrases.AddRangeAsync(dailyPhrases, ct);
        await context.SaveChangesAsync(ct);
    }
}
