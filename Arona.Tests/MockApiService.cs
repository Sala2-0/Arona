using System.Net;
using Arona.Services;

namespace Arona.Tests;

public class MockApiService : ApiService
{
    public MockApiService()
    {
        HttpClient = new HttpClient(new MockHttpMessageHandler(request =>
        {
            return request.RequestUri!.OriginalString switch
            {
                "https://clans.worldofwarships.eu/api/clanbase/500256050/claninfo/" => new HttpResponseMessage(HttpStatusCode.OK) 
                {
                    Content = new StringContent(GetJsonString("ClanView"))
                },
                "https://clans.worldofwarships.eu/api/ladder/structure/?clan_id=500256050&realm=eu" or
                    "https://clans.worldofwarships.eu/api/ladder/structure/?clan_id=500256050&realm=global" => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(GetJsonString("LadderStructure"))
                    },
                var s when s.Contains("https://api.worldofwarships.eu/wows/clans/season/") => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GetJsonString("ClanBattleSeasons"))
                },
                "https://clans.worldofwarships.eu/account_info_sync/" => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GetJsonString("AccountInfoSync"))
                },
                "https://clans.worldofwarships.eu/api/ladder/battles/?team=1" => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(GetJsonString("LadderBattles"))
                },
                _ => null
            };
        }));
    }

    private static string GetJsonString(string fileName)
        => File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", $"ExampleData/{fileName}.json"));
}