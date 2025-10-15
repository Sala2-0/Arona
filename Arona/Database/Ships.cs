using LiteDB;

namespace Arona.Database;

internal class Ship
{
    [BsonId]
    public required long Id { get; set; }

    [BsonField("name")]
    public required string Name { get; set; }

    [BsonField("short_name")]
    public required string ShortName { get; set; }

    [BsonField("tier")]
    public required int Tier { get; set; }
}
