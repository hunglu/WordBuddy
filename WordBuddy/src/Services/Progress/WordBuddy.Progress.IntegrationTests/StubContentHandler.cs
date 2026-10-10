using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Web;

namespace WordBuddy.Progress.IntegrationTests;

/// <summary>
/// Stands in for the Content service behind the real <c>ContentSenseClient</c> (same HTTP shape as
/// <c>GET /api/vocabulary/senses?ids=</c>). Like Content, it omits unknown ids and, for a Child token,
/// senses flagged hidden for children. It records every request's bearer token so tests can check
/// the caller's token is forwarded.
/// </summary>
public sealed class StubContentHandler : HttpMessageHandler
{
    private readonly object _lock = new();
    private readonly Dictionary<Guid, StubSense> _senses = [];
    private readonly List<StubRequest> _requests = [];

    /// <summary>When set, every call fails with this status code (a non-retried one).</summary>
    public HttpStatusCode? FailWith { get; set; }

    /// <summary>Registers a sense Content would return.</summary>
    public void Register(Guid senseId, string word, string? imageUrl = null, string? audioUrl = null, bool hiddenForChild = false)
    {
        lock (_lock)
        {
            _senses[senseId] = new StubSense(senseId, word, $"definition {senseId:N}", imageUrl, audioUrl, hiddenForChild);
        }
    }

    /// <summary>The age group of the token on each request that asked for any of <paramref name="senseId"/>.</summary>
    public IReadOnlyList<StubRequest> RequestsFor(Guid senseId)
    {
        lock (_lock)
        {
            return _requests.Where(r => r.Ids.Contains(senseId)).ToList();
        }
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? bearer = request.Headers.Authorization?.Parameter;
        string? ageGroup = null;
        if (bearer is not null)
        {
            ageGroup = new JwtSecurityTokenHandler().ReadJwtToken(bearer).Claims.FirstOrDefault(c => c.Type == "age_group")?.Value;
        }

        List<Guid> ids = HttpUtility.ParseQueryString(request.RequestUri!.Query).GetValues("ids")?.Select(Guid.Parse).ToList() ?? [];
        lock (_lock)
        {
            _requests.Add(new StubRequest(request.Headers.Authorization?.Scheme, bearer, ageGroup, ids));
        }

        if (FailWith is { } status)
        {
            return Task.FromResult(new HttpResponseMessage(status));
        }

        bool isChild = string.Equals(ageGroup, "Child", StringComparison.OrdinalIgnoreCase);
        List<object> visible;
        lock (_lock)
        {
            visible = ids
                .Where(id => _senses.TryGetValue(id, out StubSense? s) && !(isChild && s.HiddenForChild))
                .Select(id => _senses[id])
                .Select(s => (object)new
                {
                    senseId = s.SenseId,
                    word = s.Word,
                    definition = s.Definition,
                    example = (string?)null,
                    audioUrl = s.AudioUrl,
                    imageUrl = s.ImageUrl,
                    personalContext = (string?)null,
                })
                .ToList();
        }

        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(visible), Encoding.UTF8, "application/json"),
        });
    }

    private sealed record StubSense(Guid SenseId, string Word, string Definition, string? ImageUrl, string? AudioUrl, bool HiddenForChild);
}

/// <summary>One call the stub received.</summary>
/// <param name="Scheme">Authorization scheme, for example <c>Bearer</c>.</param>
/// <param name="Token">The forwarded bearer token.</param>
/// <param name="AgeGroup">The <c>age_group</c> claim inside the token.</param>
/// <param name="Ids">Requested sense ids.</param>
public sealed record StubRequest(string? Scheme, string? Token, string? AgeGroup, IReadOnlyList<Guid> Ids);
