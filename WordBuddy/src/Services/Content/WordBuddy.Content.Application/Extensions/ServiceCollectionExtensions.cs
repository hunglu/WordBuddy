using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Application.Features.Autofill;
using WordBuddy.Content.Application.Features.Autofill.Commands.AddAutofillSenseToMyList;
using WordBuddy.Content.Application.Features.Autofill.Commands.ApproveChildWord;
using WordBuddy.Content.Application.Features.Autofill.Queries.GetPendingChildApprovals;
using WordBuddy.Content.Application.Features.Autofill.Queries.LookupAutofill;
using WordBuddy.Content.Application.Features.Lessons.Commands.AttachLessonSense;
using WordBuddy.Content.Application.Features.Lessons.Commands.CreateLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.DeleteLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.DetachLessonSense;
using WordBuddy.Content.Application.Features.Lessons.Commands.PublishLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.ReorderLessonSenses;
using WordBuddy.Content.Application.Features.Lessons.Commands.UnpublishLesson;
using WordBuddy.Content.Application.Features.Lessons.Commands.UpdateLesson;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetAdminLessons;
using WordBuddy.Content.Application.Features.Lessons.Queries.SearchLessonSenses;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessonDetail;
using WordBuddy.Content.Application.Features.Lessons.Queries.GetLessons;
using WordBuddy.Content.Application.Features.Media.Commands.UploadMedia;
using WordBuddy.Content.Application.Features.Media.Queries.GetMediaAsset;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddPersonalVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.AddSharedVocabularyWordToMyList;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.DeletePersonalVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.ModerateSharedVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RepublishLearnerWords;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Commands.RequestShareVocabularyWord;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetMyVocabularyWords;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetPendingVocabularyModeration;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetRandomVocabularyWordsForCheck;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSensesByIds;
using WordBuddy.Content.Application.Features.PersonalVocabulary.Queries.GetSharedVocabularyWords;
using WordBuddy.Content.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;
using WordBuddy.Content.Application.Features.Groups.Commands.AssignWordsToGroup;
using WordBuddy.Content.Application.Features.Groups.Queries.GetGroupWordAssignments;
using WordBuddy.Content.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;

namespace WordBuddy.Content.Application.Extensions;

