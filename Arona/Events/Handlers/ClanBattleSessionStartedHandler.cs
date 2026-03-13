using Arona.Events;
using Arona.Services;
using Arona.Services.Message;
using Arona.Utility;
using Microsoft.Extensions.Hosting;
using NetCord.Rest;

namespace Arona.Events.Handlers;

public class ClanBattleSessionStartedHandler(ChannelMessageService channelMessageService) : IEventHandler<ClanBattleSessionStarted>, IHostedService
{
    public async Task OnEventAsync(ClanBattleSessionStarted evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(evt.ClanId);

        foreach (var guild in guilds)
        {
            await channelMessageService.SendAsync(
                guildId: ulong.Parse(guild.Id),
                channelId: ulong.Parse(guild.ChannelId),
                new MessageProperties().WithContent($"`[{evt.ClanTag}] {evt.ClanName} has started playing`")
            );
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        EventBus.SessionStarted += OnEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        EventBus.SessionStarted -= OnEventAsync;
        return Task.CompletedTask;
    }
}