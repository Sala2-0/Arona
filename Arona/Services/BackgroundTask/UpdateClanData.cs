using Arona.Events;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.Api.Official;
using Arona.Models.DB;
using Arona.Utility;
using Microsoft.Extensions.Hosting;

namespace Arona.Services.BackgroundTask;

public class UpdateClanData(ErrorService errorService, IApiService apiService) : BackgroundService, IBackgroundTask
{
    public async Task RunAsync()
    {
        await RunAsync(errorService, apiService, notifyGuilds: true);
    }
    
    public static async Task RunAsync(ErrorService errorService, IApiService apiService, bool notifyGuilds = true)
    {
        Repository.Clans.DeleteMany(c => c.ExternalData.Guilds.Count == 0);

        var currentTime = DateTimeOffset.Now.ToUnixTimeSeconds();
        var clans = Repository.Clans.FindAll().ToList();
        
        // Clear invalid guilds
        clans.ForEach(clan =>
        {
            var relevantGuilds = Repository.Guilds.Find(g => g.Clans.Contains(clan.Id));
            clan.ExternalData.Guilds.RemoveAll(guildId =>
            {
                var guild = relevantGuilds.FirstOrDefault(g => g.Id == guildId);
                return guild == null || !guild.Clans.Contains(clan.Id);
            });
        });
        
        var newClanDatas = new List<ClanView>();
        foreach (var clan in clans)
        {
            try
            {
                var newClanData = await new ClanViewQuery(apiService.HttpClient)
                    .GetAsync(new ClanViewRequest(clan.ExternalData.Region, clan.Clan.Id))
                    .IgnoreRedundantFields();
                newClanDatas.Add(newClanData);
            }
            catch (Exception ex)
            {
                await errorService.PrintErrorAsync(ex, $"Error in {nameof(UpdateClanData)}.{nameof(RunAsync)}");
            }
        }

        int latestSeasonNumber;
        try
        {
            var clanBattleSeasonsQuery = new ClanBattleSeasonsQuery(apiService.HttpClient);
            var clanBattleSeasonData = await clanBattleSeasonsQuery.GetAsync(new EmptyRequest());
            latestSeasonNumber = clanBattleSeasonData.Data
                .Where(i => int.Parse(i.Key) < 100)
                .MaxBy(i => int.Parse(i.Key)).Value.SeasonId;
        }
        catch (Exception e)
        {
            await errorService.PrintErrorAsync(e, $"Error in {nameof(UpdateClanData)}.{nameof(RunAsync)}");
            return;
        }
        
        HandleNewSeason(clans, newClanDatas, latestSeasonNumber);

        var clansWithFinishedSession = clans
            .Where(c => c.HasSessionEnded(currentTime) && c.HasASession())
            .ToList();
        var finishedClans = clansWithFinishedSession.Select(c => c.Id).ToHashSet();
        clans.RemoveAll(c => finishedClans.Contains(c.Id));
        
        clansWithFinishedSession.ForEach(clan =>
        {
            clan.CalculateSessionStatistics(out var winsCount, out var totalPoints);

            if (notifyGuilds)
            {
                EventBus.OnSessionEnded(new ClanBattleSessionEnded
                {
                    ClanId = clan.Id,
                    ClanTag = clan.Clan.Tag,
                    ClanName  = clan.Clan.Name,
                    BattlesCount = clan.ExternalData.RecentBattles.Count,
                    WinsCount = winsCount,
                    TotalPoints = totalPoints,
                    Date = DateOnly.FromDateTime(DateTime.UtcNow)
                });
            }
                    
            clan.ResetSessionData();
            Repository.Clans.Update(clan);
        });

        foreach (var clan in clans)
        {
            try
            {
                var newClanData = newClanDatas.FirstOrDefault(c => c.Clan.Id == clan.Id);
                var clanRank = await new LadderStructureByClanQuery(apiService.HttpClient)
                    .GetRegionAndGlobalRankAsync(clan.Id, clan.ExternalData.Region);

                if (newClanData == null)
                {
                    continue;
                }

                newClanData.FilterRatings(latestSeasonNumber);

                if (HasStartedPlaying(clan.WowsLadder.PrimeTime, newClanData.WowsLadder.PrimeTime))
                {
                    newClanData.Id = clan.Id;
                    newClanData.ExternalData = clan.ExternalData;
                    newClanData.ExternalData.SessionEndTime = ClanUtils.GetEndSession(newClanData.WowsLadder.PrimeTime);

                    if (notifyGuilds)
                    {
                        await EventBus.OnSessionStarted(new ClanBattleSessionStarted(
                            ClanId: clan.Id,
                            ClanTag: clan.Clan.Tag,
                            ClanName: clan.Clan.Name
                        ));
                    }

                    Repository.Clans.Update(newClanData);
                    continue;
                }

                var lastBattleTime = DateTimeOffset.Parse(newClanData.WowsLadder.LastBattleAt)
                    .ToUnixTimeSeconds();

                for (var i = 0; i < clan.WowsLadder.Ratings.Count; i++)
                {
                    var rating = clan.WowsLadder.Ratings[i];
                    var newRatingData = newClanData.WowsLadder.Ratings
                        .FirstOrDefault(r => r.TeamNumber == rating.TeamNumber);

                    if (newRatingData == null || rating.BattlesCount == newRatingData.BattlesCount)
                    {
                        continue;
                    }

                    var isVictory = newRatingData.WinsCount > rating.WinsCount;

                    var eventData = new BattleDetected
                    {
                        ClanId = clan.Id,
                        Region = clan.ExternalData.Region,
                        ClanTag = clan.Clan.Tag,
                        ClanName = clan.Clan.Name,
                        BattleTime = lastBattleTime,
                        TeamNumber = newRatingData.TeamNumber,
                        ClanRank = clanRank,
                        SuccessFactor = SuccessFactor.Calculate(
                            publicRating: newRatingData.PublicRating,
                            battlesCount: newRatingData.BattlesCount,
                            leagueExponent: ClanUtils.GetLeagueExponent(newRatingData.League)
                        ),
                        IsVictory = isVictory,
                        League = newRatingData.League,
                        Division = newRatingData.Division,
                        DivisionRating = newRatingData.DivisionRating,
                        Stage = newRatingData.Stage
                    };

                    if (HasEnteredStage(newRatingData.Stage))
                    {
                        eventData.PointsDelta = newRatingData.PublicRating - rating.PublicRating;
                    }
                    else if (newRatingData.Stage != null)
                    {
                        eventData.StageProgressOutcome = newRatingData.PublicRating > rating.PublicRating
                            ? StageProgressOutcome.Victory
                            : StageProgressOutcome.Defeat;
                    }
                    else if (newRatingData.PublicRating != rating.PublicRating)
                    {
                        eventData.PointsDelta = newRatingData.PublicRating - rating.PublicRating;
                    }
                    else
                    {
                        continue;
                    }

                    if (notifyGuilds)
                    {
                        await EventBus.OnBattleDetected(eventData);
                    }

                    clan.ExternalData.RecentBattles.Add(new RecentBattle
                    {
                        BattleTime = lastBattleTime,
                        IsVictory = isVictory,
                        PointsEarned = newRatingData.PublicRating - rating.PublicRating,
                        TeamNumber = newRatingData.TeamNumber,
                    });
                }

                newClanData.Id = clan.Id;
                newClanData.ExternalData = clan.ExternalData;
                newClanData.ExternalData.RankData = clanRank;

                Repository.Clans.Update(newClanData);
            }
            catch (Exception ex)
            {
                await errorService.PrintErrorAsync(ex, $"Error in {nameof(UpdateClanData)}.{nameof(RunAsync)}");
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(5));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await DatabaseService.WaitForAllWritesAsync();
                await DatabaseService.WaitForUpdateAsync();

                DatabaseService.IsDatabaseUpdating = true;

                await RunAsync();
            }
            catch (Exception ex)
            {
                await errorService.PrintErrorAsync(ex, $"Error in {nameof(UpdateClanData)}.{nameof(ExecuteAsync)}");
            }
            finally
            {
                DatabaseService.IsDatabaseUpdating = false;
            }
        }
    }
    
    // Support methods
    private static bool HasStartedPlaying(int? previousData, int? newData) => previousData == null && newData != null;

    private static bool HasEnteredStage(Stage? stage) => stage is { Progress.Length: 0 };
    
    private static void HandleNewSeason(List<ClanView> clanDatas, List<ClanView> newClanDatas, int latestSeasonNumber)
    {
        var hasNewSeasonStarted = clanDatas.Any(c => c.WowsLadder.SeasonNumber != latestSeasonNumber);
        if (!hasNewSeasonStarted)
        {
            return;
        }
        
        newClanDatas.ForEach(clanData =>
        {
            var oldClanData = clanDatas.FirstOrDefault(c => c.Id == clanData.Id);
            if (oldClanData == null)
            {
                return;
            }
            
            clanData.Id = oldClanData.Id;
            clanData.ExternalData = oldClanData.ExternalData;
            Repository.Clans.Update(clanData);
        });

        Repository.HurricaneLeaderboard.DeleteAll();
        clanDatas.Clear();
    }
}