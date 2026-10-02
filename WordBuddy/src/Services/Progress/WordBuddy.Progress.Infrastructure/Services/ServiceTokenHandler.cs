using System.Net.Http.Headers;

namespace WordBuddy.Progress.Infrastructure.Services;

/// <summary>Attaches Progress's service JWT as a bearer token to every outgoing Content request.</summary>
public sealed class ServiceTokenHandler : DelegatingHandler
{
    private readonly ServiceTokenProvider _tokenProvider;

    public ServiceTokenHandler(ServiceTokenProvider tokenProvider)
    {
        _tokenProvider = tokenProvider;
    }

    /// <inheritdoc />
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokenProvider.GetToken());
        return base.SendAsync(request, cancellationToken);
    }
}
