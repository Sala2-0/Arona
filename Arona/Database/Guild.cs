namespace Arona.Database;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

internal class Guild
{
    [BsonId] public required string Id { get; set; }
    [BsonElement("channel_id")] public required string ChannelId { get; set; }
    [BsonElement("clans")] public Dictionary<string, Clan> Clans { get; set; } = [];
    [BsonElement("builds")] public List<Build> Builds { get; set; } = [];
}

internal class Clan
{
    [BsonElement("clan_id")] public required long ClanId { get; set; }
    [BsonElement("region")] public required string Region { get; set; }
    [BsonElement("clan_tag")] public required string ClanTag { get; set; }
    [BsonElement("clan_name")] public required string ClanName { get; set; }
    [BsonElement("recent_battles")] public required List<BsonDocument> RecentBattles { get; set; } = [];
    [BsonElement("prime_time")] public required PrimeTime PrimeTime { get; set; }
    [BsonElement("ratings")] public required List<Rating> Ratings { get; set; } = [];
    [BsonElement("global_rank")] public required int GlobalRank { get; set; }
    [BsonElement("region_rank")] public required int RegionRank { get; set; }
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

internal class Build
{
    [BsonElement("name")] public required string Name { get; set; }
    [BsonElement("link")] public required string Link { get; set; }
    [BsonElement("creator_name")] public required string CreatorName { get; set; }
    [BsonElement("description")] public string? Description { get; set; } = null;
    [BsonElement("color")] public string? Color { get; set; } = null; // SKALL VARA I HEX-FORMAT
}