using Microsoft.Extensions.Logging;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Identity.Application.Interfaces;
using WordBuddy.Identity.Domain;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Features.Profile.Commands.RepublishLearnerProfiles;

/// <summary>
/// Publishes one snapshot per user in pages. Consumers upsert and ignore older events, so a rerun
/// only refreshes data. Returns the number of events staged.
/// </summary>
public sealed class RepublishLearnerProfilesCommandHandler : ICommandHandler<RepublishLearnerProfilesCommand, int>
{
    private const int PageSize = 200;

    private readonly IUserRepository _users;
    private readonly ILearnerProfileEventPublisher _profileEvents;
    private readonly TimeProvider _time;
    private readonly ILogger<RepublishLearnerProfilesCommandHandler> _logger;

    public RepublishLearnerProfilesCommandHandler(
        IUserRepository users,
        ILearnerProfileEventPublisher profileEvents,
        TimeProvider time,
        ILogger<RepublishLearnerProfilesCommandHandler> logger)
    {
        _users = users;
        _profileEvents = profileEvents;
        _time = time;
        _logger = logger;
    }

    public async Task<Result<int>> HandleAsync(RepublishLearnerProfilesCommand command, CancellationToken ct = default)
    {
        _logger.LogInformation("RepublishLearnerProfilesCommand started");

        int published = 0;
        int skip = 0;
        while (true)
        {
            IReadOnlyList<User> page = await _users.GetPageAsync(skip, PageSize, ct);
            if (page.Count == 0)
            {
                break;
            }

            DateTime now = _time.GetUtcNow().UtcDateTime;
            foreach (User user in page)
            {
                await _profileEvents.PublishAsync(user, now, ct);
            }

            Result save = await _users.SaveChangesAsync(ct);
            if (save.IsFailure)
            {
                _logger.LogWarning("RepublishLearnerProfilesCommand failed to save: ErrorCode={ErrorCode}", save.Error.Code);
                return Result.Failure<int>(save.Error);
            }

            published += page.Count;
            skip += page.Count;
        }

        _logger.LogInformation("RepublishLearnerProfilesCommand succeeded: Published={Published}", published);
        return Result.Success(published);
    }
}
