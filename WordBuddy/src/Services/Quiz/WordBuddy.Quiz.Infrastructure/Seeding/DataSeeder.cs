using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WordBuddy.Quiz.Domain;
using WordBuddy.Quiz.Infrastructure.Persistence;

namespace WordBuddy.Quiz.Infrastructure.Seeding;

/// <summary>Seeds the Quiz database with 1 sample quiz. Invoked only in Development.</summary>
public static class DataSeeder
{
    public static async Task MigrateAndSeedAsync(IServiceProvider services)
    {
        using IServiceScope scope = services.CreateScope();
        QuizDbContext dbContext = scope.ServiceProvider.GetRequiredService<QuizDbContext>();
        ILogger logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DataSeeder");

        await dbContext.Database.MigrateAsync();

        if (await dbContext.Quizzes.AnyAsync())
        {
            logger.LogWarning("Quiz seed skipped: Quizzes table already has data");
            return;
        }

        logger.LogInformation("Seeding Quiz development data");

        // Not a real Content LessonId — Quiz has no cross-service reference to validate against,
        // this just demonstrates the service works standalone.
        Domain.Quiz quiz = new(
            Guid.NewGuid(), Guid.NewGuid(), "Present Simple Check",
            "A short quiz testing present simple tense.", Level.Beginner, AgeGroup.Adult);

        quiz.AddQuestion(new QuizQuestion(
            Guid.NewGuid(), quiz.Id, "She ___ to school every day.", QuizQuestionType.MultipleChoice,
            ["go", "goes", "going"], correctOptionIndex: 1, explanation: "Use 'goes' for third-person singular (she/he/it)."));
        quiz.AddQuestion(new QuizQuestion(
            Guid.NewGuid(), quiz.Id, "I do not like coffee.", QuizQuestionType.TrueFalse,
            ["True", "False"], correctOptionIndex: 0, explanation: "This is a correctly formed present simple negative sentence."));
        quiz.AddQuestion(new QuizQuestion(
            Guid.NewGuid(), quiz.Id, "___ you play tennis?", QuizQuestionType.MultipleChoice,
            ["Do", "Does", "Are"], correctOptionIndex: 0, explanation: "Use 'Do' for questions with I/you/we/they."));

        await dbContext.Quizzes.AddAsync(quiz);
        await dbContext.SaveChangesAsync();

        logger.LogInformation("Quiz seed complete: 1 quiz with 3 questions created");
    }
}
