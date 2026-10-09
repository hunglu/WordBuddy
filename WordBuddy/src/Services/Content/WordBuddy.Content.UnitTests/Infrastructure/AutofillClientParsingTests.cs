using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using WordBuddy.Content.Application.Interfaces.Autofill;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Autofill;
using WordBuddy.Shared.Kernel;

namespace WordBuddy.Content.UnitTests.Infrastructure;

/// <summary>Fake <see cref="HttpMessageHandler"/>: returns one canned response and records the request.</summary>
internal sealed class StubHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;

    public StubHttpHandler(HttpStatusCode status, string body)
    {
        _status = status;
        _body = body;
    }

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastBody { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        return new HttpResponseMessage(_status) { Content = new StringContent(_body, Encoding.UTF8, "application/json") };
    }
}

public class FreeDictionaryClientTests
{
    private const string AppleJson = """
        [{"word":"apple","phonetic":"/ˈæp.əl/",
          "phonetics":[{"text":"/ˈæp.əl/","audio":"https://api.dictionaryapi.dev/media/pronunciations/en/apple-uk.mp3"},
                       {"text":"/ˈæp.l̩/","audio":"https://api.dictionaryapi.dev/media/pronunciations/en/apple-us.mp3"}],
          "meanings":[{"partOfSpeech":"noun"},{"partOfSpeech":"verb"},{"partOfSpeech":"weird"}]}]
        """;

