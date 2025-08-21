using JsonSerializer = System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using LiteDB;
using VortexAccount = Arona.ApiModels.Account;

namespace Arona.Database;

internal class Account
{
    [BsonId] public required int Id { get; set; }
    [BsonField("last_request")] public required long LastRequest { get; set; }
    [BsonField("last_update")] public required long LastUpdate { get; set; }
    [BsonField("data")] public required Dictionary<long, ShipData> Data { get; set; }

    public static async Task<Account> CreateAsync(int accountId, string region)
    {
        var unixNow = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var account = new Account
        {
            Id = accountId,
            LastRequest = unixNow,
            LastUpdate = unixNow,
            Data = []
        };

        using var client = new HttpClient();

        try
        {
            foreach (var mode in Enum.GetValues<Mode>())
            {
                Console.WriteLine($"Fetching data for account {accountId} in mode {mode.ToString().ToLower()}...");
                var res = await client.GetAsync($"https://vortex.worldofwarships.{region}/api/accounts/{accountId}/ships/{mode.ToString().ToLower()}/");
                res.EnsureSuccessStatusCode();

                var accountInfo = JsonSerializer.Deserialize<VortexAccount>(await res.Content.ReadAsStringAsync());

                throw new NotImplementedException("This section is not implemented yet.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching account info: {ex.Message}");
        }

        return account;
    }
}

internal class ShipData
{
    [BsonField("modes")] public required Dictionary<Mode, ModeStats> Modes {get;set;}
}

internal class ModeStats
{
    [BsonField("battles_count")] public required int BattlesCount { get; set; }
    [BsonField("wins")] public required int Wins { get; set; }
    [BsonField("damage_dealt")] public required long DamageDealt { get; set; }
    [BsonField("frags")] public required int Frags { get; set; }
    [BsonField("planes_killed")] public required int PlanesKilled { get; set; }
    [BsonField("original_exp")] public required long OriginalExp { get; set; }
    [BsonField("art_agro")] public required long ArtAgro { get; set; }
    [BsonField("scouting_damage")] public required long ScoutingDamage { get; set; }
    [BsonField("shots_by_main")] public required int ShotsByMain { get; set; }
    [BsonField("hits_by_main")] public required int HitsByMain { get; set; }
}

internal enum Mode
{
    [BsonField("pvp"), JsonPropertyName("pvp")] PVP,
    [BsonField("pvp_solo"), JsonPropertyName("pvp_solo")] PVP_SOLO,
    [BsonField("pvp_div2"), JsonPropertyName("pvp_div2")] PVP_DIV2,
    [BsonField("pvp_div3"), JsonPropertyName("pvp_div3")] PVP_DIV3,
    [BsonField("rank_solo"), JsonPropertyName("rank_solo")] RANK_SOLO
}