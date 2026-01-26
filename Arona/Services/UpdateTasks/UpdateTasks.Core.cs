using NetCord.Rest;
using System.Security.Authentication;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Services.Message;
using Arona.Utility;

namespace Arona.Services.UpdateTasks;

/// <summary>
/// Contains methods run by timers
/// </summary>
/// <remarks>
/// <para>
/// <term>UpdateTasks.Messaging</term> Discord send message methods used by <see cref="UpdateClansAsync"/>
/// </para>
/// 
/// <para>
/// <term>UpdateTasks.HurricaneNotification</term> Discord send message methods used by <see cref="UpdateHurricaneLeaderboardAsync"/>
/// </para>
/// </remarks>
internal static partial class UpdateTasks
{
    public static async Task UpdateHurricaneLeaderboardAsync(bool startupUpdate = false)
    {
        try
        {
            var guilds = Collections.Guilds.FindAll().ToList();
            var newLeaderboard = new List<LadderStructure>();
            string[] realms = ["eu", "us", "sg"];

            var apiQuery = new LadderStructureByRealmQuery(ApiClient.Instance);
            foreach (var realm in realms)
                newLeaderboard.AddRange(await apiQuery.GetAsync(new LadderStructureByRealmRequest(realm, League: 0, Division: 1)));

            if (startupUpdate)
            {
                Collections.HurricaneLeaderboard.DeleteAll();
                Collections.HurricaneLeaderboard.InsertBulk(newLeaderboard);
                return;
            }

            if (newLeaderboard.Count == 0) return;

            var leaderboard = Collections.HurricaneLeaderboard.FindAll().ToList();
            if (leaderboard.Count == 0)
            {
                await NotifyNewHurricaneClansAsync(guilds, newLeaderboard);

                Collections.HurricaneLeaderboard.InsertBulk(newLeaderboard);
                return;
            }

            var oldIds = leaderboard.Select(c => c.Id).ToHashSet();
            var newIds = newLeaderboard.Select(c => c.Id).ToHashSet();

            var removedClans = leaderboard.Where(c => !newIds.Contains(c.Id)).ToList();
            var addedClans = newLeaderboard.Where(c => !oldIds.Contains(c.Id)).ToList();

            await NotifyHurricaneChangesAsync(guilds, addedClans, removedClans);

            Collections.HurricaneLeaderboard.DeleteAll();
            Collections.HurricaneLeaderboard.InsertBulk(newLeaderboard);
        }
        catch (Exception ex)
        {
            await Program.LogError(ex);
        }
    }

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

                    int wins = 0, totalPoints = 0;

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
                        await SendMessageAsync(ulong.Parse(guild.ChannelId), sessionEmbed);

                    dbClan.ExternalData.RecentBattles.Clear();
                    dbClan.ExternalData.SessionEndTime = null;

