using System.Runtime.InteropServices;
using System.Security.Authentication;
using NetCord.Rest;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Models;

namespace Arona.Utility;

internal static class UpdateClan
{
    public static async Task UpdateClansAsync()
    {
        await Program.WaitForWriteAsync();
        await Program.WaitForUpdateAsync();

        Program.UpdateProgress = true;

        Collections.Clans.DeleteMany(c => c.ExternalData.Guilds.Count == 0);

        var self = await Program.Client!.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        foreach (var dbClan in Collections.Clans.Find(_ => true).ToList())
        {
            List<Guild> guilds = [];

            foreach (var guildId in dbClan.ExternalData.Guilds.ToList())
            {
                var guild = Collections.Guilds.FindOne(g => g.Id == guildId);

                if (guild == null || !guild.Clans.Exists(clanId => clanId == dbClan.Clan.Id))
                    dbClan.ExternalData.Guilds.Remove(guildId);
                else
                    guilds.Add(guild);
            }

            try
            {
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                // Session ended
                if (currentTime >= dbClan.ExternalData.SessionEndTime && dbClan.ExternalData.RecentBattles.Count > 0)
                {
                    string currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

                    int wins = 0;
                    int totalPoints = 0;

                    foreach (var battle in dbClan.ExternalData.RecentBattles)
                    {
                        if (battle.IsVictory) wins++;

                        totalPoints += battle.PointsEarned;
                    }

                    var sessionEmbed = new SessionEmbed
                    {
                        IconUrl = botIconUrl,
                        ClanFullName = $"[{dbClan.Clan.Tag}] {dbClan.Clan.Name}",
                        BattlesCount = dbClan.ExternalData.RecentBattles.Count,
                        Date = currentDate,
                        Points = totalPoints,
                        WinsCount = wins
                    }.CreateEmbed();

                    foreach (var guild in guilds)
                        await SendMessage(ulong.Parse(guild.ChannelId), sessionEmbed);

                    dbClan.ExternalData.RecentBattles.Clear();
                    dbClan.ExternalData.SessionEndTime = null;

                    Collections.Clans.Update(dbClan);
                    continue;
                }

                var apiClan = await ClanView.GetAsync(dbClan.Clan.Id, dbClan.ExternalData.Region);
                
                // Hämta rankningar
                Task<LadderStructure[]> globalRankTask = LadderStructure.GetAsync(apiClan.Clan.Id, dbClan.ExternalData.Region);
                Task<LadderStructure[]> regionRankTask = LadderStructure.GetAsync(apiClan.Clan.Id, dbClan.ExternalData.Region, ClanUtils.ConvertRegion(dbClan.ExternalData.Region));

                await Task.WhenAll(globalRankTask, regionRankTask);

                var apiClanData = new
                {
                    Id = apiClan.Clan.Id,
                    Tag = apiClan.Clan.Tag,
                    Name = apiClan.Clan.Name,
                    LatestSeason = apiClan.WowsLadder.SeasonNumber,
                    PrimeTime = apiClan.WowsLadder.PrimeTime,
                    PlannedPrimeTime = apiClan.WowsLadder.PlannedPrimeTime,
                    GlobalRank = globalRankTask.Result.First(c => c.Id == apiClan.Clan.Id).Rank,
                    RegionRank = regionRankTask.Result.First(c => c.Id == apiClan.Clan.Id).Rank
                };

                // Vid ett nytt säsong
                if (dbClan.WowsLadder.SeasonNumber != apiClanData.LatestSeason)
                {
                    dbClan.WowsLadder = apiClan.WowsLadder;
                    dbClan.ExternalData.RecentBattles.Clear();
                    dbClan.ExternalData.SessionEndTime = null;
                    dbClan.WowsLadder.Ratings.RemoveAll(r => r.SeasonNumber != dbClan.WowsLadder.SeasonNumber);
                    dbClan.ExternalData.GlobalRank = apiClanData.GlobalRank;
                    dbClan.ExternalData.RegionRank = apiClanData.RegionRank;

                    Collections.Clans.Update(dbClan);
                    continue;
                }

                if (dbClan.WowsLadder.PrimeTime == null && apiClanData.PrimeTime != null)
                {
                    dbClan.ExternalData.SessionEndTime = ClanUtils.GetEndSession(apiClanData.PrimeTime);

                    foreach (var guild in guilds)
                        await Program.Client.Rest.SendMessageAsync(
                            channelId: ulong.Parse(guild.ChannelId),
                            message: $"`[{apiClanData.Tag}] {apiClanData.Name} has started playing`"
                        );
                }

                long lastBattleUnix = DateTimeOffset.Parse(apiClan.WowsLadder.LastBattleAt).ToUnixTimeSeconds();

                // Slag resultat
                for (int i = 0; i < dbClan.WowsLadder.Ratings.Count; i++)
                {
                    var dbRating = dbClan.WowsLadder.Ratings[i];

                    var apiRating = apiClan.WowsLadder.Ratings
                        .FirstOrDefault(r => r.TeamNumber == dbRating.TeamNumber && r.SeasonNumber == apiClanData.LatestSeason) ?? null;

                    if (apiRating == null || apiRating.BattlesCount == dbRating.BattlesCount) continue;

                    bool isVictory = apiRating.WinsCount > dbRating.WinsCount;

                    var embedSkeleton = new BattleEmbed
                    {
                        IconUrl = botIconUrl,
                        BattleTime = lastBattleUnix,
                        ClanFullName = $"[{apiClanData.Tag}] {apiClanData.Name}",
                        TeamNumber = apiRating.TeamNumber,

                        League = apiRating.League,
                        Division = apiRating.Division,
                        DivisionRating = apiRating.DivisionRating,

                        GlobalRank = apiClanData.GlobalRank,
                        RegionRank = apiClanData.RegionRank,
                        SuccessFactor = SuccessFactor.Calculate(
                            rating: apiRating.PublicRating,
                            battlesCount: apiRating.BattlesCount,
                            leagueExponent: ClanUtils.GetLeagueExponent(apiRating.League)
                        ),

                        IsVictory = isVictory,
                        Stage = apiRating.Stage
                    };
                    
                    // Entering stage
                    if (apiRating.Stage != null && apiRating.Stage.Progress.Length == 0)
                        embedSkeleton.PointsDelta = apiRating.PublicRating - dbRating.PublicRating;
                    
                    // Stage progression
                    else if (apiRating.Stage != null)
                        embedSkeleton.StageProgressOutcome = apiRating.Stage.Progress.Last() == "victory" ? 1 : 0;

                    // Leaving stage
                    else if (dbRating.Stage != null)
                        embedSkeleton.StageProgressOutcome = apiRating.PublicRating > dbRating.PublicRating ? -1 : -2;

                    // Regular battle
                    else if (apiRating.PublicRating != dbRating.PublicRating)
                        embedSkeleton.PointsDelta = apiRating.PublicRating - dbRating.PublicRating;
                    else
                        continue;

                    foreach (var guild in guilds)
                    {
                        if (guild.Cookies.TryGetValue(apiClan.Clan.Id, out string? cookie))
                            try
                            {
                                var detailedData = (await LadderBattle.GetAsync(cookie, dbClan.ExternalData.Region, apiRating.TeamNumber))[0];
                                var embed = new DetailedBattleEmbed
                                {
                                    IconUrl = botIconUrl,
                                    Data = detailedData,
                                    BattleTime = lastBattleUnix,
                                    IsVictory = isVictory
                                }.CreateEmbed();

                                await SendMessage(ulong.Parse(guild.ChannelId), embed);
                            }
                            catch (InvalidCredentialException ex)
                            {
                                await Program.Error(ex);
                            }
                        else
                            await SendMessage(ulong.Parse(guild.ChannelId), embedSkeleton.CreateEmbed());
                    }

                    dbClan.ExternalData.RecentBattles.Add(new RecentBattle
                    {
                        BattleTime = lastBattleUnix,
                        IsVictory = isVictory,
                        PointsEarned = apiRating.PublicRating - dbRating.PublicRating,
                        TeamNumber = apiRating.TeamNumber,
                    });

                    dbClan.WowsLadder.Ratings[i] = apiRating;
                }

                dbClan.ExternalData.GlobalRank = apiClanData.GlobalRank;
                dbClan.ExternalData.RegionRank = apiClanData.RegionRank;
                dbClan.WowsLadder.PrimeTime = apiClanData.PrimeTime;
                dbClan.WowsLadder.PlannedPrimeTime = apiClanData.PlannedPrimeTime;

                // throw new Exception("Stop update for debug");

                Collections.Clans.Update(dbClan);
            }
            catch (Exception ex)
            {
                await Program.Error(ex);
            }
        }

        Program.UpdateProgress = false;
    }

    private static async Task SendMessage(ulong channelId, EmbedProperties embed)
    {
        try
        {
            await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties{ Embeds = [embed] });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }
}
