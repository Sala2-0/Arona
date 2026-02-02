using Arona.ClanEvents;
using Arona.Models.DB;
using Arona.Services;
using Arona.Services.UpdateTasks;
using LiteDB;

namespace Arona.Tests.UpdateTaskTests;

public class UpdateClansTest
{
    [Fact]
    public async Task UpdateClansAsync_SessionStarted_ShouldReceiveEvent()
    {
        // ARRANGE
        ClanSessionStarted? receivedEvent = null;

        Task Handler(ClanSessionStarted evt)
        {
            receivedEvent = evt;
            return Task.CompletedTask;
        }

        ClanEventBus.SessionStarted += Handler;

        var clan = new
        {
            Name = "Namn",
            Tag = "TAGG"
        };

        // ACT
        await ClanEventBus.OnSessionStarted(new ClanSessionStarted(
            ClanId: 1,
            ClanName: clan.Name,
            ClanTag: clan.Tag
        ));

        // CLEANUP
        ClanEventBus.SessionStarted -= Handler;

        // ASSERT
        Assert.NotNull(receivedEvent);
        Assert.Equal("Namn", receivedEvent.ClanName);
        Assert.Equal("TAGG", receivedEvent.ClanTag);
    }

    [Fact]
    public async Task UpdateClans_Publishes_SessionEnded_ShouldReceiveEvent()
    {
        // ARRANGE
        ClanSessionEnded? receivedEvent = null;

        Task Handler(ClanSessionEnded evt)
        {
            receivedEvent = evt;
            return Task.CompletedTask;
        }

        ClanEventBus.SessionEnded += Handler;

        // ACT
        await ClanEventBus.OnSessionEnded(new ClanSessionEnded(
            ClanId: 1234,
            ClanName: "TestClan",
            ClanTag: "TC",
            BattlesCount: 10,
            WinsCount: 4,
            TotalPoints: 200,
            Date: DateOnly.FromDateTime(DateTime.UtcNow)
        ));

        // CLEANUP
        ClanEventBus.SessionEnded -= Handler;

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
        var tempDbPath = Path.GetTempFileName();
        File.Copy("../../../../Arona/bin/Debug/net9.0/data.db", tempDbPath, true);

        var db = new LiteDatabase(tempDbPath);
        Collections.Initialize(db);

        BattleDetected? receivedEvent = null;

        Task Handler(BattleDetected evt)
        {
            receivedEvent = evt;
            return Task.CompletedTask;
        }

        ClanEventBus.BattleDetected += Handler;

        // ACT
        await UpdateTasks.UpdateClansAsync();

        var clans = Collections.Clans.FindAll().ToList();
        var targetClan = clans.Find(c => c.Id == 500143673);

        // CLEAN
        db.Dispose();
        ClanEventBus.BattleDetected -= Handler;
        File.Delete(tempDbPath);

        // ASSERT
        Assert.NotEmpty(clans);
        Assert.NotNull(targetClan);
        Assert.Equal(26, targetClan.WowsLadder.DivisionRating);

        Assert.NotNull(receivedEvent);
        Assert.Equal(16, receivedEvent.Value.PointsDelta);
    }
}
