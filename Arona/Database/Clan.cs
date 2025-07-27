namespace Arona.Database;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

internal class Clan
{
    [BsonId] public required long Id { get; set; }
    [BsonElement("region")] public required string Region { get; set; }
    [BsonElement("clan_tag")] public required string ClanTag { get; set; }
    [BsonElement("clan_name")] public required string ClanName { get; set; }
    [BsonElement("recent_battles")] public required List<RecentBattle> RecentBattles { get; set; }
    [BsonElement("prime_time")] public required PrimeTime PrimeTime { get; set; }
    [BsonElement("ratings")] public required List<Rating> Ratings { get; set; } = [];
    [BsonElement("global_rank")] public required int GlobalRank { get; set; }
    [BsonElement("region_rank")] public required int RegionRank { get; set; }
    [BsonElement("guilds")] public required List<string> Guilds { get; set; } = []; // Guild IDs
    [BsonElement("session_end_time")] public required long? SessionEndTime { get; set; } = null;
}

internal class RecentBattle
{
    [BsonElement("battle_time")] public required long BattleTime { get; init; }
    [BsonElement("game_result")] public required string GameResult { get; init; }
    [BsonElement("points_earned")] public required int PointsEarned { get; init; }
    [BsonElement("team_number")] public required int TeamNumber { get; set; }
}

internal class PrimeTime
{
    [BsonElement("planned")] public int? Planned { get; set; }
    [BsonElement("active")] public int? Active { get; set; }
}

internal class Rating
{
    [BsonElement("team_number")] public int TeamNumber { get; set; }
    [BsonElement("league")] public int League { get; set; }
    [BsonElement("division")] public int Division { get; set; }
    [BsonElement("division_rating")] public int DivisionRating { get; set; }
    [BsonElement("public_rating")] public int PublicRating { get; set; }
    [BsonElement("stage")] public Stage? Stage { get; set; }
}

internal class Stage
{
    [BsonElement("type")] public required string Type { get; set; }
    [BsonElement("target_league")] public required int TargetLeague { get; set; }
    [BsonElement("target_division")] public required int TargetDivision { get; set; }
    [BsonElement("progress")] public required List<string> Progress { get; set; } = [];
    [BsonElement("battles")] public required int Battles { get; set; }
    [BsonElement("victories_required")] public required int VictoriesRequired { get; set; }
}