using System.Text.Json;
using System.Security.Authentication;
using NetCord.Rest;
using NetCord;
using NetCord.Gateway;
using Arona.ClanEvents;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Services;
using Arona.Utility;
using Arona.Models.Dto;
using Arona.Services.Message;
using Microsoft.Extensions.Hosting;

namespace Arona.ClanEventHandlers;

public class DiscordBattleDetectedHandler : IEventHandler<BattleDetected>, IHostedService
{
    private readonly IClanEventBus _eventBus;
    private readonly GatewayClient _client;
    private readonly IApiClient _apiClient;
    private readonly IChannelMessageService _channelMessageService;
    private readonly IDatabaseRepository _repository;
    private readonly IErrorService _errorService;

    public DiscordBattleDetectedHandler(
        IClanEventBus eventBus,
        GatewayClient client,
        IApiClient apiClient,
        IChannelMessageService channelMessageService,
        IDatabaseRepository repository,
        IErrorService errorService)
    {
        _eventBus = eventBus;
        _client = client;
        _apiClient = apiClient;
        _channelMessageService = channelMessageService;
        _repository = repository;
        _errorService = errorService;
    }

    public async Task OnEventAsync(BattleDetected evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(_repository, evt.ClanId);

        var ladderBattlesQuery = new LadderBattlesQuery(_apiClient.HttpClient);

        foreach (var guild in guilds)
        {
            var battleId = $"{evt.ClanId}_{evt.BattleTime}";
            
            var dto = new BattleResultDto
            {
                ClanName = evt.ClanName,
                ClanTag = evt.ClanTag,
                Division = evt.Division,
                DivisionRating = evt.DivisionRating,
                League = evt.League,
                IsVictory = evt.IsVictory,
                PointsDelta = evt.PointsDelta,
                Stage = evt.Stage != null ? new StageDto(evt.Stage.Type, evt.Stage.Progress) : null,
            };
            
            if (guild.Cookies.TryGetValue(evt.ClanId, out var cookie))
            {
                try
                {
                    await ClanUtils.ValidateCookie(cookie, evt.Region, evt.ClanId, evt.ClanTag);

                    var lineupData = (await ladderBattlesQuery.GetAsync(
                            new LadderBattlesRequest(evt.Region, evt.TeamNumber, cookie))
                        )[0];

                    dto.IsLineupDataAvailable = true;

                    RecentInteractions.LineUpData[battleId] = LineupDto.CreateLineupDto(lineupData, dto.IsVictory);
                }
                catch (InvalidCredentialException ex)
                {
                    await _errorService.LogErrorAsync(ex);
                    await _client!.Rest
                        .SendMessageAsync(ulong.Parse(guild.ChannelId), new MessageProperties
                        {
                            Content = "Error sending detailed clan battle info >_<\n\n" +
                                      $"{ex.Message}"
                        });

                    guild.Cookies.Remove(evt.ClanId);
                }
            }
            
            try
            {
                var response = await _apiClient.PostToServiceAsync("BattleResult", JsonSerializer.Serialize(dto));
                response.EnsureSuccessStatusCode();
                    
                var imageBytes = await response.Content.ReadAsByteArrayAsync();
                using var stream = new MemoryStream(imageBytes);
                
                var button = new ButtonProperties(
                    customId: $"battle result lineup data:{battleId}",
                    label: "View lineup data",
                    style: ButtonStyle.Primary
                );

                var messageProperties = new MessageProperties
                {
                    Attachments = [new AttachmentProperties("BattleResult.png", stream)],
                    Components = [new ActionRowProperties([button])]
                };
                    
                await _channelMessageService.SendAfterTimeoutAsync(ulong.Parse(guild.Id), ulong.Parse(guild.ChannelId), messageProperties, evt.BattleTime);
                
                var timeout = TimeSpan.FromSeconds(30);
                var cts = new CancellationTokenSource();
                ComponentInactivityTimer.Timers[battleId] = cts;
                await ComponentInactivityTimer.StartLineupDataTimerAsync(battleId, timeout, cts);
            }
            catch (Exception ex)
            {
                await _errorService.LogErrorAsync(ex);
            }
        }
    }
    
    public Task StartAsync(CancellationToken cancellationToken) 
    {
        _eventBus.BattleDetected += OnEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) 
    {
        _eventBus.BattleDetected -= OnEventAsync;
        return Task.CompletedTask;
    }
}
