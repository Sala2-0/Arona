using System.Text.Json.Serialization;
using Arona.Utility;

namespace Arona.Models.Api.Clans;

public record LadderStructureByClanRequest(int ClanId, string Region, string Realm = "global");
public record LadderStructureByRealmRequest(string Realm, int League, int Division);

// Season = null => latest season data fetched
public record LadderStructureBySeasonRequest(int? Season, int League, int Division);

public class LadderStructureByClanQuery(HttpClient client) : QueryBase<LadderStructureByClanRequest, LadderStructure[]>(client)
{
    public override async Task<LadderStructure[]> GetAsync(LadderStructureByClanRequest request) =>
        await SendAndDeserializeAsync($"https://clans.worldofwarships.{request.Region}/api/ladder/structure/?clan_id={request.ClanId}&realm={request.Realm}");

    public static async Task<LadderStructure[]> GetSingleAsync(LadderStructureByClanRequest request)
    {
        var apiQuery = new LadderStructureByClanQuery(ApiClient.Instance);
        return await apiQuery.GetAsync(request);
    }
}

public class LadderStructureByRealmQuery(HttpClient client) : QueryBase<LadderStructureByRealmRequest, LadderStructure[]>(client)
{
    public override async Task<LadderStructure[]> GetAsync(LadderStructureByRealmRequest request)
    {
        var url = $"https://clans.worldofwarships.eu/api/ladder/structure/?league={request.League}&division={request.Division}&realm={request.Realm}";
        return await SendAndDeserializeAsync(url);
    }

    public static async Task<LadderStructure[]> GetSingleAsync(LadderStructureByRealmRequest request)
    {
        var apiQuery = new LadderStructureByRealmQuery(ApiClient.Instance);
        return await apiQuery.GetAsync(request);
    }
}

public class LadderStructureBySeasonQuery(HttpClient client) : QueryBase<LadderStructureBySeasonRequest, LadderStructure[]>(client)
{
    public override async Task<LadderStructure[]> GetAsync(LadderStructureBySeasonRequest request)
    {
        var url = request.Season.HasValue
            ? $"https://clans.worldofwarships.eu/api/ladder/structure/?season={request.Season.Value}&division={request.Division}&league={request.League}"
            : $"https://clans.worldofwarships.eu/api/ladder/structure/?division={request.Division}&league={request.League}";

        return await SendAndDeserializeAsync(url);
    }

    public static async Task<LadderStructure[]> GetSingleAsync(LadderStructureBySeasonRequest request)
    {
        var apiQuery = new LadderStructureBySeasonQuery(ApiClient.Instance);
        return await apiQuery.GetAsync(request);
    }
}

public class LadderStructure
{
    [JsonPropertyName("id")]
    public required int Id { get; init; }
    
    [JsonPropertyName("tag")]
    public required string Tag { get; init; }
    
    [JsonPropertyName("name")]
    public required string Name { get; init; }
    
    [JsonPropertyName("rank")]
    public required int Rank { get; init; }
    
    [JsonPropertyName("division_rating")]
    public required int DivisionRating { get; init; }
    
    [JsonPropertyName("realm")]
    public required string Realm { get; init; }
    
    [JsonPropertyName("public_rating")]
    public required int PublicRating { get; init; }
    
    [JsonPropertyName("battles_count")]
    public required int BattlesCount { get; init; }

    public double? SuccessFactor { get; set; }
}
