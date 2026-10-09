using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;

/// <summary>Adds an auto-filled sense to the caller's list. Caller id and age group come from the JWT.</summary>
public sealed record AddAutofillSenseToMyListCommand(Guid SenseId, Guid RequestingUserId, AgeGroup RequestingAgeGroup) : ICommand<Guid>;
