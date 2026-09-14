using WordBuddy.Quiz.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Quiz.Application.Interfaces;

public interface IQuizRepository
{
    Task<Result<IReadOnlyList<Domain.Quiz>>> GetAllAsync(Guid? lessonId, CancellationToken ct = default);

    /// <summary>Returns a single quiz with its questions loaded, or <see cref="Error.NotFound"/>.</summary>
    Task<Result<Domain.Quiz>> GetByIdWithQuestionsAsync(Guid id, CancellationToken ct = default);

    Task<Result> AddAsync(Domain.Quiz quiz, CancellationToken ct = default);
}
