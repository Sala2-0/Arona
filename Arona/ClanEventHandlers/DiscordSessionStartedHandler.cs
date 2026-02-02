using Arona.ClanEvents;
using Arona.Services;
using Arona.Utility;

namespace Arona.ClanEventHandlers;

internal static class DiscordSessionStartedHandler
{
    public static void Register()
    {
        ClanEventBus.SessionStarted += OnSessionStartedAsync;
    }

    private static async Task OnSessionStartedAsync(ClanSessionStarted evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(evt.ClanId);

        foreach (var guild in guilds)
        {
            await Program.Client!.Rest.SendMessageAsync(
                channelId: ulong.Parse(guild.ChannelId),
                message: $"`[{evt.ClanTag}] {evt.ClanName} has started playing`"
            );
        }
    }
}