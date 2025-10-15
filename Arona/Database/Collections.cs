using LiteDB;
using Arona.ApiModels;

namespace Arona.Database;

internal class Collections
{
    public static ILiteCollection<ClanBase.ClanView> Clans { get; private set; }
    public static ILiteCollection<Guild> Guilds { get; private set; }
    public static ILiteCollection<User> Users { get; private set; }
    public static ILiteCollection<Ship> Ships { get; private set; }

    public static void Initialize(LiteDatabase db)
    {
        Clans = db.GetCollection<ClanBase.ClanView>("clans");
        Guilds = db.GetCollection<Guild>("guilds");
        Users = db.GetCollection<User>("users");
        Ships = db.GetCollection<Ship>("ships");
    }
}