using MassTransit;
using WordBuddy.Progress.Application.Abstractions;
using WordBuddy.Progress.Application.Features.LearnerWords.Commands.RecordLearnerWordAdded;
using WordBuddy.Shared.Contracts.Vocabulary;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.Infrastructure.Messaging;

/// <summary>Thin consumer: maps <see cref="LearnerWordAdded"/> to <see cref="RecordLearnerWordAddedCommand"/>.
/// The EF inbox dedupes on <c>MessageId</c>; a failed result throws so the retry policy runs.</summary>
public sealed class LearnerWordAddedConsumer : IConsumer<LearnerWordAdded>
{
    private readonly ICommandHandler<RecordLearnerWordAddedCommand> _handler;

    public LearnerWordAddedConsumer(ICommandHandler<RecordLearnerWordAddedCommand> handler)
    {
        _handler = handler;
    }

    /// <inheritdoc />
    public async Task Consume(ConsumeContext<LearnerWordAdded> context)
    {
        LearnerWordAdded message = context.Message;
        Result result = await _handler.HandleAsync(
            new RecordLearnerWordAddedCommand(message.UserId, message.SenseId, message.AddedBy, message.AddedAtUtc),
            context.CancellationToken);

        if (result.IsFailure)
        {
            throw new InvalidOperationException(
                $"RecordLearnerWordAdded failed: {result.Error.Code}. MessageId={context.MessageId}");
        }
    }
}
