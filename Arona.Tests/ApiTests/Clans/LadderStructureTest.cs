using System.Net;
using System.Text.Json;
using Arona.Models.Api.Clans;
using Arona.Utility;

namespace Arona.Tests.ApiTests.Clans;

public class LadderStructureTest
{
    [Fact]
    public async Task GetAsync_LadderStructureByClanQuery_ShouldReturnCorrectApi()
    {
        var expectedData = new List<LadderStructure>
        {
            new()
            {
                BattlesCount = 60,
                DivisionRating = 200,
                Id = 500256050,
                Name = "Tepartiet",
                Tag = "SEIA",
                PublicRating = 2400,
                Rank = 1,
                Realm = "eu",
            },
            new()
            {
                BattlesCount = 120,
                DivisionRating = 160,
                Id = 500140589,
                Name = "-TWA-",
                Tag = "Total War Alliance",
                PublicRating = 2360,
                Rank = 2,
                Realm = "eu",
            },
            new()
            {
                BattlesCount = 156,
                DivisionRating = 12,
                Id = 500140589,
                Name = "AYAYA",
                Tag = "Lolita",
                PublicRating = 2212,
                Rank = 3,
                Realm = "sg",
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

        var query = new LadderStructureByClanQuery(client);

        const int targetClanId = 500256050,
            targetNotExistsClanId = 500143673;
        const string targetRegion = "eu";

        var result = await query.GetAsync(new LadderStructureByClanRequest(targetClanId, targetRegion));

        Assert.NotNull(result);
        Assert.True(Array.Exists(result, c => c.Id == targetClanId), 
            $"Clan '{targetClanId}' should exist on the leaderboards but it does not.");
        Assert.False(Array.Exists(result, c => c.Id == targetNotExistsClanId), 
            $"Clan '{targetNotExistsClanId}' should not exist on the leaderboards but it does.");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAsync_LadderStructureByRealmRequestApi_ShouldReturnCorrectRealm()
    {
        var query = new LadderStructureByRealmQuery(ApiClient.Instance);

        const string targetRealm = "eu",
            expectedRealm = "eu";

        var result = await query.GetAsync(new LadderStructureByRealmRequest(targetRealm, 0, 1));

        Assert.NotNull(result);
        Assert.NotEqual(0, result.Length);
        Assert.True(result.All(c => c.Realm == expectedRealm),
            $"Expected region '{expectedRealm}' but got '{targetRealm}'");
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetAsync_LadderStructureBySeasonRequestApi_ShouldReturnValidData()
    {
        var query = new LadderStructureBySeasonQuery(ApiClient.Instance);

        const int targetSeasonNumber = 32,
            expectedSeasonNumber = 32;

        var result = await query.GetAsync(new LadderStructureBySeasonRequest(targetSeasonNumber, 0, 1));

        Assert.NotNull(result);
        Assert.NotEqual(0, result.Length);
    }
}