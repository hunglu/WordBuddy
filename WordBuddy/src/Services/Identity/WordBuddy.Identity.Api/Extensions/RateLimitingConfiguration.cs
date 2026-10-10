using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace WordBuddy.Identity.Api.Extensions;

/// <summary>Per-endpoint rate-limit policies. Rejections return <c>429</c> with <c>Retry-After</c>.</summary>
internal static class RateLimitingConfiguration
{
    /// <summary>Policy for creating and accepting support-link invitations (guessing codes must stay slow).</summary>
    public const string SupportLinkInvitationPolicy = "support-link-invitation";

    /// <summary>Requests per user per window.</summary>
    private const int SupportLinkInvitationPermitLimit = 10;

    private static readonly TimeSpan SupportLinkInvitationWindow = TimeSpan.FromMinutes(1);

    /// <summary>Policy for group writes (create, rename, delete, add and remove members).</summary>
    public const string GroupWritePolicy = "group-write";

    private const int GroupWritePermitLimit = 30;

    private static readonly TimeSpan GroupWriteWindow = TimeSpan.FromMinutes(1);

    /// <summary>Registers the rate limiter and its policies.</summary>
    public static IServiceCollection AddWordBuddyRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partition by authenticated user (sub), falling back to the client IP.
            options.AddPolicy(SupportLinkInvitationPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = SupportLinkInvitationPermitLimit,
                        Window = SupportLinkInvitationWindow,
                        QueueLimit = 0,
                    }));

            options.AddPolicy(GroupWritePolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = GroupWritePermitLimit,
                        Window = GroupWriteWindow,
                        QueueLimit = 0,
                    }));

            options.OnRejected = (context, _) =>
            {
                TimeSpan retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan value)
                    ? value
                    : SupportLinkInvitationWindow;
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
