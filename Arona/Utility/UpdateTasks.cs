using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using NetCord.Rest;
using System.Security.Authentication;

using Timer = System.Timers.Timer;

namespace Arona.Utility;

/// <summary>
/// Contains methods run by timers
/// </summary>
internal static class UpdateTasks
{
    public static async Task UpdateHurricaneLeaderboardAsync(bool startupUpdate = false)
    {
        try
        {
            var guilds = Collections.Guilds.FindAll().ToList();
            var newLeaderboard = new List<LadderStructure>();
            string[] realms = ["eu", "us", "sg"];

            foreach (var realm in realms)
                newLeaderboard.AddRange(await LadderStructure.GetAsync(league: 0, division: 1, realm));

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
                leaderboard = newLeaderboard;

                foreach (var clan in leaderboard)
                    foreach (var guild in guilds)
                        await Message.SendAsync(
                            ulong.Parse(guild.Id),
                            ulong.Parse(guild.ChannelId),
                            new EmbedProperties
                            {
                                Title = "New hurricane clan",
                                Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                            }
                        );
            }

            var oldIds = leaderboard.Select(c => c.Id).ToHashSet();
            var newIds = newLeaderboard.Select(c => c.Id).ToHashSet();

            var removedClans = leaderboard.Where(c => !newIds.Contains(c.Id)).ToList();
            var addedClans = newLeaderboard.Where(c => !oldIds.Contains(c.Id)).ToList();

            foreach (var guild in guilds)
            {
                foreach (var clan in removedClans)
                    await Message.SendAsync(
                        ulong.Parse(guild.Id),
                        ulong.Parse(guild.ChannelId),
                        new EmbedProperties
                        {
                            Title = "Clan dropped from Hurricane",
                            Description = $"`[{clan.Tag}] {clan.Name}` has dropped Hurricane leaderboard!"
                        }
                    );

                foreach (var clan in addedClans)
                    await Message.SendAsync(
                        ulong.Parse(guild.Id),
                        ulong.Parse(guild.ChannelId),
                        new EmbedProperties
                        {
                            Title = "New hurricane clan",
                            Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                        }
                    );
            }

            Collections.HurricaneLeaderboard.DeleteAll();
            Collections.HurricaneLeaderboard.InsertBulk(newLeaderboard);
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
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
                //var apiClan = await ClanView.GetMockupAsync();

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
                    GlobalRank = globalRankTask.Result.FirstOrDefault(c => c.Id == apiClan.Clan.Id)?.Rank,
                    RegionRank = regionRankTask.Result.FirstOrDefault(c => c.Id == apiClan.Clan.Id)?.Rank
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
                    dbClan.WowsLadder.Ratings = apiClan.WowsLadder.Ratings.FindAll(r => r.SeasonNumber == apiClanData.LatestSeason);

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

                    foreach (var guild in guilds)
                    {
                        if (guild.Cookies.TryGetValue(apiClan.Clan.Id, out string? cookie))
                            try
                            {
                                var cookieValidationData = await AccountInfoSync.GetAsync(cookie, dbClan.ExternalData.Region);
                                if (cookieValidationData.ClanId != apiClan.Clan.Id)
                                    throw new InvalidCredentialException($"Cookie for clan `{apiClan.Clan.Tag}` is invalid: Player is not a member of the clan.");
                                if (cookieValidationData.Rank < Role.LineOfficer)
                                    throw new InvalidCredentialException($"Cookie for clan `{apiClan.Clan.Tag}` is invalid: Player is too high ranking.");

                                var detailedData = (await LadderBattle.GetAsync(cookie, dbClan.ExternalData.Region, apiRating.TeamNumber))[0];
                                var embed = new DetailedBattleEmbed
                                {
                                    IconUrl = botIconUrl,
                                    Data = detailedData,
                                    BattleTime = lastBattleUnix,
                                    IsVictory = isVictory
                                }.CreateEmbed();

                                await SendMessage(ulong.Parse(guild.ChannelId), embed, lastBattleUnix);
                            }
                            catch (InvalidCredentialException ex)
                            {
                                await Program.Error(ex);
                                await Program.Client.Rest
                                    .SendMessageAsync(ulong.Parse(guild.ChannelId), new MessageProperties
                                    {
                                        Content = "Error sending detailed clan battle info >_<\n\n" +
                                                  $"{ex.Message}"
                                    });

                                guild.Cookies.Remove(apiClan.Clan.Id);

                                // Send regular embed instead since detailed data failed
                                await SendMessage(ulong.Parse(guild.ChannelId), embedSkeleton.CreateEmbed(), lastBattleUnix);
                            }
                        else
                            await SendMessage(ulong.Parse(guild.ChannelId), embedSkeleton.CreateEmbed(), lastBattleUnix);
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
            await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties { Embeds = [embed] });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    private static async Task SendMessage(ulong channelId, EmbedProperties embed, long battleTimeSeconds)
    {
        battleTimeSeconds += 300;
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var timeDifference = battleTimeSeconds - currentTime;
        if (timeDifference <= 0)
        {
            try
            {
                await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties { Embeds = [embed] });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }

            return;
        }

        var timer = new Timer(timeDifference * 1000d);
        timer.AutoReset = false;
        timer.Elapsed += async (_, _) =>
        {
            timer.Dispose();
            try
            {
                await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties { Embeds = [embed] });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        };
        timer.Enabled = true;
        timer.Start();
    }
}