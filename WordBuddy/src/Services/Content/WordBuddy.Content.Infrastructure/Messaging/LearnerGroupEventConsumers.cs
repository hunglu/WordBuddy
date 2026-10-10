using MassTransit;
using WordBuddy.Content.Application.Abstractions;
using WordBuddy.Content.Application.Features.Groups.Commands.ApplyLearnerGroupEvent;
using WordBuddy.Shared.Contracts.Groups;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.Infrastructure.Messaging;

/// <summary>Thin consumer: maps <see cref="LearnerGroupMemberActivated"/> to <see cref="ApplyLearnerGroupEventCommand"/>.
/// The EF inbox dedupes on <c>MessageId</c>; a failed result throws so the retry policy runs.</summary>
public sealed class LearnerGroupMemberActivatedConsumer : IConsumer<LearnerGroupMemberActivated>
{
    private readonly ICommandHandler<ApplyLearnerGroupEventCommand> _handler;

    public LearnerGroupMemberActivatedConsumer(ICommandHandler<ApplyLearnerGroupEventCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LearnerGroupMemberActivated> context)
    {
        LearnerGroupMemberActivated message = context.Message;
        Result result = await _handler.HandleAsync(
            new ApplyLearnerGroupEventCommand(message.GroupId, message.OwnerId, message.LearnerId, IsActive: true, message.OccurredAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"ApplyLearnerGroupEvent failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}

/// <summary>Thin consumer: maps <see cref="LearnerGroupMemberRemoved"/> to <see cref="ApplyLearnerGroupEventCommand"/>.</summary>
public sealed class LearnerGroupMemberRemovedConsumer : IConsumer<LearnerGroupMemberRemoved>
{
    private readonly ICommandHandler<ApplyLearnerGroupEventCommand> _handler;

    public LearnerGroupMemberRemovedConsumer(ICommandHandler<ApplyLearnerGroupEventCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LearnerGroupMemberRemoved> context)
    {
        LearnerGroupMemberRemoved message = context.Message;
        Result result = await _handler.HandleAsync(
            new ApplyLearnerGroupEventCommand(message.GroupId, message.OwnerId, message.LearnerId, IsActive: false, message.OccurredAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"ApplyLearnerGroupEvent failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}

/// <summary>Thin consumer: maps <see cref="LearnerGroupDeleted"/> to a whole-group <see cref="ApplyLearnerGroupEventCommand"/>.</summary>
public sealed class LearnerGroupDeletedConsumer : IConsumer<LearnerGroupDeleted>
{
    private readonly ICommandHandler<ApplyLearnerGroupEventCommand> _handler;

    public LearnerGroupDeletedConsumer(ICommandHandler<ApplyLearnerGroupEventCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LearnerGroupDeleted> context)
    {
        LearnerGroupDeleted message = context.Message;
        Result result = await _handler.HandleAsync(
            new ApplyLearnerGroupEventCommand(message.GroupId, message.OwnerId, LearnerId: null, IsActive: false, message.OccurredAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"ApplyLearnerGroupEvent failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}
