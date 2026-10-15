using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemes;

/// <summary>Pages lexemes for the admin list. <paramref name="Search"/> matches any part of the word.</summary>
public sealed record GetAdminLexemesQuery(
    string? Search,
    PartOfSpeech? PartOfSpeech,
    CefrLevel? CefrLevel,
    int Page = 1,
    int PageSize = Paging.DefaultPageSize) : IQuery<PagedResult<AdminLexemeListItemDto>>;
