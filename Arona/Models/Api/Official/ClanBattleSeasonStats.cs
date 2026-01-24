using System.Text.Json;
using System.Text.Json.Serialization;
using Arona.Utility;

namespace Arona.Models.Api.Official;

internal record PlayerClanBattleSeasonStatsRequest(string Region, long AccountId);

internal class PlayerClanBattleSeasonStatsQuery(HttpClient client) : QueryBase<PlayerClanBattleSeasonStatsRequest, ResponseObject<PlayerClanBattleSeasonStats>>(client)
{
    public override async Task<ResponseObject<PlayerClanBattleSeasonStats>> GetAsync(PlayerClanBattleSeasonStatsRequest req) =>
        await SendAndDeserializeAsync($"https://api.worldofwarships.{req.Region}/wows/clans/seasonstats/?application_id={Config.WgApi}&account_id={req.AccountId}");

    public static async Task<ResponseObject<PlayerClanBattleSeasonStats>> GetSingleAsync(PlayerClanBattleSeasonStatsRequest request)
    {
        var apiQuery = new PlayerClanBattleSeasonStatsQuery(ApiClient.Instance);
        return await apiQuery.GetAsync(request);
    }
}

/// <summary>
/// Response deserialization model for API endpoint /wows/clans/seasonstats/ which returns clan battle stats for a player.
/// </summary>
internal class PlayerClanBattleSeasonStats
{
    [JsonPropertyName("seasons")]
    public required Season[] Seasons { get; set; }

    public class Season
    {
        [JsonPropertyName("season_id")]
        public required int SeasonId { get; set; }

        [JsonPropertyName("battles")]
        public required int Battles { get; set; }

        [JsonPropertyName("wins")]
        public required int Wins { get; set; }

        [JsonPropertyName("losses")]
        public required int Losses { get; set; }

        [JsonPropertyName("draws")]
        public required int Draws { get; set; }

        [JsonPropertyName("damage_dealt")]
        public required int DamageDealt { get; set; }

        [JsonPropertyName("frags")]
        public required int Kills { get; set; }
    }

    public static async Task<Dictionary<string, PlayerClanBattleSeasonStats>> GetAsync(string accountId, string region)
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/clans/seasonstats/?application_id={Config.WgApi}&account_id={accountId}");
        res.EnsureSuccessStatusCode();

        var data = JsonSerializer.Deserialize<ResponseObject<PlayerClanBattleSeasonStats>>(await res.Content.ReadAsStringAsync())!;

        return data.Data;
    }
}