using Microsoft.Extensions.Configuration;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>How Progress calls Content. Selected once at startup by <c>Services:Content:Transport</c>.</summary>
public enum ContentTransport
{
    /// <summary>HTTP/JSON against <c>GET /api/vocabulary/senses</c>. Kept as the explicit alternative.</summary>
    Rest,

    /// <summary>gRPC <c>ContentSenses.GetSensesByIds</c> on Content's HTTP/2 port. The default.</summary>
    Grpc,
}

/// <summary>Where the Content service lives. Bound from <c>Services:Content</c>.</summary>
public sealed class ContentOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Services:Content";

    /// <summary>Default for local development (Content REST runs on 5081).</summary>
    public const string DefaultBaseUrl = "http://localhost:5081";

    /// <summary>Default for local development (Content gRPC runs on 5091).</summary>
    public const string DefaultGrpcUrl = "http://localhost:5091";

    /// <summary>Channel used for the Content sense call. Defaults to <see cref="ContentTransport.Grpc"/>.</summary>
    public ContentTransport Transport { get; set; } = ContentTransport.Grpc;

    /// <summary>Base URL of Content's REST API, for example <c>http://content-api:8080</c>. Used when
    /// <see cref="Transport"/> is <see cref="ContentTransport.Rest"/>, and by the REST health check.</summary>
    public string BaseUrl { get; set; } = DefaultBaseUrl;

    /// <summary>Base URL of Content's gRPC endpoint (HTTP/2, cleartext), for example <c>http://content-api:8081</c>.</summary>
    public string GrpcUrl { get; set; } = DefaultGrpcUrl;

    /// <summary>Reads the options from configuration. An unknown <c>Transport</c> value fails fast at startup.</summary>
    public static ContentOptions FromConfiguration(IConfiguration configuration)
    {
        ContentOptions options = new();
        string? transport = configuration[$"{SectionName}:Transport"];
        if (!string.IsNullOrWhiteSpace(transport))
        {
            if (!Enum.TryParse(transport, ignoreCase: true, out ContentTransport parsed) || !Enum.IsDefined(parsed))
            {
                throw new InvalidOperationException(
                    $"'{SectionName}:Transport' must be 'Grpc' or 'Rest', but was '{transport}'.");
            }

            options.Transport = parsed;
        }

        options.BaseUrl = configuration[$"{SectionName}:BaseUrl"] ?? DefaultBaseUrl;
        options.GrpcUrl = configuration[$"{SectionName}:GrpcUrl"] ?? DefaultGrpcUrl;
        return options;
    }
}
