using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Auth.Commands.Login;
using WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;

namespace WordBuddy.Identity.Application.Extensions;

/// <summary>Registers Identity's Application-layer handlers and validators with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<RegisterUserCommand, AuthTokenDto>, RegisterUserCommandHandler>();
        services.AddScoped<IValidator<RegisterUserCommand>, RegisterUserCommandValidator>();

        services.AddScoped<ICommandHandler<LoginCommand, AuthTokenDto>, LoginCommandHandler>();
        services.AddScoped<IValidator<LoginCommand>, LoginCommandValidator>();

        return services;
    }
}
