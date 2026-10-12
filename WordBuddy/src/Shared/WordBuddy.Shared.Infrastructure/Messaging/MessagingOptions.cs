namespace WordBuddy.Shared.Infrastructure.Messaging;

/// <summary>
/// Messaging settings bound from the <c>Messaging</c> configuration section.
/// Defaults equal the values that were hard-coded before the options existed.
/// </summary>
public sealed class MessagingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Messaging";

    /// <summary>Transport: <c>RabbitMq</c> (default) or <c>InMemory</c> (tests).</summary>
    public string Transport { get; set; } = MessagingExtensions.RabbitMqTransport;

    /// <summary>RabbitMQ connection settings.</summary>
    public RabbitMqOptions RabbitMq { get; set; } = new();

    /// <summary>Transactional outbox settings.</summary>
    public OutboxOptions Outbox { get; set; } = new();

    /// <summary>Consumer retry settings.</summary>
    public RetryOptions Retry { get; set; } = new();
}

/// <summary>RabbitMQ connection settings.</summary>
public sealed class RabbitMqOptions
{
    /// <summary>Broker host name.</summary>
    public string Host { get; set; } = "localhost";

    /// <summary>Broker virtual host.</summary>
    public string VirtualHost { get; set; } = "/";

    /// <summary>User name. Required outside Development.</summary>
    public string? Username { get; set; }

    /// <summary>Password. Set it by environment variable or user-secrets, never in git.</summary>
    public string? Password { get; set; }

    /// <summary>AMQP heartbeat in seconds. 0 turns heartbeats off.</summary>
    public int HeartbeatSeconds { get; set; } = 10;

    /// <summary>Requested connection timeout in milliseconds.</summary>
    public int RequestedConnectionTimeoutMs { get; set; } = 10000;
}

/// <summary>Transactional outbox settings.</summary>
public sealed class OutboxOptions
{
    /// <summary>How often the outbox delivery service polls for new messages, in milliseconds.</summary>
    public int QueryDelayMs { get; set; } = 1000;

    /// <summary>Window in which a duplicate message id is ignored, in seconds (default 30 minutes).</summary>
    public int DuplicateDetectionWindowSeconds { get; set; } = 1800;
}

/// <summary>Consumer retry settings.</summary>
public sealed class RetryOptions
{
    /// <summary>Delay before each retry attempt, in milliseconds. One entry per attempt.</summary>
    public int[] IntervalsMs { get; set; } = [200, 1000, 5000];
}
