namespace Arona.Commands;
using Utility;
using NetCord.Services.ApplicationCommands;
using System.Text.Json;
using NetCord.Rest;
using NetCord;
using System.Threading.Tasks;

public class RatingsStructure(string team, string message)
{
    public readonly string Team = team;
    public readonly string Message = message;
}

public class Ratings : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("ratings", "Get detailed information about a clans current ratings on current CB season")]
    public async Task RatingsFn(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for",
            AutocompleteProviderType = typeof(ClanSearch))] string clanIdAndRegion)
    {
        HttpClient client = new HttpClient();
        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        Task<string> generalTask = client.GetStringAsync($"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/");
        Task<string> globalRankTask = client.GetStringAsync($"https://clans.worldofwarships.{region}/api/ladder/structure/?clan_id={clanId}&realm=global");
        Task<string> regionRankTask = client.GetStringAsync($"https://clans.worldofwarships.{region}/api/ladder/structure/?clan_id={clanId}&realm={GetRegionCodesClansApi(region)}");

        string[] results = await Task.WhenAll(generalTask, globalRankTask, regionRankTask);

        JsonElement generalDoc = JsonDocument.Parse(results[0])
            .RootElement
            .GetProperty("clanview")
            .GetProperty("wows_ladder");
        
        int seasonNumber = generalDoc.GetProperty("season_number").GetInt32();

        List<EmbedFieldProperties> field = [];

        List<RatingsStructure> ratings = [];

        foreach (JsonElement rating in generalDoc.GetProperty("ratings").EnumerateArray())
        {
            if (rating.GetProperty("season_number").GetInt32() != seasonNumber) continue;

            if (rating.GetProperty("stage").ValueKind != JsonValueKind.Null)
            {
                string team = rating.GetProperty("team_number").GetInt32() == 1 ? "Alpha" : "Bravo";
                string type = rating.GetProperty("stage").GetProperty("type").GetString()! == "promotion"
                    ? "Qualification for"
                    : "Qualification to stay in";
                
                string message = $"{type} {GetLeague( rating.GetProperty("stage").GetProperty("target_league").GetInt32() - (type == "Qualification for" ? 0 : 1) )} league" +
                                 $"\n{GetProgress( rating.GetProperty("stage").GetProperty("progress") )}";
                
                ratings.Add(new RatingsStructure(team, message));
            }

            else
            {
                string team = rating.GetProperty("team_number").GetInt32() == 1 ? "Alpha" : "Bravo";
                string message = $"{GetLeague(rating.GetProperty("league").GetInt32())}" +
                                 $" {GetDivision(rating.GetProperty("division").GetInt32())}" +
                                 $" ({rating.GetProperty("division_rating").GetInt32()})";
                
                ratings.Add(new RatingsStructure(team, message));
            }
        }

        if (ratings.Count == 0)
            field.Add(new EmbedFieldProperties()
                .WithName("Clan doesn't play clan battles"));

        else
        {
            ratings.Sort((a, b) => string.Compare(a.Team, b.Team, StringComparison.Ordinal));
            foreach (var r in ratings)
                field.Add(new EmbedFieldProperties()
                    .WithName(r.Team)
                    .WithValue(r.Message)
                    .WithInline(false));

            // Hämta klanens ranking
            JsonElement globalRankDoc = JsonDocument.Parse(results[1])
                .RootElement;

            foreach (JsonElement clan in globalRankDoc.EnumerateArray())
            {
                if (clan.GetProperty("id").GetInt32() != int.Parse(clanId)) continue;

                field.Add(new EmbedFieldProperties()
                    .WithName("Global ranking")
                    .WithValue($"#{clan.GetProperty("rank").GetInt32()}")
                    .WithInline());
                break;
            }

            JsonElement regionRankDoc = JsonDocument.Parse(results[2])
                .RootElement;

            foreach (JsonElement clan in regionRankDoc.EnumerateArray())
            {
                if (clan.GetProperty("id").GetInt32() != int.Parse(clanId)) continue;

                field.Add(new EmbedFieldProperties()
                    .WithName("Region ranking")
                    .WithValue($"#{clan.GetProperty("rank").GetInt32()}")
                    .WithInline());
                break;
            }
        }

        string tag = JsonDocument.Parse(results[0])
            .RootElement
            .GetProperty("clanview")
            .GetProperty("clan")
            .GetProperty("tag")
            .GetString()!;
        string name = JsonDocument.Parse(results[0])
            .RootElement
            .GetProperty("clanview")
            .GetProperty("clan")
            .GetProperty("name")
            .GetString()!;

        var embed = new EmbedProperties()
            .WithTitle($"`[{tag}] {name}` ({ClanSearchStructure.GetRegionCode(region)})")
            .WithColor(new Color(Convert.ToInt32("a4fff7", 16)))
            .WithFields(field);
        
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([ embed ])));
    }

    public static string GetLeague(int targetLeague) => targetLeague switch
    {
        0 => "Hurricane",
        1 => "Typhoon",
        2 => "Storm",
        3 => "Gale",
        4 => "Squall",
        _ => "undefined",
    };

    public static string GetDivision(int division) => division switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        _ => "undefined",
    };

    public static string GetProgress(JsonElement arr)
    {
        string[] progress = [" ⬛ ", " ⬛ ", " ⬛ ", " ⬛ ", " ⬛ "];

        for (int p = 0; p < arr.GetArrayLength(); p++)
            progress[p] = arr[p].GetString() == "victory" ? " 🟩 " : " 🟥 ";

        string str = "";
        foreach (var result in progress)
            str = string.Concat(str, result);
        return str;
    }

    public static string GetRegionCodesClansApi(string region) => region switch
    {
        "eu" or "EU" => "eu",
        "asia" or "ASIA" => "sg",
        "com" => "us",
        _ => "undefined"
    };
}