using MongoDB.Bson.Serialization.Attributes;

namespace Arona.Database;

internal class User
{
    [BsonId] public required string Id { get; set; }
    [BsonElement("account_id")] public required int AccountId { get; set; }
    [BsonElement("account_region")] public required string AccountRegion { get; set; }
}