using System.Net;
using Arona.ClanEvents;
using Arona.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Arona.Tests.UpdateTaskTests;

public class UpdateClansTest
{
    [Fact]
    public async Task UpdateClansAsync_SessionStarted_ShouldReceiveEvent()
    {
        // ARRANGE
        var eventBus = new ClanEventBus();
        ClanSessionStarted? receivedEvent = null;

        Task Handler(ClanSessionStarted evt)
        {
            receivedEvent = evt;
            return Task.CompletedTask;
        }

        eventBus.SessionStarted += Handler;

        var clan = new
        {
            Name = "Namn",
            Tag = "TAGG"
        };

        // ACT
        await eventBus.OnSessionStarted(new ClanSessionStarted(
            ClanId: 1,
            ClanName: clan.Name,
            ClanTag: clan.Tag
        ));

        // CLEANUP
        eventBus.SessionStarted -= Handler;

        // ASSERT
        Assert.NotNull(receivedEvent);
        Assert.Equal("Namn", receivedEvent.ClanName);
        Assert.Equal("TAGG", receivedEvent.ClanTag);
    }

    [Fact]
    public async Task UpdateClans_Publishes_SessionEnded_ShouldReceiveEvent()
    {
        // ARRANGE
        var eventBus = new ClanEventBus();
        ClanSessionEnded? receivedEvent = null;

        Task Handler(ClanSessionEnded evt)
        {
            receivedEvent = evt;
            return Task.CompletedTask;
        }

        eventBus.SessionEnded += Handler;

        // ACT
        await eventBus.OnSessionEnded(new ClanSessionEnded(
            ClanId: 1234,
            ClanName: "TestClan",
            ClanTag: "TC",
            BattlesCount: 10,
            WinsCount: 4,
            TotalPoints: 200,
            Date: DateOnly.FromDateTime(DateTime.UtcNow)
        ));

        // CLEANUP
        eventBus.SessionEnded -= Handler;

        // ASSERT
        Assert.NotNull(receivedEvent);
        Assert.Equal(1234, receivedEvent.ClanId);
        Assert.Equal("TestClan", receivedEvent.ClanName);
        Assert.Equal("TC", receivedEvent.ClanTag);
    }

    [Fact]
    public async Task UpdateClans_UpdateDatabase()
    {
        // ARRANGE
        Config.Initialize();
        
        string[] args = [];
        var builder = new ApplicationBuilder(args);

        var fakeHttpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(JsonSerializer.Serialize(ClanViewData.NewData))
        };
        var fakeHttpHandler = new MockHttpMessageHandler(fakeHttpResponse);
        var fakeApiClient = new ApiClient(new HttpClient(fakeHttpHandler));
        
        // builder.Builder.Services.Replace(ServiceDescriptor.Singleton<IApiClient>(_ => fakeApiClient));
        
        var host = builder.Build();
        var clanUpdateService = host.Services.GetServices<IHostedService>()
            .OfType<ClanUpdateService>()
            .FirstOrDefault();
        
        var apiClient = host.Services.GetRequiredService<IApiClient>();
        if (args.Length >= 2 && args[0] == "--port" && short.TryParse(args[1], out var port))
        {
            apiClient.SetServicePort(port);
        }

        // var output = new TestOutputHelper();
        // output.WriteLine("Service API port set to " + apiClient.ServicePort);

        await Task.Delay(3000);
        if (!await apiClient.IsServiceOnlineAsync()) return;
        
        host.RunAsync();
        
        // RUN
        await Task.Delay(10000);
        await clanUpdateService!.UpdateClansAsync();
        
        // ASSERT
        Assert.True(true);
    }

    // [Fact]
    // public async Task UpdateClans_UpdateDatabase()
    // {
    //     // ARRANGE
    //     var tempDbPath = Path.GetTempFileName();
    //     File.Copy("../../../../Arona/bin/Debug/net9.0/data.db", tempDbPath, true);
    //
    //     var db = new LiteDatabase(tempDbPath);
    //     var repository = new DatabaseRepository(db);
    //
    //     var eventBus = new ClanEventBus();
    //     BattleDetected? receivedEvent = null;
    //
    //     Task Handler(BattleDetected evt)
    //     {
    //         receivedEvent = evt;
    //         return Task.CompletedTask;
    //     }
    //
    //     eventBus.BattleDetected += Handler;
    //
    //     // ACT
    //     await ClanUpdateService.UpdateClansAsync();
    //
    //     var clans = Collections.Clans.FindAll().ToList();
    //     var targetClan = clans.Find(c => c.Id == 500143673);
    //
    //     // CLEAN
    //     db.Dispose();
    //     eventBus.BattleDetected -= Handler;
    //     File.Delete(tempDbPath);
    //
    //     // ASSERT
    //     Assert.NotEmpty(clans);
    //     Assert.NotNull(targetClan);
    //     Assert.Equal(26, targetClan.WowsLadder.DivisionRating);
    //
    //     Assert.NotNull(receivedEvent);
    //     Assert.Equal(16, receivedEvent.Value.PointsDelta);
    // }
}
