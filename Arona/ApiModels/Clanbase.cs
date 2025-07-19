namespace Arona.ApiModels;
using System.Text.Json.Serialization;

internal class Clanbase
{
    [JsonPropertyName("clanview")] public ClanView ClanView { get; set; }

    public static string GetApiUrl(string clanId, string region) =>
        $"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/";
}

internal class ClanView
{
    [JsonPropertyName("clan")] public Clan Clan { get; set; }
    [JsonPropertyName("wows_ladder")] public WowsLadder WowsLadder { get; set; }
}

internal class Clan
{
    [JsonPropertyName("tag")] public string Tag { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("color")] public string Color { get; set; }
}

internal class WowsLadder
{
    [JsonPropertyName("prime_time")] public int? PrimeTime { get; set; }
    [JsonPropertyName("planned_prime_time")] public int? PlannedPrimeTime { get; set; }
    [JsonPropertyName("ratings")] public List<Rating> Ratings { get; set; } = [];
    [JsonPropertyName("season_number")] public int SeasonNumber { get; set; }
    [JsonPropertyName("last_battle_at")] public string LastBattleAt { get; set; }
}

internal class Rating
{
    [JsonPropertyName("team_number")] public int TeamNumber { get; set; }
    [JsonPropertyName("league")] public int League { get; set; }
    [JsonPropertyName("division")] public int Division { get; set; }
    [JsonPropertyName("division_rating")] public int DivisionRating { get; set; }
    [JsonPropertyName("stage")] public Stage? Stage { get; set; }
    [JsonPropertyName("season_number")] public int SeasonNumber { get; set; }
    [JsonPropertyName("public_rating")] public int PublicRating { get; set; }
    [JsonPropertyName("battles_count")] public int BattlesCount { get; set; }
    [JsonPropertyName("wins_count")] public int WinsCount { get; set; }
}

internal class Stage
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("target_league")] public int TargetLeague { get; set; }
    [JsonPropertyName("target_division")] public int TargetDivision { get; set; }
    [JsonPropertyName("progress")] public string[] Progress { get; set; }
    [JsonPropertyName("battles")] public int Battles { get; set; }
    [JsonPropertyName("victories_required")] public int VictoriesRequired { get; set; }
}