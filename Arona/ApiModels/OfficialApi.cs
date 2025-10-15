using System.Text.Json;
using System.Text.Json.Serialization;

namespace Arona.ApiModels;

internal static class OfficialApi
{
    /// <summary>
    /// Used to deserialize responses from Official API and only extract what we need.
    /// </summary>
    /// <remarks>This version is for data that deserializes to an array</remarks>
    /// <typeparam name="T">
    /// Property that [JsonPropertyName("data")] deserializes to.
    /// </typeparam>
    public class ResponseArray<T>
    {
        [JsonPropertyName("data")]
        public required T[] Data { get; set; }
    }

    /// <summary>
    /// Used to deserialize responses from Official API and only extract what we need.
    /// </summary>
    /// <remarks>This version is for data that deserializes to an object</remarks>
    /// <typeparam name="T">
    /// Property that [JsonPropertyName("data")] deserializes to.
    /// </typeparam>
    public class ResponseObject<T>
    {
        [JsonPropertyName("data")]
        public Dictionary<string, T> Data { get; set; }
    }

    /// <summary>
    /// Response deserialization model for API endpoint /wows/clans/list/ which returns a list of clans based on search query.
    /// </summary>
    public class Clan : IOfficialApiArray<Clan>
    {
        /// <summary>
        /// Total number of members in the clan.
        /// </summary>
        [JsonPropertyName("members_count")]
        public required int MemberCount { get; set; }

        /// <summary>
        /// Clan creation date as a Unix timestamp.
        /// </summary>
        [JsonPropertyName("created_at")]
        public required long CreatedAt { get; set; }

        [JsonPropertyName("clan_id")]
        public required int ClanId { get; set; }

        [JsonPropertyName("tag")]
        public required string Tag { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        public static async Task<Clan[]> GetAsync(string query, string region)
        {
            using HttpClient client = new();
            var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/clans/list/?application_id={Config.WgApi}&search={query}");
            res.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<ResponseArray<Clan>>(await res.Content.ReadAsStringAsync())!;

            return data.Data;
        }
    }

    public class Player : IOfficialApiArray<Player>
    {
        [JsonPropertyName("account_id")]
        public required long AccountId { get; set; }

        [JsonPropertyName("nickname")]
        public required string Nickname { get; set; }
       
        public static async Task<Player[]> GetAsync(string query, string region)
        {
            using HttpClient client = new();
            var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/account/list/?application_id={Config.WgApi}&search={query}");
            res.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<ResponseArray<Player>>(await res.Content.ReadAsStringAsync())!;

            return data.Data;
        }
    }

    public class ClanBattlesSeasonStats : IOfficialApiObject<ClanBattlesSeasonStats>
    {
        [JsonPropertyName("seasons")]
        public required Season[] Seasons { get; set; }

        public class Season
        {
            [JsonPropertyName("season_id")]
            public required int SeasonId { get; set; }

            [JsonPropertyName("battles")]
            public required int Battles { get; set; }

            [JsonPropertyName("wins")]
            public required int Wins { get; set; }

            [JsonPropertyName("losses")]
            public required int Losses { get; set; }

            [JsonPropertyName("draws")]
            public required int Draws { get; set; }

            [JsonPropertyName("damage_dealt")]
            public required int DamageDealt { get; set; }

            [JsonPropertyName("frags")]
            public required int Kills { get; set; }
        }

        public static async Task<Dictionary<string, ClanBattlesSeasonStats>> GetAsync(string accountId, string region)
        {
            using HttpClient client = new();
            var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/clans/seasonstats/?application_id={Config.WgApi}&account_id={accountId}");
            res.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<ResponseObject<ClanBattlesSeasonStats>>(await res.Content.ReadAsStringAsync())!;

            return data.Data;
        }
    }

    public class ClanBattleSeasons
    {
        [JsonPropertyName("season_id")]
        public required int SeasonId { get; set; }

        [JsonPropertyName("start_time")]
        public required long StartTime { get; set; }

        [JsonPropertyName("finish_time")]
        public required long FinishTime { get; set; }

        [JsonPropertyName("name")]
        public required string Name { get; set; }

        public static async Task<Dictionary<string, ClanBattleSeasons>> GetAsync()
        {
            using HttpClient client = new();
            var res = await client.GetAsync($"https://api.worldofwarships.eu/wows/clans/season/?application_id={Config.WgApi}");
            res.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<ResponseObject<ClanBattleSeasons>>(await res.Content.ReadAsStringAsync())!;

            return data.Data;
        }
    }

    public interface IOfficialApiArray<T>
    {
        public static abstract Task<T[]> GetAsync(string query, string region);
    }

    public interface IOfficialApiObject<T>
    {
        public static abstract Task<Dictionary<string, T>> GetAsync(string query, string region);
    }
}