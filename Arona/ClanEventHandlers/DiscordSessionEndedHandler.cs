using Arona.ClanEvents;
using Arona.Models;
using Arona.Services;
using Arona.Utility;
using Arona.Services.UpdateTasks;

namespace Arona.ClanEventHandlers;

internal static class DiscordSessionEndedHandler
{
    public static void Register()
    {
        ClanEventBus.SessionEnded += OnSessionEndedAsync;
    }

    private static async Task OnSessionEndedAsync(ClanSessionEnded evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(evt.ClanId);
        var botIconUrl = await BotUtilities.GetBotIconUrl();

        var embed =  new SessionEmbed
        {
            IconUrl = botIconUrl,
            ClanFullName = $"[{evt.ClanTag}] {evt.ClanName}",
            BattlesCount = evt.BattlesCount,
            Date = evt.Date.ToString(),
            Points = evt.TotalPoints,
            WinsCount = evt.WinsCount
        }.CreateEmbed();

        foreach (var guild in guilds)
        {
            await UpdateTasks.SendMessageAsync(ulong.Parse(guild.ChannelId), embed);
        }
    }
}
