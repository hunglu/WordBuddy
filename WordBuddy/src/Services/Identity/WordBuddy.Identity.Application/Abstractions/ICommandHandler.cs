using WordBuddy.Shared.Kernel;

namespace WordBuddy.Identity.Application.Abstractions;

/// <summary>Handles a void <see cref="ICommand"/>.</summary>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    Task<Result> HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>Handles a <see cref="ICommand{TResult}"/>, producing a <typeparamref name="TResult"/> on success.</summary>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}