                    Collections.Clans.Update(dbClan);
                    continue;
                }

                var clanViewQuery = new ClanViewQuery(ApiClient.Instance);
                var apiClan = await clanViewQuery.GetAsync(new ClanViewRequest(dbClan.ExternalData.Region, dbClan.Clan.Id));
                // var apiClan = await clanViewQuery.GetMockupAsync();

                // Hämta rankningar
                var ladderStructureQuery = new LadderStructureByClanQuery(ApiClient.Instance);
                Task<LadderStructure[]> globalRankTask = ladderStructureQuery.GetAsync(
                    new LadderStructureByClanRequest(apiClan.ClanView.Clan.Id, dbClan.ExternalData.Region)
                );
                Task<LadderStructure[]> regionRankTask = ladderStructureQuery.GetAsync(
                    new LadderStructureByClanRequest(apiClan.ClanView.Clan.Id, dbClan.ExternalData.Region, ClanUtils.ToRealm(dbClan.ExternalData.Region))
                );

                await Task.WhenAll(globalRankTask, regionRankTask);

                var apiClanData = new
                {
                    Id = apiClan.ClanView.Clan.Id,
                    Tag = apiClan.ClanView.Clan.Tag,
                    Name = apiClan.ClanView.Clan.Name,
                    LatestSeason = apiClan.ClanView.WowsLadder.SeasonNumber,
                    PrimeTime = apiClan.ClanView.WowsLadder.PrimeTime,
                    PlannedPrimeTime = apiClan.ClanView.WowsLadder.PlannedPrimeTime,
                    GlobalRank = globalRankTask.Result.FirstOrDefault(c => c.Id == apiClan.ClanView.Clan.Id)?.Rank,
                    RegionRank = regionRankTask.Result.FirstOrDefault(c => c.Id == apiClan.ClanView.Clan.Id)?.Rank
                };

                // Vid ett nytt säsong
                if (dbClan.WowsLadder.SeasonNumber != apiClanData.LatestSeason)
                {
                    dbClan.WowsLadder = apiClan.ClanView.WowsLadder;
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
                    dbClan.WowsLadder.Ratings = apiClan.ClanView.WowsLadder.Ratings.FindAll(r => r.SeasonNumber == apiClanData.LatestSeason);

                    foreach (var guild in guilds)
                        await Program.Client.Rest.SendMessageAsync(
                            channelId: ulong.Parse(guild.ChannelId),
                            message: $"`[{apiClanData.Tag}] {apiClanData.Name} has started playing`"
                        );
                }

                long lastBattleUnix = DateTimeOffset.Parse(apiClan.ClanView.WowsLadder.LastBattleAt).ToUnixTimeSeconds();

                // Slag resultat
                for (int i = 0; i < dbClan.WowsLadder.Ratings.Count; i++)
                {
                    var dbRating = dbClan.WowsLadder.Ratings[i];

                    var apiRating = apiClan.ClanView.WowsLadder.Ratings
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

                        GlobalRank = (int)apiClanData.GlobalRank!,
                        RegionRank = (int)apiClanData.RegionRank!,
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

                    var ladderBattlesQuery = new LadderBattlesQuery(ApiClient.Instance);
                    foreach (var guild in guilds)
                    {
                        if (guild.Cookies.TryGetValue(apiClan.ClanView.Clan.Id, out string? cookie))
                            try
                            {
                                var cookieValidationData = await AccountInfoSync.GetAsync(cookie, dbClan.ExternalData.Region);
                                if (cookieValidationData.ClanId != apiClan.ClanView.Clan.Id)
                                    throw new InvalidCredentialException($"Cookie for clan `{apiClan.ClanView.Clan.Tag}` is invalid: Player is not a member of the clan.");
                                if (cookieValidationData.Rank < Role.LineOfficer)
                                    throw new InvalidCredentialException($"Cookie for clan `{apiClan.ClanView.Clan.Tag}` is invalid: Player is too high ranking.");

                                var detailedData = (await ladderBattlesQuery.GetAsync(
                                    new LadderBattlesRequest(dbClan.ExternalData.Region, apiRating.TeamNumber, cookie))
                                )[0];

                                var embed = new DetailedBattleEmbed
                                {
                                    IconUrl = botIconUrl,
                                    Data = detailedData,
                                    BattleTime = lastBattleUnix,
                                    IsVictory = isVictory
                                }.CreateEmbed();

                                await SendMessageAsync(ulong.Parse(guild.ChannelId), embed, lastBattleUnix);
                            }
                            catch (InvalidCredentialException ex)
                            {
                                await Program.LogError(ex);
                                await Program.Client.Rest
                                    .SendMessageAsync(ulong.Parse(guild.ChannelId), new MessageProperties
                                    {
                                        Content = "Error sending detailed clan battle info >_<\n\n" +
                                                  $"{ex.Message}"
                                    });

                                guild.Cookies.Remove(apiClan.ClanView.Clan.Id);

                                // Send regular embed instead since detailed data failed
                                await SendMessageAsync(ulong.Parse(guild.ChannelId), embedSkeleton.CreateEmbed(), lastBattleUnix);
                            }
                        else
                            await SendMessageAsync(ulong.Parse(guild.ChannelId), embedSkeleton.CreateEmbed(), lastBattleUnix);
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
                await Program.LogError(ex);
            }
        }

        Program.UpdateProgress = false;
    }
}