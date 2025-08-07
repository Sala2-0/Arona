using MongoDB.Bson.Serialization.Attributes;

namespace Arona.Database;

internal class Guild
{
    [BsonId] public required string Id { get; set; }
    [BsonElement("channel_id")] public required string ChannelId { get; set; }
    [BsonElement("clans")] public List<long> Clans { get; set; } = [];
    [BsonElement("builds")] public List<Build> Builds { get; set; } = [];
}

internal class Build
{
    [BsonElement("name")] public required string Name { get; set; }
    [BsonElement("link")] public required string Link { get; set; }
    [BsonElement("creator_name")] public required string CreatorName { get; set; }
    [BsonElement("description")] public string? Description { get; set; } = null;
    [BsonElement("color")] public string? Color { get; set; } = null; // SKALL VARA I HEX-FORMAT
}