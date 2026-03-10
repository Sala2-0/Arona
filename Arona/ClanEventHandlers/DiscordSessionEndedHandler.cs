using Arona.ClanEvents;
using Arona.Models;
using Arona.Models.DB;
using Arona.Services;
using Arona.Services.Message;
using Arona.Utility;
using Microsoft.Extensions.Hosting;
using NetCord.Gateway;
using NetCord.Rest;

namespace Arona.ClanEventHandlers;

internal class DiscordSessionEndedHandler : IEventHandler<ClanSessionEnded>, IHostedService
{
    private readonly IClanEventBus _eventBus;
    private readonly IChannelMessageService _channelMessageService;
    private readonly IDatabaseRepository _repository;
    private readonly GatewayClient _gatewayClient;

    public DiscordSessionEndedHandler(
        IClanEventBus eventBus,
        IChannelMessageService channelMessageService,
        IDatabaseRepository repository,
        GatewayClient client)
    {
        _eventBus = eventBus;
        _channelMessageService = channelMessageService;
        _repository = repository;
        _gatewayClient = client;
    }

    public async Task OnEventAsync(ClanSessionEnded evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(_repository, evt.ClanId);
        var botIconUrl = await BotUtilities.GetBotIconUrl(_gatewayClient);

        var embed = new SessionEmbed
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
            await _channelMessageService.SendAsync(
                guildId: ulong.Parse(guild.Id),
                channelId: ulong.Parse(guild.ChannelId),
                new MessageProperties
                {
                    Embeds = [embed]
                }
            );
        }
    }
    
    public Task StartAsync(CancellationToken cancellationToken) 
    {
        _eventBus.SessionEnded += OnEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) 
    {
        _eventBus.SessionEnded -= OnEventAsync;
        return Task.CompletedTask;
    }
}
