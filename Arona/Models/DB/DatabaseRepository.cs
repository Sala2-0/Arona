using LiteDB;
using Arona.Models.Api.Clans;

namespace Arona.Models.DB;

public interface IDatabaseRepository
{
    ILiteCollection<ClanView> Clans { get; }
    ILiteCollection<Guild> Guilds { get; }
    ILiteCollection<User> Users { get; }
    ILiteCollection<Ship> Ships { get; }
    ILiteCollection<LadderStructure> HurricaneLeaderboard { get; }
}

public class DatabaseRepository : IDatabaseRepository
{
    public ILiteCollection<ClanView> Clans { get; }
    public ILiteCollection<Guild> Guilds { get; }
    public ILiteCollection<User> Users { get; }
    public ILiteCollection<Ship> Ships { get; }
    public ILiteCollection<LadderStructure> HurricaneLeaderboard { get; }

    public DatabaseRepository(LiteDatabase db)
    {
        Clans = db.GetCollection<ClanView>("clans");
        Guilds = db.GetCollection<Guild>("guilds");
        Users = db.GetCollection<User>("users");
        Ships = db.GetCollection<Ship>("ships");
        HurricaneLeaderboard = db.GetCollection<LadderStructure>("hurricane_leaderboard");
    }
}