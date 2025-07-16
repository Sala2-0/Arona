namespace Arona.Database;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

internal class Guild
{
    [BsonId] public string Id { get; set; }
    [BsonElement("channel_id")] public string ChannelId { get; set; }
    [BsonElement("clans")] public Dictionary<string, Clan>? Clans { get; set; }
}

internal class Clan
{
    [BsonElement("clan_id")] public long ClanId { get; set; }
    [BsonElement("region")] public string Region { get; set; }
    [BsonElement("clan_tag")] public string ClanTag { get; set; }
    [BsonElement("clan_name")] public string ClanName { get; set; }
    [BsonElement("recent_battles")] public List<BsonDocument> RecentBattles { get; set; } = new();
    [BsonElement("prime_time")] public PrimeTime PrimeTime { get; set; }
    [BsonElement("ratings")] public List<Rating> Ratings { get; set; } = [];
    [BsonElement("global_rank")] public int GlobalRank { get; set; }
    [BsonElement("region_rank")] public int RegionRank { get; set; }
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
    [BsonElement("type")] public string Type { get; set; }
    [BsonElement("target_league")] public int TargetLeague { get; set; }
    [BsonElement("target_division")] public int TargetDivision { get; set; }
    [BsonElement("progress")] public List<string> Progress { get; set; } = [];
    [BsonElement("battles")] public int Battles { get; set; }
    [BsonElement("victories_required")] public int VictoriesRequired { get; set; }
}