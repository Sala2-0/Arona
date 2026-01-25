using System.Net;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Serialization;
using Arona.Utility;

using TeamNumber = Arona.Models.Team;

namespace Arona.Models.Api.Clans;

internal record LadderBattlesRequest(string Region, TeamNumber Team, string Cookie);

internal class LadderBattlesQuery(HttpClient client) : QueryBase<LadderBattlesRequest, LadderBattle[]>(client)
{
    public override async Task<LadderBattle[]> GetAsync(LadderBattlesRequest req)
    {
        var url = $"https://clans.worldofwarships.{req.Region}/api/ladder/battles/?team={(int)req.Team}";

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", $"wsauth_token={req.Cookie};");

        return await SendAndDeserializeAsync(url, request);
    }
}

internal class LadderBattle
{
    [JsonPropertyName("season_number")]
    public required int SeasonNumber { get; set; }

    [JsonPropertyName("teams")]
    public required Team[] Teams { get; set; }

    internal class Team
    {
        [JsonPropertyName("result")]
        public required string Result { get; set; }

        [JsonPropertyName("clan_id")]
        public required int ClanId { get; set; }

        [JsonPropertyName("id")]
        public required int Id { get; set; }

        [JsonPropertyName("division_rating")]
        public required int DivisionRating { get; set; }

        [JsonPropertyName("division")]
        public required Division Division { get; set; }

        [JsonPropertyName("league")]
        public required League League { get; set; }

        [JsonPropertyName("rating_delta")]
        public required int RatingDelta { get; set; }

        [JsonPropertyName("team_number")]
        public required TeamNumber TeamNumber { get; set; }

        [JsonPropertyName("players")]
        public required Player[] Players { get; set; }

        [JsonPropertyName("claninfo")]
        public required ClanInfo ClanInfo { get; set; }

        [JsonPropertyName("stage")]
        public required Stage? Stage { get; set; }
    }

    internal class Player
    {
        [JsonPropertyName("spa_id")]
        public required long AccountId { get; set; }

        [JsonPropertyName("clan_id")]
        public required int ClanId { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        [JsonPropertyName("survived")]
        public required bool Survived { get; set; }

        [JsonPropertyName("ship")]
        public required Ship Ship { get; set; }
    }

    internal class Ship
    {
        [JsonPropertyName("level")]
        public required Text.Tier Level { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

    }

    internal class ClanInfo
    {
        [JsonPropertyName("tag")]
        public required string Tag { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }
    }
}