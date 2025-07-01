namespace Arona.Commands;
using Utility;
using NetCord.Services.ApplicationCommands;
using System.Text.Json;
using NetCord.Rest;
using NetCord;

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

        var res = await client.GetAsync($"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/");
        JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync())
            .RootElement
            .GetProperty("clanview")
            .GetProperty("wows_ladder");
        
        int seasonNumber = doc.GetProperty("season_number").GetInt32();
        
        List<RatingsStructure> ratings = new List<RatingsStructure>();

        foreach (JsonElement rating in doc.GetProperty("ratings").EnumerateArray())
        {
            if (rating.GetProperty("season_number").GetInt32() != seasonNumber) continue;

            if (rating.GetProperty("stage").ValueKind != JsonValueKind.Null)
            {
                string team = rating.GetProperty("team_number").GetInt32() == 1 ? "Alpha" : "Bravo";
                string type = rating.GetProperty("stage").GetProperty("type").GetString()! == "promotion"
                    ? "Qualification for"
                    : "Qualification to stay in";
                
                string message = $"{type} {GetLeague( rating.GetProperty("stage").GetProperty("target_league").GetInt32() - (type == "Qualification for" ? 0 : 1) )} league" +
                                 $" {GetProgress( rating.GetProperty("stage").GetProperty("progress") )}";
                
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
        
        var field = new List<EmbedFieldProperties>();

        if (ratings.Count == 0)
            field.Add(new EmbedFieldProperties()
                .WithName("Clan doesn't play clan battles"));

        else foreach (var r in ratings)
            field.Add(new EmbedFieldProperties()
                .WithName(r.Team)
                .WithValue(r.Message)
                .WithInline(false));
        
        string tag = JsonDocument.Parse(await res.Content.ReadAsStringAsync())
            .RootElement
            .GetProperty("clanview")
            .GetProperty("clan")
            .GetProperty("tag")
            .GetString()!;
        string name = JsonDocument.Parse(await res.Content.ReadAsStringAsync())
            .RootElement
            .GetProperty("clanview")
            .GetProperty("clan")
            .GetProperty("name")
            .GetString()!;
        
        var embed = new EmbedProperties()
            .WithTitle($"`[{tag}] {name}`")
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
        string progress = "[";
        foreach (var result in arr.EnumerateArray())
        {
            progress = string.Concat(progress, result.GetString() == "victory" ? " 🟩 " : " 🟥 ");
        }
        progress = string.Concat(progress, "]");
        return progress;
    }
}