/// <summary>Registers Content's Application-layer handlers and validators with the DI container.</summary>
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IQueryHandler<GetLessonsQuery, IReadOnlyList<LessonDto>>, GetLessonsQueryHandler>();
        services.AddScoped<IQueryHandler<GetLessonDetailQuery, LessonDetailDto>, GetLessonDetailQueryHandler>();

        services.AddScoped<ICommandHandler<CreateLessonCommand, Guid>, CreateLessonCommandHandler>();
        services.AddScoped<IValidator<CreateLessonCommand>, CreateLessonCommandValidator>();

        services.AddScoped<ICommandHandler<UpdateLessonCommand>, UpdateLessonCommandHandler>();
        services.AddScoped<IValidator<UpdateLessonCommand>, UpdateLessonCommandValidator>();
        services.AddScoped<ICommandHandler<PublishLessonCommand>, PublishLessonCommandHandler>();
        services.AddScoped<ICommandHandler<UnpublishLessonCommand>, UnpublishLessonCommandHandler>();
        services.AddScoped<ICommandHandler<DeleteLessonCommand>, DeleteLessonCommandHandler>();
        services.AddScoped<ICommandHandler<AttachLessonSenseCommand>, AttachLessonSenseCommandHandler>();
        services.AddScoped<IValidator<AttachLessonSenseCommand>, AttachLessonSenseCommandValidator>();
        services.AddScoped<ICommandHandler<DetachLessonSenseCommand>, DetachLessonSenseCommandHandler>();
        services.AddScoped<ICommandHandler<ReorderLessonSensesCommand>, ReorderLessonSensesCommandHandler>();
        services.AddScoped<IValidator<ReorderLessonSensesCommand>, ReorderLessonSensesCommandValidator>();
        services.AddScoped<IQueryHandler<GetAdminLessonsQuery, IReadOnlyList<AdminLessonDto>>, GetAdminLessonsQueryHandler>();
        services.AddScoped<IQueryHandler<GetAdminLessonDetailQuery, AdminLessonDetailDto>, GetAdminLessonDetailQueryHandler>();
        services.AddScoped<IQueryHandler<SearchLessonSensesQuery, IReadOnlyList<AdminSenseSearchResultDto>>, SearchLessonSensesQueryHandler>();
        services.AddScoped<IValidator<SearchLessonSensesQuery>, SearchLessonSensesQueryValidator>();

        services.AddScoped<ICommandHandler<UploadMediaCommand, MediaAssetDto>, UploadMediaCommandHandler>();
        services.AddScoped<IValidator<UploadMediaCommand>, UploadMediaCommandValidator>();

        services.AddScoped<IQueryHandler<GetMediaAssetQuery, MediaAssetDto>, GetMediaAssetQueryHandler>();

        services.AddScoped<ICommandHandler<AddPersonalVocabularyWordCommand, Guid>, AddPersonalVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<AddPersonalVocabularyWordCommand>, AddPersonalVocabularyWordCommandValidator>();

        services.AddScoped<ICommandHandler<RequestShareVocabularyWordCommand>, RequestShareVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<RequestShareVocabularyWordCommand>, RequestShareVocabularyWordCommandValidator>();

        services.AddScoped<ICommandHandler<ModerateSharedVocabularyWordCommand>, ModerateSharedVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<ModerateSharedVocabularyWordCommand>, ModerateSharedVocabularyWordCommandValidator>();

        services.AddScoped<ICommandHandler<AddSharedVocabularyWordToMyListCommand, Guid>, AddSharedVocabularyWordToMyListCommandHandler>();
        services.AddScoped<IValidator<AddSharedVocabularyWordToMyListCommand>, AddSharedVocabularyWordToMyListCommandValidator>();

        services.AddScoped<ICommandHandler<DeletePersonalVocabularyWordCommand>, DeletePersonalVocabularyWordCommandHandler>();
        services.AddScoped<IValidator<DeletePersonalVocabularyWordCommand>, DeletePersonalVocabularyWordCommandValidator>();

        services.AddScoped<IQueryHandler<GetMyVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetMyVocabularyWordsQueryHandler>();
        services.AddScoped<IQueryHandler<GetSharedVocabularyWordsQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetSharedVocabularyWordsQueryHandler>();

        services.AddScoped<IQueryHandler<GetRandomVocabularyWordsForCheckQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetRandomVocabularyWordsForCheckQueryHandler>();
        services.AddScoped<IValidator<GetRandomVocabularyWordsForCheckQuery>, GetRandomVocabularyWordsForCheckQueryValidator>();

        services.AddScoped<IQueryHandler<GetSensesByIdsQuery, IReadOnlyList<SenseReviewDto>>, GetSensesByIdsQueryHandler>();
        services.AddScoped<IValidator<GetSensesByIdsQuery>, GetSensesByIdsQueryValidator>();

        services.AddScoped<IQueryHandler<GetPendingVocabularyModerationQuery, IReadOnlyList<PersonalVocabularyWordDto>>, GetPendingVocabularyModerationQueryHandler>();

        services.AddScoped<ICommandHandler<RepublishLearnerWordsCommand, RepublishLearnerWordsResult>, RepublishLearnerWordsCommandHandler>();
        services.AddScoped<IValidator<RepublishLearnerWordsCommand>, RepublishLearnerWordsCommandValidator>();

        services.AddScoped<ICommandHandler<ApplySupportLinkEventCommand>, ApplySupportLinkEventCommandHandler>();
        services.AddScoped<IValidator<ApplySupportLinkEventCommand>, ApplySupportLinkEventCommandValidator>();

        services.AddScoped<ICommandHandler<ApplyLearnerGroupEventCommand>, ApplyLearnerGroupEventCommandHandler>();
        services.AddScoped<IValidator<ApplyLearnerGroupEventCommand>, ApplyLearnerGroupEventCommandValidator>();
        services.AddScoped<ICommandHandler<AssignWordsToGroupCommand, GroupWordAssignmentResultDto>, AssignWordsToGroupCommandHandler>();
        services.AddScoped<IValidator<AssignWordsToGroupCommand>, AssignWordsToGroupCommandValidator>();
        services.AddScoped<IQueryHandler<GetGroupWordAssignmentsQuery, IReadOnlyList<GroupWordAssignmentDto>>, GetGroupWordAssignmentsQueryHandler>();

        services.AddScoped<AutofillCatalogWriter>();
        services.AddScoped<IQueryHandler<LookupAutofillQuery, AutofillResultDto>, LookupAutofillQueryHandler>();
        services.AddScoped<IValidator<LookupAutofillQuery>, LookupAutofillQueryValidator>();

        services.AddScoped<ICommandHandler<AddAutofillSenseToMyListCommand, Guid>, AddAutofillSenseToMyListCommandHandler>();
        services.AddScoped<IValidator<AddAutofillSenseToMyListCommand>, AddAutofillSenseToMyListCommandValidator>();

        services.AddScoped<IQueryHandler<GetPendingChildApprovalsQuery, IReadOnlyList<ChildApprovalDto>>, GetPendingChildApprovalsQueryHandler>();

        services.AddScoped<ICommandHandler<ApproveChildWordCommand>, ApproveChildWordCommandHandler>();
        services.AddScoped<IValidator<ApproveChildWordCommand>, ApproveChildWordCommandValidator>();

        return services;
    }
}
