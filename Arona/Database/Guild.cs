using LiteDB;

namespace Arona.Database;

internal class Guild
{
    [BsonId] public required string Id { get; set; }
    [BsonField("channel_id")] public required string ChannelId { get; set; }
    [BsonField("clans")] public List<int> Clans { get; set; } = [];
    [BsonField("builds")] public List<Build> Builds { get; set; } = [];
}

internal class Build
{
    [BsonField("name")] public required string Name { get; set; }
    [BsonField("link")] public required string Link { get; set; }
    [BsonField("creator_name")] public required string CreatorName { get; set; }
    [BsonField("description")] public string? Description { get; set; } = null;
    [BsonField("color")] public string? Color { get; set; } = null; // SKALL VARA I HEX-FORMAT
}