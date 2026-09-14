namespace WordBuddy.Identity.Application.Abstractions;

/// <summary>Marker for a command that mutates state and returns no value beyond success/failure.</summary>
public interface ICommand
{
}

/// <summary>Marker for a command that mutates state and returns a <typeparamref name="TResult"/> on success.</summary>
public interface ICommand<TResult>
{
}

/// <summary>Marker for a query that reads state and returns a <typeparamref name="TResult"/>.</summary>
public interface IQuery<TResult>
{
}
