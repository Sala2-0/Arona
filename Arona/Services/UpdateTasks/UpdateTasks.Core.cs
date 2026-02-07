using Arona.ClanEvents;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Utility;

using static Arona.Services.UpdateTasks.Submethods.UpdateClansSubmethods;

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
public static partial class UpdateTasks
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

    public static async Task UpdateClansAsync(bool notifyGuilds = true)
    {
        await DatabaseService.WaitForAllWritesAsync();
        await DatabaseService.WaitForUpdateAsync();

        DatabaseService.IsDatabaseUpdating = true;

        Collections.Clans.DeleteMany(c => c.ExternalData.Guilds.Count == 0);

        foreach (var dbClan in Collections.Clans.Find(_ => true).ToList())
        {
            ClearInvalidGuilds(dbClan);

            try
            {
                var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                if (HasSessionEnded(currentTime, dbClan))
                {
                    CalculateSessionStats(
                        dbClan.ExternalData.RecentBattles,
                        out var wins,
                        out var totalPoints
                    );

                    if (notifyGuilds)
                    {
                        await ClanEventBus.OnSessionEnded(new ClanSessionEnded(
                            ClanId: dbClan.Clan.Id,
                            ClanTag: dbClan.Clan.Tag,
                            ClanName: dbClan.Clan.Name,
                            BattlesCount: dbClan.ExternalData.RecentBattles.Count,
                            WinsCount: wins,
                            TotalPoints: totalPoints,
                            Date: DateOnly.FromDateTime(DateTime.UtcNow)
                        ));
                    }

                    ResetSessionData(dbClan);
                    continue;
                }

                var clanViewQuery = new ClanViewQuery(ApiClient.Instance);
                var apiClan = await clanViewQuery.GetAsync(new ClanViewRequest(dbClan.ExternalData.Region, dbClan.Clan.Id));

                // Hämta rankningar
                var ladderStructureQuery = new LadderStructureByClanQuery(ApiClient.Instance);
                Task<LadderStructure[]> 
                    globalRankTask = ladderStructureQuery.GetAsync(
                        new LadderStructureByClanRequest(apiClan.ClanView.Clan.Id, dbClan.ExternalData.Region)),
                    regionRankTask = ladderStructureQuery.GetAsync(
                        new LadderStructureByClanRequest(apiClan.ClanView.Clan.Id, dbClan.ExternalData.Region, ClanUtils.ToRealm(dbClan.ExternalData.Region)));

                await Task.WhenAll(globalRankTask, regionRankTask);

                var apiClanMinimal = new ClanViewMinimal(apiClan.ClanView, globalRankTask.Result, regionRankTask.Result);

                if (IsNewSeason(dbClan.WowsLadder.SeasonNumber, apiClanMinimal.LatestSeason))
                {
                    SetNewSeasonData(dbClan, apiClan.ClanView, apiClanMinimal);
                    continue;
                }

                if (HasStartedPlaying(dbClan.WowsLadder.PrimeTime, apiClanMinimal.PrimeTime))
                {
                    dbClan.ExternalData.SessionEndTime = ClanUtils.GetEndSession(apiClanMinimal.PrimeTime);
                    dbClan.WowsLadder.Ratings = apiClan.ClanView.WowsLadder.Ratings.FindAll(r => r.SeasonNumber == apiClanMinimal.LatestSeason);

                    if (notifyGuilds)
                    {
                        await ClanEventBus.OnSessionStarted(new ClanSessionStarted(
                            ClanId: apiClan.ClanView.Clan.Id,
                            ClanName: apiClanMinimal.Name,
                            ClanTag: apiClanMinimal.Tag
                        ));
                    }
                }

                var lastBattleUnix = DateTimeOffset.Parse(apiClan.ClanView.WowsLadder.LastBattleAt).ToUnixTimeSeconds();

                // Slag resultat
                for (var i = 0; i < dbClan.WowsLadder.Ratings.Count; i++)
                {
                    var dbRating = dbClan.WowsLadder.Ratings[i];

                    var apiRating = apiClan.ClanView.WowsLadder.Ratings
                        .FirstOrDefault(r => r.TeamNumber == dbRating.TeamNumber && r.SeasonNumber == apiClanMinimal.LatestSeason) ?? null;

                    if (apiRating == null || apiRating.BattlesCount == dbRating.BattlesCount) continue;

                    var isVictory = apiRating.WinsCount > dbRating.WinsCount;

                    var eventData = new BattleDetected
                    {
                        ClanId = dbClan.Id,
                        Region = dbClan.ExternalData.Region,
                        ClanTag = dbClan.Clan.Tag,
                        ClanName = dbClan.Clan.Name,
                        BattleTime = lastBattleUnix,
                        TeamNumber = apiRating.TeamNumber,
                        GlobalRank = (int)apiClanMinimal.GlobalRank!,
                        RegionRank = (int)apiClanMinimal.RegionRank!,
                        SuccessFactor = SuccessFactor.Calculate(
                            publicRating: apiRating.PublicRating,
                            battlesCount: apiRating.BattlesCount,
                            leagueExponent: ClanUtils.GetLeagueExponent(apiRating.League)
                        ),
                        IsVictory = isVictory,
                        League = apiRating.League,
                        Division = apiRating.Division,
                        DivisionRating = apiRating.DivisionRating,
                        Stage = apiRating.Stage
                    };

                    // Entering stage
                    if (HasEnteredStage(apiRating.Stage))
                    {
                        eventData.PointsDelta = apiRating.PublicRating - dbRating.PublicRating;
                    }
                    // Stage progression
                    else if (apiRating.Stage != null)
                    {
                        eventData.StageProgressOutcome = apiRating.Stage.Progress.Last() == "victory"
                            ? StageProgressOutcome.Victory
                            : StageProgressOutcome.Defeat;
                    }
                    // Leaving stage
                    else if (dbRating.Stage != null)
                    {
                        eventData.StageProgressOutcome = apiRating.PublicRating > dbRating.PublicRating
                            ? StageProgressOutcome.PromotedOrStayed
                            : StageProgressOutcome.DemotedOrFailed;
                    }
                    // Regular battle
                    else if (apiRating.PublicRating != dbRating.PublicRating)
                    {
                        eventData.PointsDelta = apiRating.PublicRating - dbRating.PublicRating;
                    }
                    else
                    {
                        continue;
                    }

                    if (notifyGuilds)
                    {
                        await ClanEventBus.OnBattleDetected(eventData);
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

                UpdateClanBattleData(dbClan, apiClan.ClanView, apiClanMinimal);

                Collections.Clans.Update(dbClan);
            }
            catch (Exception ex)
            {
                await Program.LogError(ex);
            }
        }

        DatabaseService.IsDatabaseUpdating = false;
    }
}