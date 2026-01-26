using System.Net;
using System.Text.Json;
using Arona.Models;
using Arona.Models.Api.Clans;

namespace Arona.Tests.ApiTests.Clans;

public class ClanViewTest
{
    [Fact]
    public async Task GetAsync_ClanviewQuery_ShouldReturnCorrectApi()
    {
        // ARRANGE
        var expectedData = new ClanViewRoot
        {
            ClanView = new ClanView
            {
                Clan = new Clan
                {
                    Id = 500256050,
                    Color = "#cda4ff",
                    Name = "Tepartiet",
                    Tag = "SEIA"
                },

                WowsLadder = new WowsLadder
                {
                    Division = Division.I,
                    DivisionRating = 200,
                    LastBattleAt = "2026 - 01 - 25T20:09:32 + 00:00",
                    LeadingTeamNumber = Team.Alpha,
                    League = League.Hurricane,
                    PlannedPrimeTime = 4,
                    PrimeTime = 4,
                    SeasonNumber = 32,
                    Ratings =
                    [
                        new Rating
                        {
                            BattlesCount = 60,
                            WinsCount = 55,
                            Division = Division.I,
                            DivisionRating = 200,
                            League = League.Hurricane,
                            PublicRating = 2400,
                            SeasonNumber = 32,
                            Stage = null,
                            TeamNumber = Team.Alpha
                        },
                        new Rating
                        {
                            BattlesCount = 46,
                            WinsCount = 34,
                            Division = Division.I,
                            DivisionRating = 99,
                            League = League.Typhoon,
                            PublicRating = 2199,
                            SeasonNumber = 32,
                            Stage = new Stage
                            {
                                Battles = 5,
                                Progress = ["victory", "defeat"],
                                TargetDivision = Division.I,
                                TargetLeague = League.Hurricane,
                                Type = StageType.Promotion,
                                VictoriesRequired = 5
                            },
                            TeamNumber = Team.Alpha
                        }
                    ]
                }
            }
        };
        var jsonResponse = JsonSerializer.Serialize(expectedData);
        var httpResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(jsonResponse)
        };

        var mockHandler = new MockHttpMessageHandler(httpResponse);
        var client = new HttpClient(mockHandler);

        var clanViewQuery = new ClanViewQuery(client);

        // ACT
        //await Assert.ThrowsAsync<HttpRequestException>(() =>
        //    clanViewQuery.GetAsync(new ClanViewRequest("eu", 12345)));

        var result = await clanViewQuery.GetAsync(new ClanViewRequest("eu", 500256050));

        // ASSERT
        Assert.NotNull(result);

        Assert.Equal(League.Hurricane, result.ClanView.WowsLadder.League);
        Assert.Equal(32, result.ClanView.WowsLadder.SeasonNumber);
    }
}