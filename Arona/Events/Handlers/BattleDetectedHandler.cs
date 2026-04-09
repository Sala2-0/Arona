using Arona.Events;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Services;
using Arona.Utility;
using NetCord.Rest;
using System.Security.Authentication;
using System.Text.Json;
using Arona.Models.DataTransfer;
using Arona.Services.Message;
using Microsoft.Extensions.Hosting;
using NetCord;
using NetCord.Gateway;

namespace Arona.Events.Handlers;

public class BattleDetectedHandler(
    GatewayClient gatewayClient,
    ErrorService errorService,
    ChannelMessageService channelMessageService,
    IApiService apiService) : IEventHandler<BattleDetected>, IHostedService
{
    public async Task OnEventAsync(BattleDetected evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(evt.ClanId);
        var self = await gatewayClient.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        var ladderBattlesQuery = new LadderBattlesQuery(apiService.HttpClient);

        foreach (var guild in guilds)
        {
            if (guild.ChannelId == null)
            {
                continue;
            }
            
            var battleId = $"{evt.ClanId}_{evt.BattleTime}";

            var dto = new BattleResult
            {
                ClanName = evt.ClanName,
                ClanTag = evt.ClanTag,
                Team = evt.TeamNumber.ToString(),
                Division = evt.Division,
                DivisionRating = evt.DivisionRating,
                League = evt.League,
                IsVictory = evt.IsVictory,
                PointsDelta = evt.PointsDelta,
                Stage = evt.Stage != null ? new BattleResult.ResultStage(evt.Stage.Type, evt.Stage.Progress) : null,
            };

            if (guild.Cookies.TryGetValue(evt.ClanId, out var cookie))
            {
                try
                {
                    await ClanUtils.ValidateCookie(cookie, evt.Region, evt.ClanId, evt.ClanTag, apiService);

                    var lineupData = (await ladderBattlesQuery.GetAsync(
                            new LadderBattlesRequest(evt.Region, evt.TeamNumber, cookie))
                        )[0];
                    
                    dto.IsLineupDataAvailable = true;
                    RecentInteractions.LineupData[battleId] = new Lineup(lineupData, evt.IsVictory);
                }
                catch (InvalidCredentialException ex)
                {
                    await errorService.PrintErrorAsync(ex, $"Error at {nameof(BattleDetectedHandler)}.{nameof(OnEventAsync)}");
                    await errorService.NotifyUserOfErrorAsync(ulong.Parse(guild.Id), ex, "Error sending detailed clan battle info");

                    guild.Cookies.Remove(evt.ClanId);
                }
            }

            try
            {
                var response = await apiService.PostToServiceAsync("BattleResult", JsonSerializer.Serialize(dto));
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

                await channelMessageService.SendAfterTimeoutAsync(
                    ulong.Parse(guild.Id), 
                    ulong.Parse(guild.ChannelId),
                    messageProperties,
                    evt.BattleTime);

                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                var deltaTime = evt.SessionEndTime - currentTime;
                
                var timeout = TimeSpan.FromSeconds(deltaTime);
                var cts = new CancellationTokenSource();
                ComponentInactivityTimer.Timers[battleId] = cts;
                await ComponentInactivityTimer.StartLineupDataTimerAsync(battleId, timeout, cts);
            }
            catch (Exception ex)
            {
                await errorService.PrintErrorAsync(ex);
            }
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        EventBus.BattleDetected += OnEventAsync;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        EventBus.BattleDetected -= OnEventAsync;
        return Task.CompletedTask;
    }
}
