using Microsoft.AspNetCore.Authorization;
using WordBuddy.Progress.Application.Features.Groups;

namespace WordBuddy.Progress.Api.Authorization;

/// <summary>Group policy names.</summary>
public static class GroupPolicies
{
    /// <summary>Caller (<c>sub</c>) owns the route <c>{groupId}</c> according to the group projection.</summary>
    public const string CanManageGroup = "CanManageGroup";

    /// <summary>Route value read by <see cref="CanManageGroup"/>.</summary>
    public const string GroupIdRouteKey = "groupId";

    /// <summary>Adds the group policies.</summary>
    public static AuthorizationOptions AddGroupPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(CanManageGroup, policy => policy.RequireAuthenticatedUser().AddRequirements(new CanManageGroupRequirement()));
        return options;
    }

    /// <summary>Registers the handler (scoped: it uses the projection repository).</summary>
    public static IServiceCollection AddGroupAuthorizationHandlers(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<GroupAccess>();
        services.AddScoped<IAuthorizationHandler, CanManageGroupHandler>();
        return services;
    }
}

/// <summary>Requirement of <see cref="GroupPolicies.CanManageGroup"/>.</summary>
public sealed class CanManageGroupRequirement : IAuthorizationRequirement
{
}

/// <summary>Succeeds when the caller owns the route group. Unknown group or other owner → deny (403).</summary>
internal sealed class CanManageGroupHandler : AuthorizationHandler<CanManageGroupRequirement>
{
    private readonly GroupAccess _access;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CanManageGroupHandler(GroupAccess access, IHttpContextAccessor httpContextAccessor)
    {
        _access = access;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanManageGroupRequirement requirement)
    {
        Guid? callerId = SupportLinkPolicies.GetSubject(context.User);
        object? routeValue = _httpContextAccessor.HttpContext?.GetRouteValue(GroupPolicies.GroupIdRouteKey);
        Guid? groupId = Guid.TryParse(routeValue?.ToString(), out Guid parsed) ? parsed : null;

        if (await _access.CanManageGroupAsync(callerId, groupId))
        {
            context.Succeed(requirement);
        }
    }
}
