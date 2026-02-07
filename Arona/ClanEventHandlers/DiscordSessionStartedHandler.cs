using Arona.ClanEvents;
using Arona.Services;
using Arona.Services.Message;
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
            await ChannelMessageService.SendAsync(
                guildId: ulong.Parse(guild.Id),
                channelId: ulong.Parse(guild.ChannelId),
                message: $"`[{evt.ClanTag}] {evt.ClanName} has started playing`"
            );
        }
    }
}