using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;
using WordBuddy.Content.Application.Features.SupportLinks;

namespace WordBuddy.Content.Api.Authorization;

/// <summary>Support-link policy names.</summary>
public static class SupportLinkPolicies
{
    /// <summary>Caller (<c>sub</c>) has an active support link to the route <c>{learnerId}</c>.</summary>
    public const string CanSupportLearner = "CanSupportLearner";

    /// <summary>A child caller needs at least one active supporter. Adults and admins pass.</summary>
    public const string ChildHasSupporter = "ChildHasSupporter";

    /// <summary>Problem title returned with 403 when <see cref="ChildHasSupporter"/> fails.</summary>
    public const string SupporterRequiredCode = "Learner.SupporterRequired";

    /// <summary>Route value read by <see cref="CanSupportLearner"/>.</summary>
    public const string LearnerIdRouteKey = "learnerId";

    /// <summary>Adds both policies.</summary>
    public static AuthorizationOptions AddSupportLinkPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(CanSupportLearner, policy => policy.RequireAuthenticatedUser().AddRequirements(new CanSupportLearnerRequirement()));
        options.AddPolicy(ChildHasSupporter, policy => policy.RequireAuthenticatedUser().AddRequirements(new ChildHasSupporterRequirement()));
        return options;
    }

    /// <summary>Registers the handlers (scoped: they use the projection repository) and the 403 problem writer.</summary>
    public static IServiceCollection AddSupportLinkAuthorizationHandlers(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<SupportAccess>();
        services.AddScoped<IAuthorizationHandler, CanSupportLearnerHandler>();
        services.AddScoped<IAuthorizationHandler, ChildHasSupporterHandler>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, SupporterRequiredResultHandler>();
        return services;
    }

    internal static Guid? GetSubject(ClaimsPrincipal user)
    {
        string? subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(subject, out Guid id) ? id : null;
    }
}

/// <summary>Requirement of <see cref="SupportLinkPolicies.CanSupportLearner"/>.</summary>
public sealed class CanSupportLearnerRequirement : IAuthorizationRequirement
{
}

/// <summary>Requirement of <see cref="SupportLinkPolicies.ChildHasSupporter"/>.</summary>
public sealed class ChildHasSupporterRequirement : IAuthorizationRequirement
{
}

/// <summary>Succeeds when the caller has an active projection link to the route learner. Inactive or missing link → deny.</summary>
internal sealed class CanSupportLearnerHandler : AuthorizationHandler<CanSupportLearnerRequirement>
{
    private readonly SupportAccess _access;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CanSupportLearnerHandler(SupportAccess access, IHttpContextAccessor httpContextAccessor)
    {
        _access = access;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, CanSupportLearnerRequirement requirement)
    {
        Guid? callerId = SupportLinkPolicies.GetSubject(context.User);
        object? routeValue = _httpContextAccessor.HttpContext?.GetRouteValue(SupportLinkPolicies.LearnerIdRouteKey);

        Guid? learnerId = Guid.TryParse(routeValue?.ToString(), out Guid parsed) ? parsed : null;

        if (await _access.CanSupportLearnerAsync(callerId, learnerId))
        {
            context.Succeed(requirement);
        }
    }
}

/// <summary>Adults and admins pass; a child passes only with at least one active supporter.</summary>
internal sealed class ChildHasSupporterHandler : AuthorizationHandler<ChildHasSupporterRequirement>
{
    private readonly SupportAccess _access;

    public ChildHasSupporterHandler(SupportAccess access)
    {
        _access = access;
    }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, ChildHasSupporterRequirement requirement)
    {
        bool isAdmin = string.Equals(context.User.FindFirstValue("is_admin"), "true", StringComparison.OrdinalIgnoreCase);
        bool isChild = string.Equals(context.User.FindFirstValue("age_group"), "Child", StringComparison.OrdinalIgnoreCase);

        if (await _access.ChildHasSupporterAsync(SupportLinkPolicies.GetSubject(context.User), isChild, isAdmin))
        {
            context.Succeed(requirement);
            return;
        }

        context.Fail(new AuthorizationFailureReason(this, SupportLinkPolicies.SupporterRequiredCode));
    }
}

/// <summary>Writes a 403 problem with title <c>Learner.SupporterRequired</c> when the child gate fails, so the UI can show the "Add a supporter" screen.</summary>
internal sealed class SupporterRequiredResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _default = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult authorizeResult)
    {
        // context.Fail(reason) leaves FailedRequirements empty, so check the reason too.
        AuthorizationFailure? failure = authorizeResult.AuthorizationFailure;
        bool supporterMissing = authorizeResult.Forbidden && failure is not null
            && (failure.FailedRequirements.OfType<ChildHasSupporterRequirement>().Any()
                || failure.FailureReasons.Any(r => r.Message == SupportLinkPolicies.SupporterRequiredCode));

        if (!supporterMissing)
        {
            await _default.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(
            new ProblemDetails
            {
                Status = StatusCodes.Status403Forbidden,
                Title = SupportLinkPolicies.SupporterRequiredCode,
                Detail = "A child account needs an active supporter before using learning features.",
            },
            options: null,
            contentType: "application/problem+json");
    }
}
