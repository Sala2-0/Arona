using System.Text.Json;
using Arona.Models.Api.Clans;

namespace Arona.Tests.Models.ApiTests.Clans;

public class ClanViewTest
{
    [Fact]
    public async Task MockDataTest()
    {
        var mockService = new MockApiService();
        var response = await mockService.HttpClient.GetAsync("https://clans.worldofwarships.eu/api/clanbase/500256050/claninfo/");
        response.EnsureSuccessStatusCode();
        
        var clanView = JsonSerializer.Deserialize<ClanViewRoot>(await response.Content.ReadAsStringAsync());
        
        Assert.NotNull(clanView);
        Assert.Equal(50, clanView.ClanView.WowsLadder.BattlesCount);
    }
}