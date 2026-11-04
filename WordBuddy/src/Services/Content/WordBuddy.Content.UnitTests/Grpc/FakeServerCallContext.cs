using Grpc.Core;
using Microsoft.AspNetCore.Http;

namespace WordBuddy.Content.UnitTests.Grpc;

/// <summary>Minimal <see cref="ServerCallContext"/> for unit tests. Carries an <see cref="HttpContext"/>
/// in <c>UserState["__HttpContext"]</c>, the key <c>GetHttpContext()</c> reads.</summary>
internal sealed class FakeServerCallContext : ServerCallContext
{
    private readonly Dictionary<object, object> _userState = new();

    public FakeServerCallContext(HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        _userState["__HttpContext"] = httpContext;
        CancellationTokenCore = cancellationToken;
    }

    protected override string MethodCore => "/wordbuddy.content.v1.ContentSenses/GetSensesByIds";

    protected override string HostCore => "localhost";

    protected override string PeerCore => "test";

    protected override DateTime DeadlineCore => DateTime.MaxValue;

    protected override Metadata RequestHeadersCore { get; } = new Metadata();

    protected override CancellationToken CancellationTokenCore { get; }

    protected override Metadata ResponseTrailersCore { get; } = new Metadata();

    protected override Status StatusCore { get; set; }

    protected override WriteOptions? WriteOptionsCore { get; set; }

    protected override AuthContext AuthContextCore { get; } = new AuthContext("test", new Dictionary<string, List<AuthProperty>>());

    protected override IDictionary<object, object> UserStateCore => _userState;

    protected override ContextPropagationToken CreatePropagationTokenCore(ContextPropagationOptions? options) =>
        throw new NotSupportedException();

    protected override Task WriteResponseHeadersAsyncCore(Metadata responseHeaders) => Task.CompletedTask;
}
