using MassTransit;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.SupportLinks.Commands.ApplySupportLinkEvent;
using WordBuddy.Shared.Contracts.SupportLinks;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Messaging;

/// <summary>Thin consumer: maps <see cref="SupportLinkActivated"/> to <see cref="ApplySupportLinkEventCommand"/>.
/// The EF inbox dedupes on <c>MessageId</c>; a failed result throws so the retry policy runs.</summary>
public sealed class SupportLinkActivatedConsumer : IConsumer<SupportLinkActivated>
{
    private readonly ICommandHandler<ApplySupportLinkEventCommand> _handler;

    public SupportLinkActivatedConsumer(ICommandHandler<ApplySupportLinkEventCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SupportLinkActivated> context)
    {
        SupportLinkActivated message = context.Message;
        Result result = await _handler.HandleAsync(
            new ApplySupportLinkEventCommand(message.LinkId, message.LearnerId, message.SupporterId, IsActive: true, message.OccurredAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"ApplySupportLinkEvent failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}

/// <summary>Thin consumer: maps <see cref="SupportLinkRevoked"/> to <see cref="ApplySupportLinkEventCommand"/>.</summary>
public sealed class SupportLinkRevokedConsumer : IConsumer<SupportLinkRevoked>
{
    private readonly ICommandHandler<ApplySupportLinkEventCommand> _handler;

    public SupportLinkRevokedConsumer(ICommandHandler<ApplySupportLinkEventCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<SupportLinkRevoked> context)
    {
        SupportLinkRevoked message = context.Message;
        Result result = await _handler.HandleAsync(
            new ApplySupportLinkEventCommand(message.LinkId, message.LearnerId, message.SupporterId, IsActive: false, message.OccurredAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException($"ApplySupportLinkEvent failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}
