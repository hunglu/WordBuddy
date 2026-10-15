using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace WordBuddy.Identity.IntegrationTests;

/// <summary>Reads every page of a paged list endpoint (<c>items/page/pageSize/totalCount</c>) so a test
/// that looks for one id is not hurt by rows other tests left in the shared database.</summary>
internal static class PagedHttp
{
    private const int PageSize = 100;

    /// <summary>All items of all pages, deserialized.</summary>
    public static async Task<List<T>> GetAllAsync<T>(HttpClient client, string url, JsonSerializerOptions options)
    {
        List<T> all = [];
        int page = 1;
        while (true)
        {
            string separator = url.Contains('?') ? "&" : "?";
            HttpResponseMessage response = await client.GetAsync($"{url}{separator}page={page}&pageSize={PageSize}");
            string body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(HttpStatusCode.OK, body);

            using JsonDocument document = JsonDocument.Parse(body);
            JsonElement items = document.RootElement.GetProperty("items");
            foreach (JsonElement item in items.EnumerateArray())
            {
                all.Add(item.Deserialize<T>(options)!);
            }

            int total = document.RootElement.GetProperty("totalCount").GetInt32();
            if (all.Count >= total || items.GetArrayLength() == 0)
            {
                break;
            }

            page++;
        }

        return all;
    }

    /// <summary>One page, as JSON.</summary>
    public static async Task<JsonElement> GetPageAsync(HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        string body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(HttpStatusCode.OK, body);
        using JsonDocument document = JsonDocument.Parse(body);
        return document.RootElement.Clone();
    }
}
