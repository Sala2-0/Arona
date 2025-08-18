using LiteDB;

namespace Arona.Database;

internal class User
{
    [BsonId] public required string Id { get; set; }
    [BsonField("account_id")] public required int AccountId { get; set; }
    [BsonField("account_region")] public required string AccountRegion { get; set; }
}