using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Quiz.Application.Abstractions;
using WordBuddy.Quiz.Application.DTOs;
using WordBuddy.Quiz.Application.Features.Quizzes.Commands.CreateQuiz;
using WordBuddy.Quiz.Application.Features.Quizzes.Commands.SubmitQuizAnswer;
using WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizById;
using WordBuddy.Quiz.Application.Features.Quizzes.Queries.GetQuizzes;

namespace WordBuddy.Quiz.Application.Extensions;

/// <summary>Registers Quiz's Application-layer handlers and validators with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetQuizzesQuery, IReadOnlyList<QuizSummaryDto>>, GetQuizzesQueryHandler>();
        services.AddScoped<IQueryHandler<GetQuizByIdQuery, QuizDto>, GetQuizByIdQueryHandler>();

        services.AddScoped<ICommandHandler<SubmitQuizAnswerCommand, SubmitQuizAnswerResultDto>, SubmitQuizAnswerCommandHandler>();
        services.AddScoped<IValidator<SubmitQuizAnswerCommand>, SubmitQuizAnswerCommandValidator>();

        services.AddScoped<ICommandHandler<CreateQuizCommand, Guid>, CreateQuizCommandHandler>();
        services.AddScoped<IValidator<CreateQuizCommand>, CreateQuizCommandValidator>();

        return services;
    }
}
