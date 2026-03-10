using LiteDB;
using NetCord;

namespace Arona.Models.DB;

public class User : IEntity
{
    [BsonId]
    public string Id { get; set; }

    [BsonField("custom_leaderboard")]
    public CustomLeaderboard? CustomLeaderboard { get; set; }
}

public class CustomLeaderboard
{
    [BsonField("region")]
    public required Region Region { get; set; }

    [BsonField("clans")]
    public required List<int> Clans { get; set; }
}