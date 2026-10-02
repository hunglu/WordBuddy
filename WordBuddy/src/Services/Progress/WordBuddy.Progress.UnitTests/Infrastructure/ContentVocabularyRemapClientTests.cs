using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using WordBuddy.Progress.Application.DTOs;
using WordBuddy.Progress.Infrastructure.Services;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Progress.UnitTests.Infrastructure;

public class ContentVocabularyRemapClientTests
{
    private const string Secret = "unit-test-secret-do-not-use-in-production-32chars!";

    private readonly StubHandler _stub = new();

    /// <summary>Real pipeline minus resilience: <see cref="ServiceTokenHandler"/> → stub transport.</summary>
    private ContentVocabularyRemapClient CreateClient()
    {
        ServiceTokenHandler tokenHandler = new(new ServiceTokenProvider(Secret, "WordBuddy", TimeProvider.System))
        {
            InnerHandler = _stub,
        };
        HttpClient httpClient = new(tokenHandler) { BaseAddress = new Uri("http://content-api:8080/") };
        return new ContentVocabularyRemapClient(httpClient, NullLogger<ContentVocabularyRemapClient>.Instance);
    }

    [Fact]
    public async Task ContentVocabularyRemapClient_GetPendingAsync_Success_SendsBearerAndParsesItems()
    {
        Guid oldId = Guid.NewGuid();
        Guid newId = Guid.NewGuid();
        _stub.Respond(HttpStatusCode.OK, $$"""{"items":[{"oldId":"{{oldId}}","newId":"{{newId}}"}]}""");

        Result<IReadOnlyList<VocabularyWordIdRemapPair>> result = await CreateClient().GetPendingAsync(250);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Equal(new VocabularyWordIdRemapPair(oldId, newId));
        _stub.LastRequest!.Method.Should().Be(HttpMethod.Get);
        _stub.LastRequest.RequestUri!.ToString().Should().Be("http://content-api:8080/internal/vocabulary-remaps?limit=250");
        _stub.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        _stub.LastRequest.Headers.Authorization.Parameter.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ContentVocabularyRemapClient_AcknowledgeAsync_NoContent_PostsOldIdsWithBearer()
    {
        List<Guid> ids = [Guid.NewGuid(), Guid.NewGuid()];
        _stub.Respond(HttpStatusCode.NoContent);

        Result result = await CreateClient().AcknowledgeAsync(ids);

        result.IsSuccess.Should().BeTrue();
        _stub.LastRequest!.Method.Should().Be(HttpMethod.Post);
        _stub.LastRequest.RequestUri!.AbsolutePath.Should().Be("/internal/vocabulary-remaps/acknowledge");
        _stub.LastRequest.Headers.Authorization!.Scheme.Should().Be("Bearer");
        using JsonDocument body = JsonDocument.Parse(_stub.LastRequestBody!);
        body.RootElement.GetProperty("oldIds").EnumerateArray().Select(e => e.GetGuid()).Should().Equal(ids);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "ContentApi.Unavailable")]
    [InlineData(HttpStatusCode.Unauthorized, "ContentApi.Unauthorized")]
    [InlineData(HttpStatusCode.Forbidden, "ContentApi.Unauthorized")]
    [InlineData(HttpStatusCode.InternalServerError, "ContentApi.Unavailable")]
    [InlineData(HttpStatusCode.ServiceUnavailable, "ContentApi.Unavailable")]
    public async Task ContentVocabularyRemapClient_GetPendingAsync_ErrorStatus_MapsToTypedError(HttpStatusCode status, string expectedCode)
    {
        _stub.Respond(status);

        Result<IReadOnlyList<VocabularyWordIdRemapPair>> result = await CreateClient().GetPendingAsync(10);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, "ContentApi.Unavailable")]
    [InlineData(HttpStatusCode.Forbidden, "ContentApi.Unauthorized")]
    [InlineData(HttpStatusCode.BadGateway, "ContentApi.Unavailable")]
    public async Task ContentVocabularyRemapClient_AcknowledgeAsync_ErrorStatus_MapsToTypedError(HttpStatusCode status, string expectedCode)
    {
        _stub.Respond(status);

        Result result = await CreateClient().AcknowledgeAsync([Guid.NewGuid()]);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public async Task ContentVocabularyRemapClient_GetPendingAsync_NetworkError_ReturnsUnavailable()
    {
        _stub.Throw(new HttpRequestException("connection refused"));

        Result<IReadOnlyList<VocabularyWordIdRemapPair>> result = await CreateClient().GetPendingAsync(10);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("ContentApi.Unavailable");
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private HttpStatusCode _status = HttpStatusCode.OK;
        private string? _body;
        private Exception? _exception;

        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        public void Respond(HttpStatusCode status, string? body = null)
        {
            _status = status;
            _body = body;
            _exception = null;
        }

        public void Throw(Exception exception) => _exception = exception;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

            if (_exception is not null)
            {
                throw _exception;
            }

            HttpResponseMessage response = new(_status);
            if (_body is not null)
            {
                response.Content = new StringContent(_body, Encoding.UTF8, "application/json");
            }

            return response;
        }
    }
}
