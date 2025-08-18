using LiteDB;

namespace Arona.Database;

internal class Clan
{
    [BsonId] public required long Id { get; set; }
    [BsonField("region")] public required string Region { get; set; }
    [BsonField("clan_tag")] public required string ClanTag { get; set; }
    [BsonField("clan_name")] public required string ClanName { get; set; }
    [BsonField("recent_battles")] public required List<RecentBattle> RecentBattles { get; set; }
    [BsonField("prime_time")] public required PrimeTime PrimeTime { get; set; }
    [BsonField("ratings")] public required List<Rating> Ratings { get; set; } = [];
    [BsonField("global_rank")] public required int GlobalRank { get; set; }
    [BsonField("region_rank")] public required int RegionRank { get; set; }
    [BsonField("guilds")] public required List<string> Guilds { get; set; } = []; // Guild IDs
    [BsonField("session_end_time")] public required long? SessionEndTime { get; set; } = null;
}

internal class RecentBattle
{
    [BsonField("battle_time")] public required long BattleTime { get; init; }
    [BsonField("game_result")] public required string GameResult { get; init; }
    [BsonField("points_earned")] public required int PointsEarned { get; init; }
    [BsonField("team_number")] public required int TeamNumber { get; set; }
}

internal class PrimeTime
{
    [BsonField("planned")] public int? Planned { get; set; }
    [BsonField("active")] public int? Active { get; set; }
}

internal class Rating
{
    [BsonField("team_number")] public int TeamNumber { get; set; }
    [BsonField("league")] public int League { get; set; }
    [BsonField("division")] public int Division { get; set; }
    [BsonField("division_rating")] public int DivisionRating { get; set; }
    [BsonField("public_rating")] public int PublicRating { get; set; }
    [BsonField("stage")] public Stage? Stage { get; set; }
}

internal class Stage
{
    [BsonField("type")] public required string Type { get; set; }
    [BsonField("target_league")] public required int TargetLeague { get; set; }
    [BsonField("target_division")] public required int TargetDivision { get; set; }
    [BsonField("progress")] public required List<string> Progress { get; set; } = [];
    [BsonField("battles")] public required int Battles { get; set; }
    [BsonField("victories_required")] public required int VictoriesRequired { get; set; }
}