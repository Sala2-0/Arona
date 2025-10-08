using System.Text.Json.Serialization;

namespace Arona.ApiModels;

internal class LadderStructure
{
    [JsonPropertyName("id")] public required int Id { get; init; }
    [JsonPropertyName("tag")] public required string Tag { get; init; }
    [JsonPropertyName("name")] public required string Name { get; init; }
    [JsonPropertyName("rank")] public required int Rank { get; init; }
    [JsonPropertyName("division_rating")] public required int DivisionRating { get; init; }
    [JsonPropertyName("realm")] public required string Realm { get; init; }
    [JsonPropertyName("public_rating")] public required int PublicRating { get; init; }
    [JsonPropertyName("battles_count")] public required int BattlesCount { get; init; }

    public double? SuccessFactor { get; set; }

    public static string GetApiTargetClanUrl(string clanId, string region, string realm = "global") =>
        $"https://clans.worldofwarships.{region}/api/ladder/structure/?clan_id={clanId}&realm={realm}";

    public static string GetApiGeneralUrl(int league, int division, string realm) =>
        $"https://clans.worldofwarships.eu/api/ladder/structure/?league={league}&division={division}&realm={realm}";

    public static string GetUrl(int? season, int league, int division) => season.HasValue
        ? $"https://clans.worldofwarships.eu/api/ladder/structure/?season={season.Value}&division={division}&league={league}"
        : $"https://clans.worldofwarships.eu/api/ladder/structure/?division={division}&league={league}";

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
