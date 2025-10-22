using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.Models.Api.Clans;

internal class LadderStructure
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
    
    [JsonPropertyName("tag")]
    public required string Tag { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("rank")]
    public required int Rank { get; init; }
    
    [JsonPropertyName("division_rating")]
    public required int DivisionRating { get; init; }
    
    [JsonPropertyName("realm")]
    public required string Realm { get; init; }
    
    [JsonPropertyName("public_rating")]
    public required int PublicRating { get; init; }
    
    [JsonPropertyName("battles_count")]
    public required int BattlesCount { get; init; }

    public double? SuccessFactor { get; set; }

    public static async Task<LadderStructure[]> GetAsync(int clanId, string region, string realm = "global")
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://clans.worldofwarships.{region}/api/ladder/structure/?clan_id={clanId}&realm={realm}");
        res.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<LadderStructure[]>(await res.Content.ReadAsStringAsync())!;
    }

    public static async Task<LadderStructure[]> GetAsync(int league, int division, string realm)
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://clans.worldofwarships.eu/api/ladder/structure/?league={league}&division={division}&realm={realm}");
        res.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<LadderStructure[]>(await res.Content.ReadAsStringAsync())!;
    }

    public static async Task<LadderStructure[]> GetAsync(int? season, int league, int division)
    {
        var url = season.HasValue
            ? $"https://clans.worldofwarships.eu/api/ladder/structure/?season={season.Value}&division={division}&league={league}"
            : $"https://clans.worldofwarships.eu/api/ladder/structure/?division={division}&league={league}";

        using HttpClient client = new();
        var res = await client.GetAsync(url);
        res.EnsureSuccessStatusCode();

        return JsonSerializer.Deserialize<LadderStructure[]>(await res.Content.ReadAsStringAsync())!;
    }
}
