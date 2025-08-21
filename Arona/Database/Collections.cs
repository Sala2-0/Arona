using LiteDB;

namespace Arona.Database;

internal class Collections
{
    public static ILiteCollection<Clan> Clans { get; private set; }
    public static ILiteCollection<Guild> Guilds { get; private set; }
    public static ILiteCollection<User> Users { get; private set; }
    public static ILiteCollection<Account> Accounts { get; private set; }

    public static void Initialize(LiteDatabase db)
    {
        Clans = db.GetCollection<Clan>("clans");
        Guilds = db.GetCollection<Guild>("guilds");
        Users = db.GetCollection<User>("users");
        Accounts = db.GetCollection<Account>("accounts");
    }
}