using Arona.ClanEvents;
using Arona.Models.DB;
using Arona.Services;
using Arona.Services.Message;
using Arona.Utility;
using Microsoft.Extensions.Hosting;
using NetCord.Rest;

namespace Arona.ClanEventHandlers;

internal class DiscordSessionStartedHandler : IEventHandler<ClanSessionStarted>, IHostedService
{
    private readonly IClanEventBus _eventBus;
    private readonly IChannelMessageService _channelMessageService;
    private readonly IDatabaseRepository _repository;

    public DiscordSessionStartedHandler(
        IClanEventBus eventBus,
        IChannelMessageService channelMessageService,
        IDatabaseRepository repository)
    {
        _eventBus = eventBus;
        _channelMessageService = channelMessageService;
        _repository = repository;
    }

    public async Task OnEventAsync(ClanSessionStarted evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(_repository, evt.ClanId);

        foreach (var guild in guilds)
        {
            await _channelMessageService.SendAsync(
                guildId: ulong.Parse(guild.Id),
                channelId: ulong.Parse(guild.ChannelId),
                properties: new MessageProperties
                {
                    Content = $"`[{evt.ClanTag}] {evt.ClanName} has started playing`"
                }
            );
        }
    }
    
    public Task StartAsync(CancellationToken cancellationToken) 
    {
        _eventBus.SessionStarted += OnEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) 
    {
        _eventBus.SessionStarted -= OnEventAsync;
        return Task.CompletedTask;
    }
}