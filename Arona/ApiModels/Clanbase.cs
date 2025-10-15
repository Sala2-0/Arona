using System.Text.Json.Serialization;
using LiteDB;

using League = Arona.Utility.ClanUtils.League;
using Division = Arona.Utility.ClanUtils.Division;
using JsonSerializer = System.Text.Json.JsonSerializer;
using Team = Arona.Utility.ClanUtils.Team;
using StageType = Arona.Utility.ClanUtils.StageType;

namespace Arona.ApiModels;

internal static class ClanBase
{
    public class Base
    {
        [JsonPropertyName("clanview")]
        public required ClanView ClanView { get; set; }
    }

    public class ClanView
    {
        /// <summary>
        /// Unique identifier for the clan.
        /// </summary>
        [BsonId]
        public int Id { get; set; }

        [JsonPropertyName("clan")]
        public required Clan Clan { get; set; }

        [JsonPropertyName("wows_ladder")]
        public required WowsLadder WowsLadder { get; set; }

        public External ExternalData { get; set; } = new();
    }

    public class Clan
    {
        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("tag")]
        public required string Tag { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }


        [JsonPropertyName("color")]
        public required string Color { get; set; }
    }

    public class WowsLadder
    {
        [JsonPropertyName("prime_time")]
        public required int? PrimeTime { get; set; }

        [JsonPropertyName("planned_prime_time")]
        public required int? PlannedPrimeTime { get; set; }

        [JsonPropertyName("ratings")]
        public required List<Rating> Ratings { get; set; } = [];

        [JsonPropertyName("season_number")]
        public required int SeasonNumber { get; set; }

        [JsonPropertyName("last_battle_at")]
        public required string LastBattleAt { get; set; }

        [JsonPropertyName("league")]
        public required League League { get; set; }

        [JsonPropertyName("division")]
        public required Division Division { get; set; }

        [JsonPropertyName("division_rating")]
        public required int DivisionRating { get; set; }

        [JsonPropertyName("leading_team_number")]
        public required Team LeadingTeamNumber { get; set; }
    }

    public class Rating
    {
        [JsonPropertyName("team_number")]
        public required Team TeamNumber { get; set; }

        [JsonPropertyName("league")]
        public required League League { get; set; }

        [JsonPropertyName("division")]
        public required Division Division { get; set; }

        [JsonPropertyName("division_rating")]
        public required int DivisionRating { get; set; }

        [JsonPropertyName("stage")]
        public required Stage? Stage { get; set; }

        [JsonPropertyName("season_number")]
        public required int SeasonNumber { get; set; }

        [JsonPropertyName("public_rating")]
        public required int PublicRating { get; set; }

        [JsonPropertyName("battles_count")]
        public required int BattlesCount { get; set; }

        [JsonPropertyName("wins_count")]
        public required int WinsCount { get; set; }
    }

    public class Stage
    {
        [JsonPropertyName("type")]
        public required StageType Type { get; set; }

        [JsonPropertyName("target_league")]
        public required League TargetLeague { get; set; }

        [JsonPropertyName("target_division")]
        public required Division TargetDivision { get; set; }

        [JsonPropertyName("progress")]
        public required string[] Progress { get; set; }

        [JsonPropertyName("battles")]
        public required int Battles { get; set; }

        [JsonPropertyName("victories_required")]
        public required int VictoriesRequired { get; set; }
    }

    public class External
    {
        public string Region { get; set; }
        public int GlobalRank { get; set; }
        public int RegionRank { get; set; }
        public long? SessionEndTime { get; set; } = null;
        public List<RecentBattle> RecentBattles { get; set; } = [];
        public List<string> Guilds { get; set; } = [];
    }

    internal class RecentBattle
    {
        public required long BattleTime { get; init; }
        public required bool IsVictory { get; init; }
        public required int PointsEarned { get; init; }
        public required Team TeamNumber { get; set; }
    }

    public static async Task<ClanView> GetAsync(int clanId, string region)
    {
        using HttpClient client = new();
        var res = await client.GetAsync($"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/");
        res.EnsureSuccessStatusCode();

        var baseData = JsonSerializer.Deserialize<Base>(await res.Content.ReadAsStringAsync())!;

        return baseData.ClanView;
    }
}