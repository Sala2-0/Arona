using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.Models.Api.Official;

/// <summary>
/// Response deserialization model for API endpoint /wows/clans/list/ which returns a list of clans based on a search query.
/// </summary>
internal class Clan
{
    /// <summary>
    /// Total number of members in the clan.
    /// </summary>
    [JsonPropertyName("members_count")]
    public required int MemberCount { get; set; }

    /// <summary>
    /// Clan creation date as a Unix timestamp.
    /// </summary>
    [JsonPropertyName("created_at")]
    public required long CreatedAt { get; set; }

    [JsonPropertyName("clan_id")]
    public required int ClanId { get; set; }

    [JsonPropertyName("tag")]
    public required string Tag { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    public static async Task<Clan[]> GetAsync(string query, string region)
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/clans/list/?application_id={Config.WgApi}&search={query}");
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<ResponseArray<Clan>>(await res.Content.ReadAsStringAsync())!;

        return data.Data;
    }
}