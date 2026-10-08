using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Identity.Application.Abstractions;
using WordBuddy.Identity.Application.DTOs;
using WordBuddy.Identity.Application.Features.Auth.Commands.Login;
using WordBuddy.Identity.Application.Features.Auth.Commands.RegisterUser;
using WordBuddy.Identity.Application.Features.Profile.Commands.UpdateProfileAliasAvatar;
using WordBuddy.Identity.Application.Features.Profile.Queries.GetCurrentUser;
using WordBuddy.Identity.Application.Features.SupportLinks;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AcceptInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminCompleteUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminHandoverPrimary;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.AdminRejectUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CancelUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.CreateInvitation;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.EscalateUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RequestUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondToPendingSupporter;
using WordBuddy.Identity.Application.Features.SupportLinks.Commands.RespondUnlink;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetLearnerSupportLinksForAdmin;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetMySupportLinks;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetPendingSupporterApprovals;
using WordBuddy.Identity.Application.Features.SupportLinks.Queries.GetUnlinkRequestsForAdmin;

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

        // Profile
        services.AddScoped<ICommandHandler<UpdateProfileAliasAvatarCommand, UserDto>, UpdateProfileAliasAvatarCommandHandler>();
        services.AddScoped<IValidator<UpdateProfileAliasAvatarCommand>, UpdateProfileAliasAvatarCommandValidator>();
        services.AddScoped<IQueryHandler<GetCurrentUserQuery, UserDto>, GetCurrentUserQueryHandler>();

        // Support links
        services.AddScoped<LinkActorResolver>();

        services.AddScoped<ICommandHandler<CreateInvitationCommand, CreatedInvitationDto>, CreateInvitationCommandHandler>();
        services.AddScoped<IValidator<CreateInvitationCommand>, CreateInvitationCommandValidator>();
        services.AddScoped<ICommandHandler<AcceptInvitationCommand, SupportLinkDto>, AcceptInvitationCommandHandler>();
        services.AddScoped<IValidator<AcceptInvitationCommand>, AcceptInvitationCommandValidator>();
        services.AddScoped<ICommandHandler<CancelInvitationCommand>, CancelInvitationCommandHandler>();
        services.AddScoped<IValidator<CancelInvitationCommand>, CancelInvitationCommandValidator>();
        services.AddScoped<ICommandHandler<RespondToPendingSupporterCommand>, RespondToPendingSupporterCommandHandler>();
        services.AddScoped<IValidator<RespondToPendingSupporterCommand>, RespondToPendingSupporterCommandValidator>();

        services.AddScoped<ICommandHandler<RequestUnlinkCommand>, RequestUnlinkCommandHandler>();
        services.AddScoped<IValidator<RequestUnlinkCommand>, RequestUnlinkCommandValidator>();
        services.AddScoped<ICommandHandler<RespondUnlinkCommand>, RespondUnlinkCommandHandler>();
        services.AddScoped<IValidator<RespondUnlinkCommand>, RespondUnlinkCommandValidator>();
        services.AddScoped<ICommandHandler<CancelUnlinkCommand>, CancelUnlinkCommandHandler>();
        services.AddScoped<IValidator<CancelUnlinkCommand>, CancelUnlinkCommandValidator>();
        services.AddScoped<ICommandHandler<EscalateUnlinkCommand>, EscalateUnlinkCommandHandler>();
        services.AddScoped<IValidator<EscalateUnlinkCommand>, EscalateUnlinkCommandValidator>();

        services.AddScoped<ICommandHandler<AdminCompleteUnlinkCommand>, AdminCompleteUnlinkCommandHandler>();
        services.AddScoped<IValidator<AdminCompleteUnlinkCommand>, AdminCompleteUnlinkCommandValidator>();
        services.AddScoped<ICommandHandler<AdminRejectUnlinkCommand>, AdminRejectUnlinkCommandHandler>();
        services.AddScoped<IValidator<AdminRejectUnlinkCommand>, AdminRejectUnlinkCommandValidator>();
        services.AddScoped<ICommandHandler<AdminHandoverPrimaryCommand>, AdminHandoverPrimaryCommandHandler>();
        services.AddScoped<IValidator<AdminHandoverPrimaryCommand>, AdminHandoverPrimaryCommandValidator>();

        services.AddScoped<IQueryHandler<GetMySupportLinksQuery, MySupportLinksDto>, GetMySupportLinksQueryHandler>();
        services.AddScoped<IQueryHandler<GetPendingSupporterApprovalsQuery, IReadOnlyList<SupportLinkDto>>, GetPendingSupporterApprovalsQueryHandler>();
        services.AddScoped<IQueryHandler<GetUnlinkRequestsForAdminQuery, IReadOnlyList<AdminUnlinkRequestDto>>, GetUnlinkRequestsForAdminQueryHandler>();
        services.AddScoped<IQueryHandler<GetLearnerSupportLinksForAdminQuery, IReadOnlyList<AdminSupportLinkDto>>, GetLearnerSupportLinksForAdminQueryHandler>();

        return services;
    }
}
