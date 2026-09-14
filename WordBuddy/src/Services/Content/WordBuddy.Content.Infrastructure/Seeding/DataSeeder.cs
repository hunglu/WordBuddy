using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.Infrastructure.Seeding;

/// <summary>Seeds the Content database with 3 sample lessons (one per <see cref="LessonType"/>). Invoked only in Development.</summary>
public static class DataSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        ContentDbContext dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        await dbContext.Database.MigrateAsync();

        if (await dbContext.Lessons.AnyAsync())
        {
            logger.LogWarning("Content seed skipped: Lessons table already has data");
            return;
        }

        logger.LogInformation("Seeding Content development data");

        Lesson vocabularyLesson = new(
            Guid.NewGuid(), "Animals", "Learn the names of common animals.",
            LessonType.Vocabulary, Level.Beginner, AgeGroup.Child);
        vocabularyLesson.AddVocabularyItem(new VocabularyItem(Guid.NewGuid(), vocabularyLesson.Id, "Dog", "A common domesticated animal.", "The dog barked loudly."));
        vocabularyLesson.AddVocabularyItem(new VocabularyItem(Guid.NewGuid(), vocabularyLesson.Id, "Cat", "A small domesticated feline.", "The cat is sleeping."));
        vocabularyLesson.AddVocabularyItem(new VocabularyItem(Guid.NewGuid(), vocabularyLesson.Id, "Bird", "A warm-blooded egg-laying animal with wings.", "The bird flew away."));
        vocabularyLesson.AddVocabularyItem(new VocabularyItem(Guid.NewGuid(), vocabularyLesson.Id, "Fish", "A cold-blooded animal that lives in water.", "The fish swam in the pond."));

        Lesson grammarLesson = new(
            Guid.NewGuid(), "Present Simple", "Learn how to use the present simple tense.",
            LessonType.Grammar, Level.Beginner, AgeGroup.Adult);
        grammarLesson.AddGrammarRule(new GrammarRule(Guid.NewGuid(), grammarLesson.Id, "Affirmative", "Subject + base verb (+s for he/she/it).", ["I play.", "She plays."]));
        grammarLesson.AddGrammarRule(new GrammarRule(Guid.NewGuid(), grammarLesson.Id, "Negative", "Subject + do/does not + base verb.", ["I do not play.", "She does not play."]));
        grammarLesson.AddGrammarRule(new GrammarRule(Guid.NewGuid(), grammarLesson.Id, "Question", "Do/Does + subject + base verb?", ["Do you play?", "Does she play?"]));

        Lesson dailyPhraseLesson = new(
            Guid.NewGuid(), "Daily Greetings", "Common phrases for greeting people.",
            LessonType.DailyPhrase, Level.Beginner, AgeGroup.Child);
        dailyPhraseLesson.AddDailyPhrase(new DailyPhrase(Guid.NewGuid(), dailyPhraseLesson.Id, "Good morning!", "A greeting used in the morning."));
        dailyPhraseLesson.AddDailyPhrase(new DailyPhrase(Guid.NewGuid(), dailyPhraseLesson.Id, "How are you?", "A common question asking about wellbeing."));
        dailyPhraseLesson.AddDailyPhrase(new DailyPhrase(Guid.NewGuid(), dailyPhraseLesson.Id, "Nice to meet you.", "Said when meeting someone for the first time."));
        dailyPhraseLesson.AddDailyPhrase(new DailyPhrase(Guid.NewGuid(), dailyPhraseLesson.Id, "See you later!", "A casual way to say goodbye."));

        await dbContext.Lessons.AddRangeAsync(vocabularyLesson, grammarLesson, dailyPhraseLesson);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Content seed complete: 3 lessons created");
    }
}
