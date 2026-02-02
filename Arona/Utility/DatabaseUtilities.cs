using Arona.Models.DB;

namespace Arona.Utility;

internal static class DatabaseUtilities
{
    public static Guild[] GetGuildsForClan(int clanId) =>
        Collections.Guilds.Find(guild => guild.Clans.Contains(clanId)).ToArray();
}