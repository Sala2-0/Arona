using Arona.ClanEvents;
using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Services.Message;
using Arona.Utility;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Rest;

namespace Arona.Services;

public class ClanUpdateService : BackgroundService
{
    private readonly IDatabaseRepository _repository;
    private readonly IApiClient _apiClient;
    private readonly IClanEventBus _eventBus;
    private readonly IChannelMessageService _channelMessageService;
    private readonly ILogger<ClanUpdateService> _logger;
    private readonly IErrorService _errorService;
    
    public ClanUpdateService(
        IDatabaseRepository repository,
        IApiClient apiClient,
        IClanEventBus eventBus,
        IChannelMessageService channelMessageService,
        ILogger<ClanUpdateService> logger,
        IErrorService errorService)
    {
        _repository = repository;
        _apiClient = apiClient;
        _eventBus = eventBus;
        _channelMessageService = channelMessageService;
        _logger = logger;
        _errorService = errorService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await UpdateHurricaneLeaderboardAsync(startupUpdate: true);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            
            try
            {
                await UpdateHurricaneLeaderboardAsync();
                await UpdateClansAsync();
            }
            catch (OperationCanceledException e)
            {
                _logger.LogError(e, "Error");
            }
        }
    }
    
    private async Task UpdateHurricaneLeaderboardAsync(bool startupUpdate = false)
    {
        try
        {
            var guilds = _repository.Guilds.FindAll().ToList();
            var newLeaderboard = new List<LadderStructure>();
            string[] realms = ["eu", "us", "sg"];

            var apiQuery = new LadderStructureByRealmQuery(_apiClient.HttpClient);
            foreach (var realm in realms)
                newLeaderboard.AddRange(await apiQuery.GetAsync(new LadderStructureByRealmRequest(realm, League: 0, Division: 1)));

            if (startupUpdate)
            {
                _repository.HurricaneLeaderboard.DeleteAll();
                _repository.HurricaneLeaderboard.InsertBulk(newLeaderboard);
                return;
            }

            if (newLeaderboard.Count == 0) return;

            var leaderboard = _repository.HurricaneLeaderboard.FindAll().ToList();
            if (leaderboard.Count == 0)
            {
                await NotifyNewHurricaneClansAsync(guilds, newLeaderboard);

                _repository.HurricaneLeaderboard.InsertBulk(newLeaderboard);
                return;
            }

            var oldIds = leaderboard.Select(c => c.Id).ToHashSet();
            var newIds = newLeaderboard.Select(c => c.Id).ToHashSet();

            var removedClans = leaderboard.Where(c => !newIds.Contains(c.Id)).ToList();
            var addedClans = newLeaderboard.Where(c => !oldIds.Contains(c.Id)).ToList();

            await NotifyHurricaneChangesAsync(guilds, addedClans, removedClans);

            _repository.HurricaneLeaderboard.DeleteAll();
            _repository.HurricaneLeaderboard.InsertBulk(newLeaderboard);
        }
        catch (Exception ex)
        {
            await _errorService.LogErrorAsync(ex);
        }
    }
    
    public async Task UpdateClansAsync(bool notifyGuilds = true)
    {
        await DatabaseService.WaitForAllWritesAsync();
        await DatabaseService.WaitForUpdateAsync();

        DatabaseService.IsDatabaseUpdating = true;

        _repository.Clans.DeleteMany(c => c.ExternalData.Guilds.Count == 0);

        foreach (var dbClan in _repository.Clans.Find(_ => true).ToList())
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
                        await _eventBus.OnSessionEnded(new ClanSessionEnded(
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

                var clanViewQuery = new ClanViewQuery(_apiClient.HttpClient);
                var apiClan = await clanViewQuery.GetAsync(new ClanViewRequest(dbClan.ExternalData.Region, dbClan.Clan.Id));

                // Hämta rankningar
                var ladderStructureQuery = new LadderStructureByClanQuery(_apiClient.HttpClient);
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
                        await _eventBus.OnSessionStarted(new ClanSessionStarted(
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
                        await _eventBus.OnBattleDetected(eventData);
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

                _repository.Clans.Update(dbClan);
            }
            catch (Exception ex)
            {
                await _errorService.LogErrorAsync(ex);
            }
        }

        DatabaseService.IsDatabaseUpdating = false;
    }
    
    private async Task NotifyHurricaneChangesAsync(
        List<Guild> guilds,
        List<LadderStructure> added,
        List<LadderStructure> removed
    )
    {
        foreach (var guild in guilds)
        {
            foreach (var clan in removed)
            {
                await _channelMessageService.SendAsync(
                    ulong.Parse(guild.Id),
                    ulong.Parse(guild.ChannelId),
                    new MessageProperties
                    {
                        Embeds =
                        [
                            new EmbedProperties
                            {
                                Title = "Clan dropped from Hurricane",
                                Description = $"`[{clan.Tag}] {clan.Name}` has dropped from Hurricane leaderboard!"
                            }
                        ]
                    }
                );
            }

            foreach (var clan in added)
            {
                await _channelMessageService.SendAsync(
                    ulong.Parse(guild.Id),
                    ulong.Parse(guild.ChannelId),
                    new MessageProperties
                    {
                        Embeds =
                        [
                            new EmbedProperties
                            {
                                Title = "New hurricane clan",
                                Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                            }
                        ]
                    }
                );
            }
        }
    }

    private async Task NotifyNewHurricaneClansAsync(
        List<Guild> guilds,
        List<LadderStructure> added
    )
    {
        foreach (var guild in guilds)
        {
            foreach (var clan in added)
            {
                await _channelMessageService.SendAsync(
                    ulong.Parse(guild.Id),
                    ulong.Parse(guild.ChannelId),
                    new MessageProperties
                    {
                        Embeds =
                        [
                            new EmbedProperties
                            {
                                Title = "New hurricane clan",
                                Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                            }
                        ]
                    }
                );
            }
        }
    }
    
    private void ClearInvalidGuilds(ClanView clan)
    {
        foreach (var guildId in clan.ExternalData.Guilds.ToList())
        {
            var guild = _repository.Guilds.FindOne(g => g.Id == guildId);

            if (guild == null || !guild.Clans.Exists(clanId => clanId == clan.Clan.Id))
                clan.ExternalData.Guilds.Remove(guildId);
        }
    }

    private bool HasSessionEnded(long currentTime, ClanView clan) =>
        currentTime >= clan.ExternalData.SessionEndTime && clan.ExternalData.RecentBattles.Count > 0;

    public static void CalculateSessionStats(List<RecentBattle> battles, out int wins, out int totalPoints)
    {
        wins = 0;
        totalPoints = 0;

        foreach (var battle in battles)
        {
            if (battle.IsVictory) wins++;
            totalPoints += battle.PointsEarned;
        }
    }

    private void ResetSessionData(ClanView clan)
    {
        clan.ExternalData.RecentBattles.Clear();
        clan.ExternalData.SessionEndTime = null;

        _repository.Clans.Update(clan);
    }

    private bool IsNewSeason(int a, int b) => a != b;

    private void SetNewSeasonData(ClanView dbClan, ClanView apiClan, ClanViewMinimal apiClanMinimal)
    {
        dbClan.WowsLadder = apiClan.WowsLadder;
        dbClan.ExternalData.RecentBattles.Clear();
        dbClan.ExternalData.SessionEndTime = null;
        dbClan.WowsLadder.Ratings.RemoveAll(r => r.SeasonNumber != dbClan.WowsLadder.SeasonNumber);
        dbClan.ExternalData.GlobalRank = apiClanMinimal.GlobalRank;
        dbClan.ExternalData.RegionRank = apiClanMinimal.RegionRank;

        _repository.Clans.Update(dbClan);
    }

    private bool HasStartedPlaying(int? a, int? b) => a == null && b != null;

    private bool HasEnteredStage(Stage? stage) => stage is { Progress.Length: 0 };

    private void UpdateClanBattleData(ClanView dbClan, ClanView apiClan, ClanViewMinimal minimalData)
    {
        dbClan.ExternalData.GlobalRank = minimalData.GlobalRank;
        dbClan.ExternalData.RegionRank = minimalData.RegionRank;

        dbClan.WowsLadder.PrimeTime = minimalData.PrimeTime;
        dbClan.WowsLadder.PlannedPrimeTime = minimalData.PlannedPrimeTime;
        dbClan.WowsLadder.League = apiClan.WowsLadder.League;
        dbClan.WowsLadder.Division = apiClan.WowsLadder.Division;
        dbClan.WowsLadder.DivisionRating = apiClan.WowsLadder.DivisionRating;
        dbClan.WowsLadder.LastBattleAt = apiClan.WowsLadder.LastBattleAt;
        dbClan.WowsLadder.LeadingTeamNumber = apiClan.WowsLadder.LeadingTeamNumber;
    }
}