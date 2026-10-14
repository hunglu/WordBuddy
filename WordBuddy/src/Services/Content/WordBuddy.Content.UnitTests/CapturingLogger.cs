using Microsoft.Extensions.Logging;

namespace WordBuddy.Content.UnitTests;

/// <summary>Logger that keeps every formatted message and its state values, so privacy tests can
/// assert that a secret text never reaches a log line.</summary>
internal sealed class CapturingLogger<T> : ILogger<T>
{
    private readonly List<string> _entries = [];

    /// <summary>Formatted messages plus all state values and exception text.</summary>
    public IReadOnlyList<string> Entries => _entries;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        string text = formatter(state, exception);
        if (state is IEnumerable<KeyValuePair<string, object?>> pairs)
        {
            text += " " + string.Join(" ", pairs.Select(p => p.Value?.ToString()));
        }

        _entries.Add(text + " " + exception);
    }
}
