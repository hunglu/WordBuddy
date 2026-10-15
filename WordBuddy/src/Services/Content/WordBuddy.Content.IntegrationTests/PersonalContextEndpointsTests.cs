using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using WordBuddy.Content.Api.Models;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-49: the private note on a learner word — write on all add flows, edit/clear with
/// <c>PUT api/vocabulary/{id}/personal-context</c>, read on "mine", and privacy between users.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class PersonalContextEndpointsTests
{
    private readonly ContentApiFactory _factory;

    public PersonalContextEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient Client(Guid userId, string ageGroup = "Adult", bool isAdmin = false)
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(userId, ageGroup, isAdmin));
        return client;
    }

    private static string UniqueWord() =>
        "w" + new string(Guid.NewGuid().ToString("N").Select(c => (char)('a' + (Convert.ToInt32(c.ToString(), 16) % 26))).ToArray());

    private static async Task<JsonElement> ReadJsonAsync(HttpResponseMessage response)
    {
        response.IsSuccessStatusCode.Should().BeTrue(await response.Content.ReadAsStringAsync());
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.Clone();
    }

    private static async Task<Guid> AddWordAsync(HttpClient client, string? note)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest(UniqueWord(), "a definition", null, note));
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return await response.Content.ReadFromJsonAsync<Guid>();
    }

    /// <summary>The note on "mine" for one word, or null when missing or empty.</summary>
    private static async Task<string?> MineNoteAsync(HttpClient client, Guid id)
    {
        JsonElement mine = await PagedHttp.GetAllAsArrayAsync(client, "/api/vocabulary/mine");
        JsonElement item = mine.EnumerateArray().Single(i => i.GetProperty("id").GetGuid() == id);
        return item.TryGetProperty("personalContext", out JsonElement note) && note.ValueKind == JsonValueKind.String
            ? note.GetString()
            : null;
    }

    private static Task<HttpResponseMessage> PutNoteAsync(HttpClient client, Guid id, string? note) =>
        client.PutAsJsonAsync($"/api/vocabulary/{id}/personal-context", new UpdatePersonalContextRequest(note));

    private async Task<Guid> ShareAndApproveAsync(HttpClient owner, Guid id)
    {
        (await owner.PostAsync($"/api/vocabulary/{id}/share", null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        HttpClient admin = Client(Guid.NewGuid(), isAdmin: true);
        (await admin.PostAsJsonAsync($"/api/vocabulary/moderation/{id}", new ModerateVocabularyWordRequest(Approve: true, VisibleToChildren: true)))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        return id;
    }

    [Fact]
    public async Task PersonalContext_AddWordWithNote_MineShowsNote()
    {
        HttpClient client = Client(Guid.NewGuid());

        Guid id = await AddWordAsync(client, "heard at the market");

        (await MineNoteAsync(client, id)).Should().Be("heard at the market");
    }

    [Fact]
    public async Task PersonalContext_Put_EditsThenClears()
    {
        HttpClient client = Client(Guid.NewGuid());
        Guid id = await AddWordAsync(client, "first");

        (await PutNoteAsync(client, id, "second")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await MineNoteAsync(client, id)).Should().Be("second");

        (await PutNoteAsync(client, id, null)).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await MineNoteAsync(client, id)).Should().BeNull();
    }

    [Fact]
    public async Task PersonalContext_PutOtherUsersWord_Returns404()
    {
        Guid id = await AddWordAsync(Client(Guid.NewGuid()), "mine");

        HttpResponseMessage response = await PutNoteAsync(Client(Guid.NewGuid()), id, "hijack");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PersonalContext_Put501Chars_Returns400()
    {
        HttpClient client = Client(Guid.NewGuid());
        Guid id = await AddWordAsync(client, "keep");

        HttpResponseMessage response = await PutNoteAsync(client, id, new string('x', 501));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await MineNoteAsync(client, id)).Should().Be("keep");
    }

    [Fact]
    public async Task PersonalContext_AddWord501Chars_Returns400()
    {
        HttpResponseMessage response = await Client(Guid.NewGuid()).PostAsJsonAsync(
            "/api/vocabulary", new AddPersonalVocabularyWordRequest(UniqueWord(), "def", null, new string('x', 501)));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PersonalContext_SharedAddToMine_WithAndWithoutBody_Succeeds(bool withNote)
    {
        HttpClient owner = Client(Guid.NewGuid());
        Guid id = await ShareAndApproveAsync(owner, await AddWordAsync(owner, null));
        HttpClient adopter = Client(Guid.NewGuid());

        HttpResponseMessage response = withNote
            ? await adopter.PostAsJsonAsync($"/api/vocabulary/shared/{id}/add-to-mine", new AddToMyListRequest("seen in class"))
            : await adopter.PostAsync($"/api/vocabulary/shared/{id}/add-to-mine", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        (await MineNoteAsync(adopter, id)).Should().Be(withNote ? "seen in class" : null);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PersonalContext_AutofillAddToMine_WithAndWithoutBody_Succeeds(bool withNote)
    {
        HttpClient adult = Client(Guid.NewGuid());
        JsonElement lookup = await ReadJsonAsync(await adult.GetAsync($"/api/vocabulary/autofill?word={UniqueWord()}"));
        lookup.GetProperty("autofillUnavailable").GetBoolean().Should().BeFalse();
        Guid senseId = lookup.GetProperty("senses")[0].GetProperty("senseId").GetGuid();

        HttpResponseMessage response = withNote
            ? await adult.PostAsJsonAsync($"/api/vocabulary/autofill/{senseId}/add-to-mine", new AddToMyListRequest("from a book"))
            : await adult.PostAsync($"/api/vocabulary/autofill/{senseId}/add-to-mine", content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        (await MineNoteAsync(adult, senseId)).Should().Be(withNote ? "from a book" : null);
    }

    [Fact]
    public async Task PersonalContext_OtherUser_NeverSeesNote()
    {
        HttpClient owner = Client(Guid.NewGuid());
        Guid id = await ShareAndApproveAsync(owner, await AddWordAsync(owner, "SECRET-NOTE-123"));
        HttpClient other = Client(Guid.NewGuid());
        (await other.PostAsync($"/api/vocabulary/shared/{id}/add-to-mine", content: null)).StatusCode.Should().Be(HttpStatusCode.Created);

        string shared = (await PagedHttp.GetAllAsArrayAsync(other, "/api/vocabulary/shared")).GetRawText();
        string senses = await (await other.GetAsync($"/api/vocabulary/senses?ids={id}")).Content.ReadAsStringAsync();
        string mine = (await PagedHttp.GetAllAsArrayAsync(other, "/api/vocabulary/mine")).GetRawText();

        shared.Should().NotContain("SECRET-NOTE-123");
        senses.Should().NotContain("SECRET-NOTE-123");
        mine.Should().NotContain("SECRET-NOTE-123");
        (await MineNoteAsync(owner, id)).Should().Be("SECRET-NOTE-123");
    }
}
