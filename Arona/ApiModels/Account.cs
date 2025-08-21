using System.Text.Json;
using System.Text.Json.Serialization;
using Arona.Database;

namespace Arona.ApiModels;

internal class Vortex
{
    public static string Extract(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var statistics = doc.RootElement
            .GetProperty("data")
            .EnumerateObject()
            .First() // Konto id
            .Value
            .GetProperty("statistics");
        
        return statistics.GetRawText();
    }
}

internal class Entry
{
    [JsonPropertyName("pvp")] public Data? Pvp { get; set; }
    [JsonPropertyName("pvp_solo")] public Data? PvpSolo { get; set; }
    [JsonPropertyName("pvp_div2")] public Data? PvpDiv2 { get; set; }
    [JsonPropertyName("pvp_div3")] public Data? PvpDiv3 { get; set; }
    [JsonPropertyName("rank_solo")] public Data? RankSolo { get; set; }

    public Data GetModeData(string mode) => mode switch
    {
        "pvp" => Pvp!,
        "pvp_solo" => PvpSolo!,
        "pvp_div2" => PvpDiv2!,
        "pvp_div3" => PvpDiv3!,
        "rank_solo" => RankSolo!,
        _ => throw new Exception("Invalid mode")
    };
}

internal class Data
{
    [JsonPropertyName("battles_count")] public int BattlesCount { get; set; }
    [JsonPropertyName("wins")] public int Wins { get; set; }
    [JsonPropertyName("damage_dealt")] public long DamageDealt { get; set; }
    [JsonPropertyName("frags")] public int Frags { get; set; }
    [JsonPropertyName("planes_killed")] public int PlanesKilled { get; set; }
    [JsonPropertyName("original_exp")] public long OriginalExp { get; set; }
    [JsonPropertyName("art_agro")] public long ArtAgro { get; set; }
    [JsonPropertyName("scouting_damage")] public long ScoutingDamage { get; set; }
    [JsonPropertyName("shots_by_main")] public int ShotsByMain { get; set; }
    [JsonPropertyName("hits_by_main")] public int HitsByMain { get; set; }
}