    private static FreeDictionaryClient Client(StubHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://dict.test/entries/en/") }, NullLogger<FreeDictionaryClient>.Instance);

    [Fact]
    public async Task FreeDictionaryClient_LookupAsync_ValidJson_MapsIpaAudioAndPos()
    {
        Result<DictionaryEntry> result = await Client(new StubHttpHandler(HttpStatusCode.OK, AppleJson)).LookupAsync("apple");

        result.IsSuccess.Should().BeTrue();
        result.Value.IpaUk.Should().Be("/ˈæp.əl/");
        result.Value.IpaUs.Should().Be("/ˈæp.l̩/");
        result.Value.AudioUkUrl.Should().EndWith("apple-uk.mp3");
        result.Value.AudioUsUrl.Should().EndWith("apple-us.mp3");
        result.Value.PartsOfSpeech.Should().Equal(PartOfSpeech.Noun, PartOfSpeech.Verb);
    }

    [Fact]
    public async Task FreeDictionaryClient_LookupAsync_SendsOnlyTheWordInThePath()
    {
        StubHttpHandler handler = new(HttpStatusCode.OK, AppleJson);

        await Client(handler).LookupAsync(" apple ");

        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://dict.test/entries/en/apple");
        handler.LastBody.Should().BeNull();
    }

    [Fact]
    public async Task FreeDictionaryClient_LookupAsync_BadJson_ReturnsFailure()
    {
        Result<DictionaryEntry> result = await Client(new StubHttpHandler(HttpStatusCode.OK, "{not json")).LookupAsync("apple");

        result.IsFailure.Should().BeTrue();
        result.Error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task FreeDictionaryClient_LookupAsync_404_ReturnsNotFound()
    {
        Result<DictionaryEntry> result = await Client(new StubHttpHandler(HttpStatusCode.NotFound, "{}")).LookupAsync("zzz");

        result.Error.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task FreeDictionaryClient_LookupAsync_ServerError_ReturnsFailure()
    {
        Result<DictionaryEntry> result = await Client(new StubHttpHandler(HttpStatusCode.InternalServerError, "")).LookupAsync("apple");

        result.Error.Type.Should().Be(ErrorType.Failure);
    }
}

public class ClaudeSenseGeneratorTests
{
    private static readonly Guid ExistingId = Guid.NewGuid();

    private static SenseGenerationRequest Request() =>
        new("apple", [PartOfSpeech.Noun], ["vi"], [new ExistingCatalogSense(ExistingId, "a fruit")]);

    private static string ToolResponse(JsonNode input) => new JsonObject
    {
        ["content"] = new JsonArray(new JsonObject
        {
            ["type"] = "tool_use",
            ["name"] = ClaudeSenseGenerator.ToolName,
            ["input"] = input,
        }),
    }.ToJsonString();

    private static string ValidResponse(string matchedId) => ToolResponse(JsonNode.Parse($$"""
        {"lexemes":[{"partOfSpeech":"Noun","syllables":"ap·ple","wordForms":["apples"],"cefrLevel":"A1",
          "matchedExistingSenseIds":["{{matchedId}}"],
          "senses":[{"definition":"A round fruit.","examples":["I eat an apple."],
                     "translations":[{"locale":"vi","text":"quả táo"}],"childSuitable":true}]}]}
        """)!);

    private static ClaudeSenseGenerator Generator(StubHttpHandler handler, string? apiKey = "test-key") =>
        new(
            new HttpClient(handler) { BaseAddress = new Uri("https://claude.test/") },
            Options.Create(new AutofillClientSettings { Claude = new AutofillClientSettings.ClaudeSettings { ApiKey = apiKey } }),
            NullLogger<ClaudeSenseGenerator>.Instance);

    [Fact]
    public async Task ClaudeSenseGenerator_GenerateAsync_ValidToolOutput_MapsSensesAndMatches()
    {
        StubHttpHandler handler = new(HttpStatusCode.OK, ValidResponse(ExistingId.ToString()));

        Result<GeneratedWord> result = await Generator(handler).GenerateAsync(Request());

        result.IsSuccess.Should().BeTrue();
        GeneratedLexeme lexeme = result.Value.Lexemes.Should().ContainSingle().Subject;
        lexeme.PartOfSpeech.Should().Be(PartOfSpeech.Noun);
        lexeme.CefrLevel.Should().Be(CefrLevel.A1);
        lexeme.MatchedExistingSenseIds.Should().Equal(ExistingId);
        lexeme.Senses.Single().Translations["vi"].Should().Be("quả táo");
    }

    [Fact]
    public async Task ClaudeSenseGenerator_GenerateAsync_SendsModelKeyAndForcedTool()
    {
        StubHttpHandler handler = new(HttpStatusCode.OK, ValidResponse(ExistingId.ToString()));

        await Generator(handler).GenerateAsync(Request());

        handler.LastRequest!.RequestUri!.ToString().Should().Be("https://claude.test/v1/messages");
        handler.LastRequest.Headers.GetValues("x-api-key").Should().Equal("test-key");
        JsonNode body = JsonNode.Parse(handler.LastBody!)!;
        body["model"]!.GetValue<string>().Should().Be("claude-sonnet-5-5");
        body["tool_choice"]!["name"]!.GetValue<string>().Should().Be(ClaudeSenseGenerator.ToolName);
    }

    [Fact]
    public async Task ClaudeSenseGenerator_GenerateAsync_UnknownMatchedId_IsDropped()
    {
        StubHttpHandler handler = new(HttpStatusCode.OK, ValidResponse(Guid.NewGuid().ToString()));

        Result<GeneratedWord> result = await Generator(handler).GenerateAsync(Request());

        result.Value.Lexemes.Single().MatchedExistingSenseIds.Should().BeEmpty();
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("""{"content":[{"type":"text","text":"hi"}]}""")]
    [InlineData("""{"content":[{"type":"tool_use","name":"save_word_senses","input":{"lexemes":[]}}]}""")]
    [InlineData("""{"content":[{"type":"tool_use","name":"save_word_senses","input":{"lexemes":[{"partOfSpeech":"NotAPos","senses":[]}]}}]}""")]
    public async Task ClaudeSenseGenerator_GenerateAsync_BadJson_ReturnsFailure(string body)
    {
        Result<GeneratedWord> result = await Generator(new StubHttpHandler(HttpStatusCode.OK, body)).GenerateAsync(Request());

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task ClaudeSenseGenerator_GenerateAsync_HttpError_ReturnsFailure()
    {
        Result<GeneratedWord> result = await Generator(new StubHttpHandler(HttpStatusCode.TooManyRequests, "{}")).GenerateAsync(Request());

        result.Error.Type.Should().Be(ErrorType.Failure);
    }

    [Fact]
    public async Task ClaudeSenseGenerator_GenerateAsync_NoApiKey_FailsWithoutCalling()
    {
        StubHttpHandler handler = new(HttpStatusCode.OK, ValidResponse(ExistingId.ToString()));

        Result<GeneratedWord> result = await Generator(handler, apiKey: null).GenerateAsync(Request());

        result.IsFailure.Should().BeTrue();
        handler.LastRequest.Should().BeNull();
    }
}
