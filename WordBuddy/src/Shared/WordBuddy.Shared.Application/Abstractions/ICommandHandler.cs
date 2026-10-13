using WordBuddy.Shared.Kernel;

namespace WordBuddy.Shared.Application.Abstractions;

/// <summary>Handles a void <see cref="ICommand"/>.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
public interface ICommandHandler<in TCommand>
    where TCommand : ICommand
{
    /// <summary>Handles the command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result"/> describing success or failure.</returns>
    Task<Result> HandleAsync(TCommand command, CancellationToken ct = default);
}

/// <summary>Handles a <see cref="ICommand{TResult}"/>, producing a <typeparamref name="TResult"/> on success.</summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The value returned on success.</typeparam>
public interface ICommandHandler<in TCommand, TResult>
    where TCommand : ICommand<TResult>
{
    /// <summary>Handles the command.</summary>
    /// <param name="command">The command.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="Result{T}"/> with the value or an error.</returns>
    Task<Result<TResult>> HandleAsync(TCommand command, CancellationToken ct = default);
}
