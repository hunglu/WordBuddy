using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace WordBuddy.Content.IntegrationTests;

/// <summary>Reads every page of a paged list endpoint (<c>items/page/pageSize/totalCount</c>) so a
/// test that looks for one id is not hurt by rows other tests left in the shared database.</summary>
internal static class PagedHttp
{
    private const int PageSize = 100;

    /// <summary>All items of all pages, as one JSON array.</summary>
    public static async Task<JsonElement> GetAllAsArrayAsync(HttpClient client, string url)
    {
        List<JsonElement> all = [];
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
                all.Add(item.Clone());
            }

            int total = document.RootElement.GetProperty("totalCount").GetInt32();
            if (all.Count >= total || items.GetArrayLength() == 0)
            {
                break;
            }

            page++;
        }

        using JsonDocument array = JsonDocument.Parse(JsonSerializer.Serialize(all));
        return array.RootElement.Clone();
    }

    /// <summary>All items of all pages, deserialized.</summary>
    public static async Task<List<T>> GetAllAsync<T>(HttpClient client, string url, JsonSerializerOptions? options = null)
    {
        JsonElement array = await GetAllAsArrayAsync(client, url);
        return array.Deserialize<List<T>>(options ?? new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
    }
}
