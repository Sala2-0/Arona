using Arona.Models.DB;

namespace Arona.Utility;

internal static class DatabaseUtilities
{
    public static Guild[] GetGuildsForClan(IDatabaseRepository repository, int clanId) =>
        repository.Guilds.Find(guild => guild.Clans.Contains(clanId)).ToArray();
}