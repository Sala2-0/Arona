using Arona.Models.Api.Clans;
using Arona.Models.DB;
using Arona.Services.Message;
using Arona.Utility;
using Microsoft.Extensions.Hosting;
using NetCord.Rest;

namespace Arona.Services.BackgroundTask;

public class UpdateLeaderboardTask : BackgroundService, IBackgroundTask
{
    private readonly IApiService _apiService;
    private readonly ErrorService _errorService;
    private readonly ChannelMessageService _channelMessageService;
    private bool _isRunning;
    
    private static UpdateLeaderboardTask SingletonInstance { get; set; }

    public UpdateLeaderboardTask(ChannelMessageService channelMessageService, ErrorService errorService, IApiService apiService)
    {
        _apiService =  apiService;
        _errorService = errorService;
        _channelMessageService = channelMessageService;

        SingletonInstance = this;
    }
    
    public async Task RunAsync()
    {
        await RunAsync(startupUpdate: false);
    }

    public static async Task RunAsync(bool startupUpdate = false)
    {
        if (SingletonInstance._isRunning)
        {
            return;
        }
        SingletonInstance._isRunning = true;
        
        var guilds = Repository.Guilds.FindAll().ToList();
        var newLeaderboard = new List<LadderStructure>();
        string[] realms = ["eu", "us", "sg"];
        
        var query = new LadderStructureByRealmQuery(SingletonInstance._apiService.HttpClient);
        foreach (var realm in realms)
        {
            try
            {
                newLeaderboard.AddRange(await query.GetAsync(new LadderStructureByRealmRequest(
                    realm,
                    League: 0,
                    Division: 1
                )));
            }
            catch (Exception e)
            {
                await SingletonInstance._errorService.PrintErrorAsync(e);
            }
        }

        if (startupUpdate)
        {
            Repository.HurricaneLeaderboard.DeleteAll();
            Repository.HurricaneLeaderboard.InsertBulk(newLeaderboard);
            return;
        }
        
        var leaderboard = Repository.HurricaneLeaderboard
            .FindAll()
            .ToList();
        if (leaderboard.Count == 0)
        {
            await SingletonInstance.NotifyNewHurricaneClansAsync(guilds, newLeaderboard);
            
            Repository.HurricaneLeaderboard.InsertBulk(newLeaderboard);
            return;
        }
        
        var oldClanIds = leaderboard.Select(x => x.Id).ToHashSet();
        var newClanIds = newLeaderboard.Select(x => x.Id).ToHashSet();
        
        var removedClans = leaderboard.Where(x => !newClanIds.Contains(x.Id)).ToList();
        var addedClans = newLeaderboard.Where(x => !oldClanIds.Contains(x.Id)).ToList();
        
        await SingletonInstance.NotifyHurricaneChangesAsync(guilds, addedClans, removedClans);
        
        Repository.HurricaneLeaderboard.DeleteAll();
        Repository.HurricaneLeaderboard.InsertBulk(newLeaderboard);

        SingletonInstance._isRunning = false;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Startup update
        await RunAsync(startupUpdate: true);
        
        using var timer = new PeriodicTimer(TimeSpan.FromMinutes(20));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await RunAsync();
            }
            catch (Exception ex)
            {
                await _errorService.PrintErrorAsync(ex);
            }
        }
    }
    
    private async Task NotifyNewHurricaneClansAsync(
        List<Guild> guilds,
        List<LadderStructure> added)
    {
        foreach (var guild in guilds)
        {
            foreach (var clan in added)
            {
                try
                {
                    await _channelMessageService.SendAsync(
                        ulong.Parse(guild.Id),
                        ulong.Parse(guild.ChannelId),
                        new MessageProperties().AddEmbeds(
                            new EmbedProperties
                            {
                                Title = "New hurricane clan",
                                Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                            })
                    );
                }
                catch (Exception e)
                {
                    await _errorService.PrintErrorAsync(e);
                }
            }
        }
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
                try
                {
                    await _channelMessageService.SendAsync(
                        ulong.Parse(guild.Id),
                        ulong.Parse(guild.ChannelId),
                        new MessageProperties().AddEmbeds(
                            new EmbedProperties
                            {
                                Title = "Clan dropped from Hurricane",
                                Description = $"`[{clan.Tag}] {clan.Name}` has dropped from Hurricane leaderboard!"
                            })
                    );
                }
                catch (Exception e)
                {
                    await _errorService.PrintErrorAsync(e);
                }
            }

            foreach (var clan in added)
            {
                try
                {
                    await _channelMessageService.SendAsync(
                        ulong.Parse(guild.Id),
                        ulong.Parse(guild.ChannelId),
                        new MessageProperties().AddEmbeds(
                            new EmbedProperties
                            {
                                Title = "New hurricane clan",
                                Description = $"`[{clan.Tag}] {clan.Name}` has entered Hurricane leaderboard!"
                            })
                    );
                }
                catch (Exception e)
                {
                    await _errorService.PrintErrorAsync(e);
                }
            }
        }
    }
}