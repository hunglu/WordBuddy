namespace WordBuddy.Content.Application.DTOs;

public sealed record GrammarRuleDto(Guid Id, string Title, string Explanation, IReadOnlyList<string> Examples);
