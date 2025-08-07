using System.Text.Json.Serialization;

namespace Arona.ApiModels;

internal class LadderStructure
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("tag")] public string Tag { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("rank")] public int Rank { get; set; }
    [JsonPropertyName("division_rating")] public int DivisionRating { get; set; }
    [JsonPropertyName("realm")] public string Realm { get; set; }
    [JsonPropertyName("public_rating")] public int PublicRating { get; set; }
    [JsonPropertyName("battles_count")] public int BattlesCount { get; set; }

    public static string GetApiTargetClanUrl(string clanId, string region, string realm = "global") =>
        $"https://clans.worldofwarships.{region}/api/ladder/structure/?clan_id={clanId}&realm={realm}";

    public static string GetApiGeneralUrl(int league, int division, string realm) =>
        $"https://clans.worldofwarships.eu/api/ladder/structure/?league={league}&division={division}&realm={realm}";

    public static string ConvertRegion(string region) => region switch
    {
        "eu" or "EU" => "eu",
        "com" or "COM" => "us",
        "asia" or "ASIA" => "sg",
        _ => "undefined"
    };

    public static string ConvertRealm(string realm) => realm switch
    {
        "global" => "Global",
        "eu" => "EU",
        "sg" => "ASIA",
        "us" => "NA",
        _ => "undefined"
    };
}
