using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemes;

/// <summary>Pages lexemes for the admin list. <paramref name="Search"/> matches any part of the word.</summary>
public sealed record GetAdminLexemesQuery(
    string? Search,
    PartOfSpeech? PartOfSpeech,
    CefrLevel? CefrLevel,
    int Page = 1,
    int PageSize = GetAdminLexemesQuery.DefaultPageSize) : IQuery<AdminLexemePageDto>
{
    /// <summary>Page size when the caller gives none.</summary>
    public const int DefaultPageSize = 25;

    /// <summary>Largest allowed page size.</summary>
    public const int MaxPageSize = 100;
}
