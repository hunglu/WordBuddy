using MassTransit;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordRemoved;
using WordBuddy.Shared.Contracts.Vocabulary;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Messaging;

/// <summary>Thin consumer: maps <see cref="LearnerWordRemoved"/> to <see cref="RecordLearnerWordRemovedCommand"/>.
/// The EF inbox dedupes on <c>MessageId</c>; a failed result throws so the retry policy runs.</summary>
public sealed class LearnerWordRemovedConsumer : IConsumer<LearnerWordRemoved>
{
    private readonly ICommandHandler<RecordLearnerWordRemovedCommand> _handler;

    public LearnerWordRemovedConsumer(ICommandHandler<RecordLearnerWordRemovedCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LearnerWordRemoved> context)
    {
        LearnerWordRemoved message = context.Message;
        Result result = await _handler.HandleAsync(
            new RecordLearnerWordRemovedCommand(message.UserId, message.SenseId, message.RemovedAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"RecordLearnerWordRemoved failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}
