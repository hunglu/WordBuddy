using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;

namespace WordBuddy.Content.Application.Features.Autofill.Queries.LookupAutofill;

/// <summary>Auto-fill lookup of a typed word. <paramref name="RequestingUserId"/> and
/// <paramref name="RequestingAgeGroup"/> come from the JWT. A Child gets unapproved senses without content.</summary>
public sealed record LookupAutofillQuery(string Word, Guid RequestingUserId, AgeGroup RequestingAgeGroup) : IQuery<AutofillResultDto>;
