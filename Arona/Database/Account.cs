using JsonSerializer = System.Text.Json.JsonSerializer;
using LiteDB;
using Arona.ApiModels;

namespace Arona.Database;

internal class Account
{
    [BsonId] public required int Id { get; set; }
    [BsonField("last_request")] public required long LastRequest { get; set; }
    [BsonField("last_update")] public required long LastUpdate { get; set; }
    [BsonField("data")] public required Dictionary<string, Dictionary<string, Stats>> Data { get; set; }

    public static async Task CreateAsync(int accountId, string region)
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
            foreach (var i in Enum.GetValues<Mode>())
            {
                string mode = i.ToString();

                var res = await client.GetAsync($"https://vortex.worldofwarships.{region}/api/accounts/{accountId}/ships/{mode}/");
                res.EnsureSuccessStatusCode();

                string statistics = Vortex.Extract(await res.Content.ReadAsStringAsync());
                
                var data = JsonSerializer.Deserialize<Dictionary<string, Entry>>(statistics)!;

                foreach (var entry in data)
                {
                    var shipId = entry.Key;
                    var modeData = entry.Value;

                    if (!account.Data.TryGetValue(shipId, out var modes))
                        account.Data[shipId] = new Dictionary<string, Stats>();

                    account.Data[shipId][mode] = new Stats
                    {
                        BattlesCount = modeData.GetModeData(mode).BattlesCount,
                        Wins = modeData.GetModeData(mode).Wins,
                        DamageDealt = modeData.GetModeData(mode).DamageDealt,
                        Frags = modeData.GetModeData(mode).Frags,
                        PlanesKilled = modeData.GetModeData(mode).PlanesKilled,
                        OriginalExp = modeData.GetModeData(mode).OriginalExp,
                        ArtAgro = modeData.GetModeData(mode).ArtAgro,
                        ScoutingDamage = modeData.GetModeData(mode).ScoutingDamage,
                        ShotsByMain = modeData.GetModeData(mode).ShotsByMain,
                        HitsByMain = modeData.GetModeData(mode).HitsByMain
                    };
                }

                Console.WriteLine(account.Data["3539875824"][mode].BattlesCount);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching account info: {ex.Message}");
        }

        Collections.Accounts.Upsert(account);
    }
}

internal class Stats
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
    pvp,
    pvp_solo,
    pvp_div2,
    pvp_div3,
    rank_solo
}