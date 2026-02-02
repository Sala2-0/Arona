using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Services.UpdateTasks.Submethods;

namespace Arona.Tests.EmbedTests;

public class SessionEmbedTest
{
    [Fact]
    public void CreateEmbed_SessionEmbed_ShouldReturnCorrectStats()
    {
        var recentBattlesExample = new List<RecentBattle>
        {
            new()
            {
                BattleTime = 1769947764,
                IsVictory = true,
                PointsEarned = 32,
                TeamNumber = TeamNumber.Alpha
            },
            new()
            {
                BattleTime = 1769947765,
                IsVictory = true,
                PointsEarned = 14,
                TeamNumber = TeamNumber.Alpha
            },
            new()
            {
                BattleTime = 1769947766,
                IsVictory = false,
                PointsEarned = -20,
                TeamNumber = TeamNumber.Alpha
            },
            new()
            {
                BattleTime = 1769947767,
                IsVictory = true,
                PointsEarned = 20,
                TeamNumber = TeamNumber.Alpha
            },
        };

        UpdateClansSubmethods.CalculateSessionStats(
            recentBattlesExample,
            out var wins,
            out var totalPoints
        );

        var sessionEmbed = new SessionEmbed
        {
            IconUrl = "https://arona.se",
            ClanFullName = "[TAGG] Namn",
            BattlesCount = recentBattlesExample.Count,
            Date = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            Points = totalPoints,
            WinsCount = wins
        };

        Assert.Equal(recentBattlesExample.Count, sessionEmbed.BattlesCount);
        Assert.Equal(3, sessionEmbed.WinsCount);
        Assert.Equal(46, sessionEmbed.Points);
    }
}
