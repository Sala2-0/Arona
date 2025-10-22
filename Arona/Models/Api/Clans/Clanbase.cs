using System.Text.Json.Serialization;
using LiteDB;

using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Arona.Models.Api.Clans;

internal class Base
{
    [JsonPropertyName("clanview")]
    [BsonField("clanview")]
    public required ClanView ClanView { get; set; }
}

internal class ClanView
{
    /// <summary>
    /// Unique identifier for the clan.
    /// </summary>
    [BsonId]
    public int Id { get; set; }

    [JsonPropertyName("clan")]
    [BsonField("clan")]
    public required Clan Clan { get; set; }

    [JsonPropertyName("wows_ladder")]
    [BsonField("wows_ladder")]
    public required WowsLadder WowsLadder { get; set; }

    [BsonField("external_data")]
    public External ExternalData { get; set; } = new();
    
    public static async Task<ClanView> GetAsync(int clanId, string region)
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/");
        res.EnsureSuccessStatusCode();

        var baseData = JsonSerializer.Deserialize<Base>(await res.Content.ReadAsStringAsync())!;

        return baseData.ClanView;
    }
}

internal class Clan
{
    [JsonPropertyName("id")]
    [BsonField("id")]
    public required int Id { get; set; }

    [JsonPropertyName("tag")]
    [BsonField("tag")]
    public required string Tag { get; set; }

    [JsonPropertyName("name")]
    [BsonField("name")]
    public required string Name { get; set; }

    [JsonPropertyName("color")]
    [BsonField("color")]
    public required string Color { get; set; }
}

internal class WowsLadder
{
    [JsonPropertyName("prime_time")]
    [BsonField("prime_time")]
    public required int? PrimeTime { get; set; }

    [JsonPropertyName("planned_prime_time")]
    [BsonField("planned_prime_time")]
    public required int? PlannedPrimeTime { get; set; }

    [JsonPropertyName("ratings")]
    [BsonField("ratings")]
    public required List<Rating> Ratings { get; set; } = [];

    [JsonPropertyName("season_number")]
    [BsonField("season_number")]
    public required int SeasonNumber { get; set; }

    [JsonPropertyName("last_battle_at")]
    [BsonField("last_battle_at")]
    public required string LastBattleAt { get; set; }

    [JsonPropertyName("league")]
    [BsonField("league")]
    public required League League { get; set; }

    [JsonPropertyName("division")]
    [BsonField("division")]
    public required Division Division { get; set; }

    [JsonPropertyName("division_rating")]
    [BsonField("division_rating")]
    public required int DivisionRating { get; set; }

    [JsonPropertyName("leading_team_number")]
    [BsonField("leading_team_number")]
    public required Team LeadingTeamNumber { get; set; }
}

internal class Rating
{
    [JsonPropertyName("team_number")]
    [BsonField("team_number")]
    public required Team TeamNumber { get; set; }

    [JsonPropertyName("league")]
    [BsonField("league")]
    public required League League { get; set; }

    [JsonPropertyName("division")]
    [BsonField("division")]
    public required Division Division { get; set; }

    [JsonPropertyName("division_rating")]
    [BsonField("division_rating")]
    public required int DivisionRating { get; set; }

    [JsonPropertyName("stage")]
    [BsonField("stage")]
    public required Stage? Stage { get; set; }

    [JsonPropertyName("season_number")]
    [BsonField("season_number")]
    public required int SeasonNumber { get; set; }

    [JsonPropertyName("public_rating")]
    [BsonField("public_rating")]
    public required int PublicRating { get; set; }

    [JsonPropertyName("battles_count")]
    [BsonField("battles_count")]
    public required int BattlesCount { get; set; }

    [JsonPropertyName("wins_count")]
    [BsonField("wins_count")]
    public required int WinsCount { get; set; }
}

internal class Stage
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("type")]
    [BsonField("type")]
    public required StageType Type { get; set; }

    [JsonPropertyName("target_league")]
    [BsonField("target_league")]
    public required League TargetLeague { get; set; }

    [JsonPropertyName("target_division")]
    [BsonField("target_division")]
    public required Division TargetDivision { get; set; }

    [JsonPropertyName("progress")]
    [BsonField("progress")]
    public required string[] Progress { get; set; }

    [JsonPropertyName("battles")]
    [BsonField("battles")]
    public required int Battles { get; set; }

    [JsonPropertyName("victories_required")]
    [BsonField("victories_required")]
    public required int VictoriesRequired { get; set; }
}

internal class External
{
    [BsonField("region")]
    public string Region { get; set; }

    [BsonField("global_rank")]
    public int GlobalRank { get; set; }

    [BsonField("region_rank")]
    public int RegionRank { get; set; }
    
    [BsonField("session_end_time")]
    public long? SessionEndTime { get; set; } = null;

    [BsonField("recent_battles")]
    public List<RecentBattle> RecentBattles { get; set; } = [];

    [BsonField("guilds")]
    public List<string> Guilds { get; set; } = [];
}

internal class RecentBattle
{
    [BsonField("battle_time")]
    public required long BattleTime { get; init; }

    [BsonField("is_victory")]
    public required bool IsVictory { get; init; }

    [BsonField("points_earned")]
    public required int PointsEarned { get; init; }

    [BsonField("team_number")]
    public required Team TeamNumber { get; set; }
}