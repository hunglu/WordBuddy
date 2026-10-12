using MassTransit;
using WordBuddy.Shared.Application.Abstractions;
using WordBuddy.Progress.Application.Features.Challenges.Commands.ApplyLearnerProfileEvent;
using WordBuddy.Shared.Contracts.Profiles;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Messaging;

/// <summary>Thin consumer: maps <see cref="LearnerProfileUpdated"/> to <see cref="ApplyLearnerProfileEventCommand"/>.
/// The EF inbox dedupes on <c>MessageId</c>; the handler is an idempotent upsert; a failed result throws so the retry policy runs.</summary>
public sealed class LearnerProfileUpdatedConsumer : IConsumer<LearnerProfileUpdated>
{
    private readonly ICommandHandler<ApplyLearnerProfileEventCommand> _handler;

    public LearnerProfileUpdatedConsumer(ICommandHandler<ApplyLearnerProfileEventCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LearnerProfileUpdated> context)
    {
        LearnerProfileUpdated message = context.Message;
        Result result = await _handler.HandleAsync(
            new ApplyLearnerProfileEventCommand(
                message.UserId, message.AgeGroup, message.Alias, message.AvatarId, message.DisplayName,
                message.TimeZoneId, message.IsGuardian, message.OccurredAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"ApplyLearnerProfileEvent failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}
