using MongoDB.Driver;

namespace Arona.Database;

internal class Collections
{
    public required IMongoCollection<Clan> Clans { get; init; }
    public required IMongoCollection<Guild> Guilds { get; init; }
    public required IMongoCollection<User> Users { get; init; }
}