using WordBuddy.Content.Application.DTOs;
using WordBuddy.Shared.Application.Abstractions;

namespace WordBuddy.Content.Application.Features.LexemesAdmin.Queries.GetAdminLexemeDetail;

/// <summary>One lexeme for the admin editor: fields, audio and catalog senses.</summary>
public sealed record GetAdminLexemeDetailQuery(Guid LexemeId) : IQuery<AdminLexemeDetailDto>;
