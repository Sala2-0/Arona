using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.Models.Api.Official;

/// <summary>
/// Response deserialization model for API endpoint /wows/account/list/ which returns a list of players based on a name search.
/// </summary>
internal class Player
{
    [JsonPropertyName("account_id")]
    public required long AccountId { get; set; }

    [JsonPropertyName("nickname")]
    public required string Nickname { get; set; }
       
    public static async Task<Player[]> GetAsync(string query, string region)
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/account/list/?application_id={Config.WgApi}&search={query}");
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<ResponseArray<Player>>(await res.Content.ReadAsStringAsync())!;

        return data.Data;
    }
}