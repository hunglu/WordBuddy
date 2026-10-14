using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WordBuddy.Content.Application.DTOs;
using WordBuddy.Content.Domain;
using WordBuddy.Content.Infrastructure.Persistence;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>WB-50 admin lexeme screen through the real API and SQL Server: auth, "contains" search,
/// edit, audio upload and guarded delete, and cache invalidation.</summary>
[Collection(ContentApiCollection.Name)]
public sealed class LexemesAdminEndpointsTests
{
    private const string Base = "/api/vocabulary/admin/lexemes";
    private static readonly string StorageBase = Path.Combine(Path.GetTempPath(), "wordbuddy-content-tests");

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly ContentApiFactory _factory;

    public LexemesAdminEndpointsTests(ContentApiFactory factory)
    {
        _factory = factory;
    }

    private async Task<HttpClient> ClientAsync(string ageGroup, bool isAdmin = false)
    {
        Guid id = Guid.NewGuid();
        if (ageGroup == "Child")
        {
            await SupportLinkTestSeed.SeedActiveSupporterAsync(_factory.Services, id);
        }

        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestJwtTokenFactory.CreateToken(id, ageGroup, isAdmin));
        return client;
    }

    private Task<HttpClient> AdminAsync() => ClientAsync("Adult", isAdmin: true);

    /// <summary>A unique letters-and-digits token so each test only sees its own lexemes.</summary>
    private static string Token() => "zq" + Guid.NewGuid().ToString("N")[..10];

    private async Task<T> WithDbAsync<T>(Func<ContentDbContext, Task<T>> action)
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        return await action(scope.ServiceProvider.GetRequiredService<ContentDbContext>());
    }

    private async Task<Lexeme> SeedLexemeAsync(string lemma, PartOfSpeech? pos = PartOfSpeech.Noun, bool catalogSense = true, string? ipaUk = null)
    {
        Lexeme lexeme = Lexeme.Create(Guid.NewGuid(), lemma, pos).Value;
        if (ipaUk is not null)
        {
            lexeme.Enrich(pos, ipaUk, null, null, null, null);
        }

        await WithDbAsync(async db =>
        {
            db.Lexemes.Add(lexeme);
            if (catalogSense)
            {
                db.Senses.Add(Sense.CreateSystem(Guid.NewGuid(), lexeme.Id, lemma, $"definition {Guid.NewGuid():N}", "An example."));
            }

            await db.SaveChangesAsync();
            return 0;
        });
        return lexeme;
    }

    private static async Task<AdminLexemePageDto> SearchAsync(HttpClient admin, string query)
    {
        HttpResponseMessage response = await admin.GetAsync($"{Base}?{query}");
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<AdminLexemePageDto>(JsonOptions))!;
    }

    private static async Task<AdminLexemeDetailDto> DetailAsync(HttpClient admin, Guid id) =>
        (await admin.GetFromJsonAsync<AdminLexemeDetailDto>($"{Base}/{id}", JsonOptions))!;

    private static object UpdateBody(string pos = "Noun", string? ipaUk = "/uk/") => new
    {
        partOfSpeech = pos,
        ipaUk,
        ipaUs = "/us/",
        syllables = "wo·rd",
        wordForms = new[] { "words", "Words", " word's " },
        cefrLevel = "B2",
        frequencyRank = 1234,
    };

    private static MultipartFormDataContent Mp3(byte[] bytes, string contentType = "audio/mpeg")
    {
        MultipartFormDataContent form = new();
        ByteArrayContent file = new(bytes);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        form.Add(file, "file", "audio.mp3");
        return form;
    }

    private static string AudioPath(Guid lexemeId, string locale) =>
        Path.Combine(StorageBase, "audio", $"lexeme-{lexemeId}-{locale}.mp3");

    // ---- auth

    public static TheoryData<string, string> Endpoints() => new()
    {
        { "GET", Base },
        { "GET", Base + "/{id}" },
        { "PUT", Base + "/{id}" },
        { "PUT", Base + "/{id}/audio/en-GB" },
        { "DELETE", Base + "/{id}/audio/en-GB" },
    };

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task LexemesAdminController_Anonymous_Returns401(string method, string urlTemplate)
    {
        string url = urlTemplate.Replace("{id}", Guid.NewGuid().ToString());

        HttpResponseMessage response = await _factory.CreateClient().SendAsync(new HttpRequestMessage(new HttpMethod(method), url));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [MemberData(nameof(Endpoints))]
    public async Task LexemesAdminController_AdultAndChildLearner_Returns403(string method, string urlTemplate)
    {
        string url = urlTemplate.Replace("{id}", Guid.NewGuid().ToString());
        foreach (string ageGroup in new[] { "Adult", "Child" })
        {
            HttpClient client = await ClientAsync(ageGroup);

            // The upload action consumes multipart/form-data; any other type is answered 415 before authorization.
            HttpContent body = method == "PUT" && url.Contains("/audio/", StringComparison.Ordinal)
                ? Mp3([1])
                : JsonContent.Create(new { });

            HttpResponseMessage response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = body });

            response.StatusCode.Should().Be(HttpStatusCode.Forbidden, $"{ageGroup} must not reach {method} {urlTemplate}");
        }
    }

    // ---- search

    [Fact]
    public async Task LexemesAdmin_Search_MatchesMiddleOfWord()
    {
        string token = Token();
        HttpClient admin = await AdminAsync();
        Lexeme middle = await SeedLexemeAsync($"big{token}wolf");

        AdminLexemePageDto page = await SearchAsync(admin, $"search={token}");

        page.Items.Should().ContainSingle().Which.Id.Should().Be(middle.Id);
        page.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task LexemesAdmin_Search_RanksPrefixMatchesFirst()
    {
        string token = Token();
        HttpClient admin = await AdminAsync();
        Lexeme containsOnly = await SeedLexemeAsync($"aaa{token}");
        Lexeme prefix = await SeedLexemeAsync($"{token}zzz");

        AdminLexemePageDto page = await SearchAsync(admin, $"search={token}");

        // "aaa..." would sort first alphabetically; the prefix match must still win.
        page.Items.Select(i => i.Id).Should().Equal(prefix.Id, containsOnly.Id);
    }

    [Theory]
    [InlineData("%", "50%off", "50xoff")]
    [InlineData("_", "a_b", "axb")]
    [InlineData("[", "a[b", "axb")]
    public async Task LexemesAdmin_Search_SpecialCharacterMatchesOnlyTheLiteral(string special, string literalLemma, string lookAlikeLemma)
    {
        string token = Token();
        HttpClient admin = await AdminAsync();
        Lexeme literal = await SeedLexemeAsync(token + literalLemma);
        await SeedLexemeAsync(token + lookAlikeLemma);
        string term = token + literalLemma[..(literalLemma.IndexOf(special[0]) + 1)];

        AdminLexemePageDto page = await SearchAsync(admin, $"search={Uri.EscapeDataString(term)}");

        page.Items.Should().ContainSingle().Which.Id.Should().Be(literal.Id);
    }

    [Fact]
    public async Task LexemesAdmin_Search_Paging_ReturnsTotalCountAndSecondPage()
    {
        string token = Token();
        HttpClient admin = await AdminAsync();
        foreach (string suffix in new[] { "a", "b", "c" })
        {
            await SeedLexemeAsync($"{token}{suffix}");
        }

        AdminLexemePageDto second = await SearchAsync(admin, $"search={token}&page=2&pageSize=2");

        second.TotalCount.Should().Be(3);
        second.Items.Should().HaveCount(1);
        second.Page.Should().Be(2);
        second.PageSize.Should().Be(2);
    }

    [Fact]
    public async Task LexemesAdmin_Search_FiltersByPartOfSpeechAndCountsSenses()
    {
        string token = Token();
        HttpClient admin = await AdminAsync();
        Lexeme noun = await SeedLexemeAsync($"{token}run", PartOfSpeech.Noun);
        await SeedLexemeAsync($"{token}run", PartOfSpeech.Verb);

        AdminLexemePageDto page = await SearchAsync(admin, $"search={token}&partOfSpeech=Noun");

        AdminLexemeListItemDto item = page.Items.Should().ContainSingle().Subject;
        item.Id.Should().Be(noun.Id);
        item.CatalogSenseCount.Should().Be(1);
        item.PrivateLearnerSenseCount.Should().Be(0);
    }

    [Theory]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=0")]
    public async Task LexemesAdmin_Search_InvalidPaging_Returns400(string query)
    {
        HttpClient admin = await AdminAsync();

        (await admin.GetAsync($"{Base}?{query}")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---- edit

    [Fact]
    public async Task LexemesAdmin_Detail_ShowsCatalogSensesAndHidesPrivateLearnerText()
    {
        string token = Token();
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{token}word");
        string secret = $"SECRET-{Guid.NewGuid():N}";
        await WithDbAsync(async db =>
        {
            db.Senses.Add(Sense.CreateLearner(Guid.NewGuid(), lexeme.Id, Guid.NewGuid(), AgeGroup.Child, $"{token}word", secret, secret).Value);
            await db.SaveChangesAsync();
            return 0;
        });

        HttpResponseMessage response = await admin.GetAsync($"{Base}/{lexeme.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        string raw = await response.Content.ReadAsStringAsync();
        raw.Should().NotContain(secret);
        AdminLexemeDetailDto detail = JsonSerializer.Deserialize<AdminLexemeDetailDto>(raw, JsonOptions)!;
        detail.Senses.Should().ContainSingle();
        detail.CatalogSenseCount.Should().Be(1);
        detail.PrivateLearnerSenseCount.Should().Be(1);
    }

    [Fact]
    public async Task LexemesAdmin_Detail_UnknownId_Returns404()
    {
        HttpClient admin = await AdminAsync();

        (await admin.GetAsync($"{Base}/{Guid.NewGuid()}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LexemesAdmin_Update_Returns204AndFieldsReadBack()
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");

        HttpResponseMessage response = await admin.PutAsJsonAsync($"{Base}/{lexeme.Id}", UpdateBody("Verb"));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        AdminLexemeDetailDto detail = await DetailAsync(admin, lexeme.Id);
        detail.PartOfSpeech.Should().Be(PartOfSpeech.Verb);
        detail.IpaUk.Should().Be("/uk/");
        detail.IpaUs.Should().Be("/us/");
        detail.Syllables.Should().Be("wo·rd");
        detail.WordForms.Should().Equal("words", "word's");
        detail.CefrLevel.Should().Be(CefrLevel.B2);
        detail.FrequencyRank.Should().Be(1234);
    }

    [Fact]
    public async Task LexemesAdmin_Update_PartOfSpeechOfExistingSibling_Returns409()
    {
        HttpClient admin = await AdminAsync();
        string lemma = $"{Token()}run";
        Lexeme noun = await SeedLexemeAsync(lemma, PartOfSpeech.Noun);
        await SeedLexemeAsync(lemma, PartOfSpeech.Verb);

        HttpResponseMessage response = await admin.PutAsJsonAsync($"{Base}/{noun.Id}", UpdateBody("Verb"));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await DetailAsync(admin, noun.Id)).PartOfSpeech.Should().Be(PartOfSpeech.Noun);
    }

    [Theory]
    [InlineData("""{ "ipaUk": "/x/" }""")]
    [InlineData("""{ "partOfSpeech": "Noun", "frequencyRank": 0 }""")]
    public async Task LexemesAdmin_Update_InvalidBody_Returns400(string json)
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");

        HttpResponseMessage response = await admin.PutAsync(
            $"{Base}/{lexeme.Id}", new StringContent(json, System.Text.Encoding.UTF8, "application/json"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LexemesAdmin_Update_UnknownId_Returns404()
    {
        HttpClient admin = await AdminAsync();

        (await admin.PutAsJsonAsync($"{Base}/{Guid.NewGuid()}", UpdateBody())).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LexemesAdmin_Update_ClearsCachesSoSharedPoolAndAutofillShowNewIpa()
    {
        string word = "w" + new string(Guid.NewGuid().ToString("N").Select(c => (char)('a' + (Convert.ToInt32(c.ToString(), 16) % 26))).ToArray());
        HttpClient admin = await AdminAsync();
        HttpClient adult = await ClientAsync("Adult");

        // Auto-fill saves a catalog lexeme (IPA from the fake dictionary) and caches the result.
        JsonElement first = JsonDocument.Parse(await adult.GetStringAsync($"/api/vocabulary/autofill?word={word}")).RootElement;
        first.GetProperty("senses")[0].GetProperty("ipaUk").GetString().Should().Be("/test-uk/");
        Guid senseId = first.GetProperty("senses")[0].GetProperty("senseId").GetGuid();
        // Warm the shared-pool cache.
        string poolBefore = await adult.GetStringAsync("/api/vocabulary/shared");
        poolBefore.Should().Contain("/test-uk/");

        Guid lexemeId = await WithDbAsync(db => db.Senses.Where(s => s.Id == senseId).Select(s => s.LexemeId).SingleAsync());
        (await admin.PutAsJsonAsync($"{Base}/{lexemeId}", UpdateBody("Noun", "/edited-uk/"))).StatusCode.Should().Be(HttpStatusCode.NoContent);

        JsonElement second = JsonDocument.Parse(await adult.GetStringAsync($"/api/vocabulary/autofill?word={word}")).RootElement;
        second.GetProperty("senses")[0].GetProperty("ipaUk").GetString().Should().Be("/edited-uk/");
        string poolAfter = await adult.GetStringAsync("/api/vocabulary/shared");
        poolAfter.Should().Contain("/edited-uk/");
    }

    // ---- audio

    [Fact]
    public async Task LexemesAdmin_UploadAudio_Returns200AndFileExistsThenSecondUploadOverwritesInPlace()
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");

        HttpResponseMessage first = await admin.PutAsync($"{Base}/{lexeme.Id}/audio/en-GB", Mp3([1, 2, 3]));

        first.StatusCode.Should().Be(HttpStatusCode.OK, await first.Content.ReadAsStringAsync());
        MediaAssetDto asset = (await first.Content.ReadFromJsonAsync<MediaAssetDto>(JsonOptions))!;
        string path = AudioPath(lexeme.Id, "en-GB");
        File.Exists(path).Should().BeTrue();
        (await File.ReadAllBytesAsync(path)).Should().Equal(1, 2, 3);
        (await DetailAsync(admin, lexeme.Id)).HasUkAudio.Should().BeTrue();

        HttpResponseMessage second = await admin.PutAsync($"{Base}/{lexeme.Id}/audio/en-GB", Mp3([9, 9]));

        second.StatusCode.Should().Be(HttpStatusCode.OK);
        (await second.Content.ReadFromJsonAsync<MediaAssetDto>(JsonOptions))!.Id.Should().Be(asset.Id);
        (await File.ReadAllBytesAsync(path)).Should().Equal(9, 9);
        int rows = await WithDbAsync(db => db.MediaAssets.CountAsync(a => a.Url == asset.Url));
        rows.Should().Be(1);
    }

    [Theory]
    [InlineData("en-GB", "audio/wav")]
    [InlineData("en-AU", "audio/mpeg")]
    public async Task LexemesAdmin_UploadAudio_WrongTypeOrLocale_Returns400(string locale, string contentType)
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");

        HttpResponseMessage response = await admin.PutAsync($"{Base}/{lexeme.Id}/audio/{locale}", Mp3([1], contentType));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task LexemesAdmin_UploadAudio_UnknownLexeme_Returns404()
    {
        HttpClient admin = await AdminAsync();

        (await admin.PutAsync($"{Base}/{Guid.NewGuid()}/audio/en-US", Mp3([1]))).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task LexemesAdmin_RemoveAudio_UnusedAsset_Returns204AndRowAndFileAreGone()
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");
        MediaAssetDto asset = (await (await admin.PutAsync($"{Base}/{lexeme.Id}/audio/en-US", Mp3([1, 2]))).Content.ReadFromJsonAsync<MediaAssetDto>(JsonOptions))!;
        File.Exists(AudioPath(lexeme.Id, "en-US")).Should().BeTrue();

        HttpResponseMessage response = await admin.DeleteAsync($"{Base}/{lexeme.Id}/audio/en-US");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await WithDbAsync(db => db.MediaAssets.AnyAsync(a => a.Id == asset.Id))).Should().BeFalse();
        File.Exists(AudioPath(lexeme.Id, "en-US")).Should().BeFalse();
        (await DetailAsync(admin, lexeme.Id)).HasUsAudio.Should().BeFalse();
    }

    [Fact]
    public async Task LexemesAdmin_RemoveAudio_SharedAsset_OnlyUnlinksAndSenseStillPlays()
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");
        MediaAssetDto asset = (await (await admin.PutAsync($"{Base}/{lexeme.Id}/audio/en-GB", Mp3([1, 2]))).Content.ReadFromJsonAsync<MediaAssetDto>(JsonOptions))!;
        Guid senseId = Guid.NewGuid();
        await WithDbAsync(async db =>
        {
            db.Senses.Add(Sense.CreateSystem(senseId, lexeme.Id, "shared-audio", $"definition {Guid.NewGuid():N}", "ex", asset.Id));
            await db.SaveChangesAsync();
            return 0;
        });

        HttpResponseMessage response = await admin.DeleteAsync($"{Base}/{lexeme.Id}/audio/en-GB");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await DetailAsync(admin, lexeme.Id)).HasUkAudio.Should().BeFalse();
        (await WithDbAsync(db => db.MediaAssets.AnyAsync(a => a.Id == asset.Id))).Should().BeTrue();
        File.Exists(AudioPath(lexeme.Id, "en-GB")).Should().BeTrue();
        (await WithDbAsync(db => db.Senses.Where(s => s.Id == senseId).Select(s => s.AudioAssetId).SingleAsync())).Should().Be(asset.Id);

        // Second DELETE is a no-op.
        (await admin.DeleteAsync($"{Base}/{lexeme.Id}/audio/en-GB")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await WithDbAsync(db => db.MediaAssets.AnyAsync(a => a.Id == asset.Id))).Should().BeTrue();
    }

    [Fact]
    public async Task LexemesAdmin_RemoveAudio_NothingLinked_Returns204()
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");

        (await admin.DeleteAsync($"{Base}/{lexeme.Id}/audio/en-GB")).StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task LexemesAdmin_RemoveAudio_UnknownLexemeOrLocale_Returns404Or400()
    {
        HttpClient admin = await AdminAsync();
        Lexeme lexeme = await SeedLexemeAsync($"{Token()}word");

        (await admin.DeleteAsync($"{Base}/{Guid.NewGuid()}/audio/en-GB")).StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await admin.DeleteAsync($"{Base}/{lexeme.Id}/audio/en-AU")).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
