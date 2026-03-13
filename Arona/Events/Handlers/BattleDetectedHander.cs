using Arona.Events;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Services;
using Arona.Utility;
using NetCord.Rest;
using System.Security.Authentication;
using Arona.Services.Message;
using Microsoft.Extensions.Hosting;
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
            var embed = new BattleEmbed
            {
                IconUrl = botIconUrl,
                BattleTime = evt.BattleTime,
                ClanFullName = $"[{evt.ClanTag}] {evt.ClanName}",
                TeamNumber = evt.TeamNumber,

                League = evt.League,
                Division = evt.Division,
                DivisionRating = evt.DivisionRating,
                Stage = evt.Stage,

                GlobalRank = (int)evt.ClanRank.Global!,
                RegionRank = (int)evt.ClanRank.Region!,
                SuccessFactor = evt.SuccessFactor,

                IsVictory = evt.IsVictory,
                PointsDelta = evt.PointsDelta,
                StageProgressOutcome = evt.StageProgressOutcome
            };

            if (guild.Cookies.TryGetValue(evt.ClanId, out var cookie))
            {
                try
                {
                    await ClanUtils.ValidateCookie(cookie, evt.Region, evt.ClanId, evt.ClanTag);

                    var detailedData = (await ladderBattlesQuery.GetAsync(
                            new LadderBattlesRequest(evt.Region, evt.TeamNumber, cookie))
                        )[0];

                    var detailedEmbed = new DetailedBattleEmbed
                    {
                        IconUrl = botIconUrl,
                        Data = detailedData,
                        BattleTime = evt.BattleTime,
                        IsVictory = evt.IsVictory
                    }.CreateEmbed();

                    await channelMessageService.SendMessageAfterTimeoutAsync(
                        ulong.Parse(guild.Id),
                        ulong.Parse(guild.ChannelId), 
                        new MessageProperties().AddEmbeds(detailedEmbed), 
                        evt.BattleTime);
                }
                catch (InvalidCredentialException ex)
                {
                    await errorService.PrintErrorAsync(ex, $"Error at {nameof(BattleDetectedHandler)}.{nameof(OnEventAsync)}");
                    await errorService.NotifyUserOfErrorAsync(ulong.Parse(guild.Id), ex, "Error sending detailed clan battle info");

                    guild.Cookies.Remove(evt.ClanId);

                    // Send regular embed instead since detailed data failed
                    await channelMessageService.SendMessageAfterTimeoutAsync(
                        ulong.Parse(guild.Id), 
                        ulong.Parse(guild.ChannelId),
                        new MessageProperties().AddEmbeds(embed.CreateEmbed()),
                        evt.BattleTime);
                }
            }
            else
            {
                await channelMessageService.SendMessageAfterTimeoutAsync(
                    ulong.Parse(guild.Id), 
                    ulong.Parse(guild.ChannelId),
                    new MessageProperties().AddEmbeds(embed.CreateEmbed()),
                    evt.BattleTime);
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
