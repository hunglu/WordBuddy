using System.Net.Http.Headers;
using Microsoft.AspNetCore.Http;

namespace WordBuddy.Progress.Infrastructure.ContentClient;

/// <summary>
/// Copies the caller's <c>Authorization</c> header to the outgoing Content request, so Content applies
/// the same child filter as for a direct call. The token is never logged.
/// </summary>
internal sealed class ForwardBearerTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardBearerTokenHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        string? header = _httpContextAccessor.HttpContext?.Request.Headers.Authorization.ToString();
        if (!string.IsNullOrWhiteSpace(header) && AuthenticationHeaderValue.TryParse(header, out AuthenticationHeaderValue? value))
        {
            request.Headers.Authorization = value;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
