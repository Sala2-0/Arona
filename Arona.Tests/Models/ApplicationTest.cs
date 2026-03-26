using Arona.Models.DB;
using Arona.Services;
using Arona.Services.BackgroundTask;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Arona.Tests.Models;

public class ApplicationTest
{
    [Fact]
    public async Task StartupTest()
    {
        var args = Environment.GetCommandLineArgs();
        Config.Initialize();
        
        var builder = new ApplicationBuilder(args);
        builder.SetDatabase(new MockDatabaseService(new MemoryStream()));
        builder.SetApiService(new MockApiService());
        var host = builder.Build();
        
        var apiService = host.Services.GetRequiredService<IApiService>();
        if (args is ["--port", var portStr, ..] && short.TryParse(portStr, out var port))
        {
            apiService.SetServicePort(port);
        }   
        
        await Task.Delay(3000);
        if (!await apiService.IsServiceOnline()) return;
        
        var database =  host.Services.GetRequiredService<LiteDatabase>();
        var emojiService = host.Services.GetRequiredService<EmojiService>();
        await emojiService.InitializeAsync();
        
        Repository.Initialize(database);
        
        host.RunAsync();
        await Task.Delay(TimeSpan.FromSeconds(30));
    }

    [Fact]
    public async Task ClanUpdateTest()
    {
        var args = Environment.GetCommandLineArgs();
        Config.Initialize();
        
        var builder = new ApplicationBuilder(args);
        builder.SetDatabase(new MockDatabaseService(new MemoryStream()));
        builder.SetApiService(new MockApiService());
        var host = builder.Build();
        
        var apiService = host.Services.GetRequiredService<IApiService>();
        if (args is ["--port", var portStr, ..] && short.TryParse(portStr, out var port))
        {
            apiService.SetServicePort(port);
        }   
        
        await Task.Delay(3000);
        if (!await apiService.IsServiceOnline()) return;
        
        var database =  host.Services.GetRequiredService<LiteDatabase>();
        var emojiService = host.Services.GetRequiredService<EmojiService>();
        await emojiService.InitializeAsync();
        
        Repository.Initialize(database);
        
        host.RunAsync();
        
        var errorService = host.Services.GetRequiredService<ErrorService>();
        
        await Task.Delay(TimeSpan.FromSeconds(5));
        await UpdateClanData.RunAsync(errorService, apiService);
        await Task.Delay(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void MockDatabaseTest()
    {
        var args = Environment.GetCommandLineArgs();
        Config.Initialize();
        
        var builder = new ApplicationBuilder(args);
        builder.SetDatabase(new MockDatabaseService(new MemoryStream()));
        builder.SetApiService(new MockApiService());
        var host = builder.Build();
        
        var database = host.Services.GetRequiredService<LiteDatabase>();
        
        Repository.Initialize(database);

        var guild = Repository.Guilds.FindById(Config.GuildId);
        var clan = Repository.Clans.FindById(500256050);
        
        Assert.NotNull(guild);
        Assert.NotNull(clan);
        
        Assert.Equal(3180, clan.WowsLadder.PublicRating);
    }
}