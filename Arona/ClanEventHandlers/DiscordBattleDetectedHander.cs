using Arona.ClanEvents;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Services;
using Arona.Services.UpdateTasks;
using Arona.Utility;
using NetCord.Rest;
using System.Security.Authentication;
using Arona.Services.UpdateTasks.Submethods;

namespace Arona.ClanEventHandlers;

public class DiscordBattleDetectedHandler
{
    public static void Register() => ClanEventBus.BattleDetected += OnBattleDetectedAsync;

    private static async Task OnBattleDetectedAsync(BattleDetected evt)
    {
        var guilds = DatabaseUtilities.GetGuildsForClan(evt.ClanId);
        var botIconUrl = await BotUtilities.GetBotIconUrl();

        var ladderBattlesQuery = new LadderBattlesQuery(ApiClient.Instance);

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

                GlobalRank = evt.GlobalRank,
                RegionRank = evt.RegionRank,
                SuccessFactor = evt.SuccessFactor,

                IsVictory = evt.IsVictory,
                PointsDelta = evt.PointsDelta,
                StageProgressOutcome = evt.StageProgressOutcome
            };

            if (guild.Cookies.TryGetValue(evt.ClanId, out var cookie))
            {
                try
                {
                    await UpdateClansSubmethods.ValidateCookie(cookie, evt.Region, evt.ClanId, evt.ClanTag);

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

                    await UpdateTasks.SendMessageAsync(ulong.Parse(guild.Id),ulong.Parse(guild.ChannelId), detailedEmbed, evt.BattleTime);
                }
                catch (InvalidCredentialException ex)
                {
                    await Program.LogError(ex);
                    await Program.Client!.Rest
                        .SendMessageAsync(ulong.Parse(guild.ChannelId), new MessageProperties
                        {
                            Content = "Error sending detailed clan battle info >_<\n\n" +
                                      $"{ex.Message}"
                        });

                    guild.Cookies.Remove(evt.ClanId);

                    // Send regular embed instead since detailed data failed
                    await UpdateTasks.SendMessageAsync(ulong.Parse(guild.Id), ulong.Parse(guild.ChannelId), embed.CreateEmbed(), evt.BattleTime);
                }
            }
            else
            {
                await UpdateTasks.SendMessageAsync(ulong.Parse(guild.Id), ulong.Parse(guild.ChannelId), embed.CreateEmbed(), evt.BattleTime);
            }
        }
    }
}
