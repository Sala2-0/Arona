using Arona.Events;
using Arona.Models;
using Arona.Services;
using Arona.Services.Message;
using Arona.Utility;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Rest;

namespace Arona.Events.Handlers;

public class ClanBattleSessionEndedHandler(GatewayClient gatewayClient, ChannelMessageService channelMessageService) : IEventHandler<ClanBattleSessionEnded>, IHostedService
{
    public async Task OnEventAsync(ClanBattleSessionEnded evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(evt.ClanId);
        var self = await gatewayClient.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

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
            await channelMessageService.SendAsync(
                guildId: ulong.Parse(guild.Id),
                channelId: ulong.Parse(guild.ChannelId),
                new MessageProperties().AddEmbeds(embed)
            );
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        EventBus.SessionEnded += OnEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        EventBus.SessionEnded -= OnEventAsync;
        return Task.CompletedTask;
    }
}
