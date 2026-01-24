using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.Models.Api.Official;

internal record EmptyRequest;

internal class ClanBattleSeasonsQuery(HttpClient client) : QueryBase<EmptyRequest, ResponseObject<ClanBattleSeasons>>(client)
{
    public override async Task<ResponseObject<ClanBattleSeasons>> GetAsync(EmptyRequest req) =>
        await SendAndDeserializeAsync("https://api.worldofwarships.eu/wows/clans/season/?application_id={Config.WgApi}");
}

/// <summary>
/// Response deserialization model for API endpoint /wows/clans/season/ which returns a dictionary of clan battle season data for each season.
/// </summary>
internal class ClanBattleSeasons
{
    [JsonPropertyName("season_id")]
    public required int SeasonId { get; set; }

    [JsonPropertyName("start_time")]
    public required long StartTime { get; set; }

    [JsonPropertyName("finish_time")]
    public required long FinishTime { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    public static async Task<Dictionary<string, ClanBattleSeasons>> GetAsync()
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://api.worldofwarships.eu/wows/clans/season/?application_id={Config.WgApi}");
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<ResponseObject<ClanBattleSeasons>>(await res.Content.ReadAsStringAsync())!;

        return data.Data;
    }
}