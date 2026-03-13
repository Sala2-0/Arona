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
                    Content = new StringContent(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ExampleData/ClanView.json")))
                },
                "https://clans.worldofwarships.eu/api/ladder/structure/?clan_id=500256050&realm=eu" or
                    "https://clans.worldofwarships.eu/api/ladder/structure/?clan_id=500256050&realm=global" => new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ExampleData/LadderStructure.json")))
                    },
                var s when s.Contains("https://api.worldofwarships.eu/wows/clans/season/") => new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "ExampleData/ClanBattleSeasons.json")))
                },
                _ => null
            };
        }));
    }
}