using System.Globalization;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace WordBuddy.Progress.Api.Extensions;

/// <summary>Per-endpoint rate-limit policies. Rejections return <c>429</c> with <c>Retry-After</c>.</summary>
internal static class RateLimitingConfiguration
{
    /// <summary>Policy for <c>POST /api/progress/vocabulary/reviews</c> — strict, like quiz submission.</summary>
    public const string VocabularyReviewPolicy = "vocabulary-review";

    /// <summary>Answers per user per window.</summary>
    private const int VocabularyReviewPermitLimit = 60;

    private static readonly TimeSpan VocabularyReviewWindow = TimeSpan.FromMinutes(1);

    /// <summary>Registers the rate limiter and its policies.</summary>
    public static IServiceCollection AddWordBuddyRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Partition by authenticated user (sub), falling back to the client IP.
            options.AddPolicy(VocabularyReviewPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                        ?? context.Connection.RemoteIpAddress?.ToString()
                        ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = VocabularyReviewPermitLimit,
                        Window = VocabularyReviewWindow,
                        QueueLimit = 0,
                    }));

            options.OnRejected = (context, _) =>
            {
                TimeSpan retryAfter = context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan value)
                    ? value
                    : VocabularyReviewWindow;
                context.HttpContext.Response.Headers.RetryAfter =
                    ((int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
                return ValueTask.CompletedTask;
            };
        });

        return services;
    }
}
