using LiteDB;
using Arona.Utility;

namespace Arona.Models.DB;

public class Ship
{
    [BsonId]
    public required long Id { get; set; }

    [BsonField("name")]
    public required string Name { get; set; }

    [BsonField("short_name")]
    public required string ShortName { get; set; }

    [BsonField("tier")]
    public required  Text.Tier Tier { get; set; }
}
