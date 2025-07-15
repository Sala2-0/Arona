namespace Arona.ApiModels;
using System.Text.Json.Serialization;

public class Clanbase
{
    [JsonPropertyName("clanview")] public ClanView ClanView { get; set; }

    public static string GetApiUrl(string clanId, string region) =>
        $"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/";
}

public class ClanView
{
    [JsonPropertyName("clan")] public Clan Clan { get; set; }
    [JsonPropertyName("wows_ladder")] public WowsLadder WowsLadder { get; set; }
}

public class Clan
{
    [JsonPropertyName("tag")] public string Tag { get; set; }
    [JsonPropertyName("name")] public string Name { get; set; }
    [JsonPropertyName("id")] public int Id { get; set; }
    [JsonPropertyName("color")] public string Color { get; set; }
}

public class WowsLadder
{
    [JsonPropertyName("prime_time")] public int? PrimeTime { get; set; }
    [JsonPropertyName("planned_prime_time")] public int PlannedPrimeTime { get; set; }
    [JsonPropertyName("ratings")] public List<Rating> Ratings { get; set; } = [];
    [JsonPropertyName("season_number")] public int SeasonNumber { get; set; }
    [JsonPropertyName("last_battle_at")] public string LastBattleAt { get; set; }
}

public class Rating
{
    [JsonPropertyName("team_number")] public int TeamNumber { get; set; }
    [JsonPropertyName("league")] public int League { get; set; }
    [JsonPropertyName("division")] public int Division { get; set; }
    [JsonPropertyName("division_rating")] public int DivisionRating { get; set; }
    [JsonPropertyName("stage")] public Stage? Stage { get; set; }
    [JsonPropertyName("season_number")] public int SeasonNumber { get; set; }
}

public class Stage
{
    [JsonPropertyName("type")] public string Type { get; set; }
    [JsonPropertyName("target_league")] public int TargetLeague { get; set; }
    [JsonPropertyName("target_division")] public int TargetDivision { get; set; }
    [JsonPropertyName("progress")] public string[] Progress { get; set; }
    [JsonPropertyName("battles")] public int Battles { get; set; }
    [JsonPropertyName("victories_required")] public int VictoriesRequired { get; set; }
}