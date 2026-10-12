using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace WordBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Validates <see cref="MessagingOptions"/> at startup. Each failure message names the config key.
/// </summary>
public sealed class MessagingOptionsValidator : IValidateOptions<MessagingOptions>
{
    private readonly IHostEnvironment _environment;

    /// <summary>Creates the validator.</summary>
    /// <param name="environment">Host environment; credentials are optional only in Development.</param>
    public MessagingOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, MessagingOptions options)
    {
        List<string> failures = [];

        bool inMemory = string.Equals(options.Transport, MessagingExtensions.InMemoryTransport, StringComparison.OrdinalIgnoreCase);
        bool rabbit = string.Equals(options.Transport, MessagingExtensions.RabbitMqTransport, StringComparison.OrdinalIgnoreCase);

        if (!inMemory && !rabbit)
        {
            failures.Add($"Messaging:Transport '{options.Transport}' is invalid. Use '{MessagingExtensions.RabbitMqTransport}' or '{MessagingExtensions.InMemoryTransport}'.");
        }

        if (options.Outbox.QueryDelayMs <= 0)
        {
            failures.Add("Messaging:Outbox:QueryDelayMs must be greater than 0.");
        }

        if (options.Outbox.DuplicateDetectionWindowSeconds <= 0)
        {
            failures.Add("Messaging:Outbox:DuplicateDetectionWindowSeconds must be greater than 0.");
        }

        if (options.Retry.IntervalsMs is null || options.Retry.IntervalsMs.Length == 0)
        {
            failures.Add("Messaging:Retry:IntervalsMs must contain at least one value.");
        }
        else if (options.Retry.IntervalsMs.Any(i => i <= 0))
        {
            failures.Add("Messaging:Retry:IntervalsMs values must all be greater than 0.");
        }

        if (options.RabbitMq.HeartbeatSeconds < 0)
        {
            failures.Add("Messaging:RabbitMq:HeartbeatSeconds must be 0 or greater.");
        }

        if (options.RabbitMq.RequestedConnectionTimeoutMs <= 0)
        {
            failures.Add("Messaging:RabbitMq:RequestedConnectionTimeoutMs must be greater than 0.");
        }

        if (rabbit)
        {
            if (string.IsNullOrWhiteSpace(options.RabbitMq.Host))
            {
                failures.Add("Messaging:RabbitMq:Host must not be empty.");
            }

            if (!_environment.IsDevelopment())
            {
                if (string.IsNullOrWhiteSpace(options.RabbitMq.Username))
                {
                    failures.Add("Messaging:RabbitMq:Username is required outside Development.");
                }

                if (string.IsNullOrWhiteSpace(options.RabbitMq.Password))
                {
                    failures.Add("Messaging:RabbitMq:Password is required outside Development.");
                }
            }
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
