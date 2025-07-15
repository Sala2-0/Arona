namespace Arona.ApiModels;
using System.Text.Json.Serialization;

class LadderStructure
{
    [JsonPropertyName("id")] public long Id { get; set; }
    [JsonPropertyName("rank")] public int Rank { get; set; }

    public static string GetApiTargetClanUrl(string clanId, string region, string realm = "global") =>
        $"https://clans.worldofwarships.{region}/api/ladder/structure/?clan_id={clanId}&realm={realm}";

    public static string ConvertRegion(string region) => region switch
    {
        "eu" => "eu",
        "com" => "us",
        "asia" => "sg",
        _ => "undefined"
    };
}
