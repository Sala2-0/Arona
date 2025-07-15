namespace Arona.Commands;
using Utility;
using NetCord.Services.ApplicationCommands;
using System.Text.Json;
using NetCord.Rest;
using NetCord;
using System.Threading.Tasks;
using ApiModels;

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
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());

        HttpClient client = new HttpClient();
        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        Task<string> generalTask = client.GetStringAsync(Clanbase.GetApiUrl(clanId, region));
        Task<string> globalRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId, region));
        Task<string> regionRankTask = client.GetStringAsync(LadderStructure.GetApiTargetClanUrl(clanId, region, LadderStructure.ConvertRegion(region)));

        string[] results = await Task.WhenAll(generalTask, globalRankTask, regionRankTask);

        var general = JsonSerializer.Deserialize<Clanbase>(results[0]);

        if (general == null)
        {
            await Context.Interaction.ModifyResponseAsync(options => options.Content = "❌ Error fetching clan data from API.");
            return;
        }

        int latestSeason = general.ClanView.WowsLadder.SeasonNumber;

        List<EmbedFieldProperties> field = [];

        List<RatingsStructure> ratings = [];

        foreach (Rating rating in general.ClanView.WowsLadder.Ratings)
        {
            if (rating.SeasonNumber != latestSeason) continue;

            if (rating.Stage != null)
            {
                string team = rating.TeamNumber == 1 ? "Alpha" : "Bravo";
                string type = rating.Stage.Type == "promotion"
                    ? "Qualification for"
                    : "Qualification to stay in";
                
                string message = $"{type} {GetLeague(rating.Stage.TargetLeague - (type == "Qualification for" ? 0 : 1) )} {GetDivision(rating.Stage.TargetDivision)}" +
                                 $"\n{GetProgress(rating.Stage.Progress)}";
                
                ratings.Add(new RatingsStructure(team, message));
            }

            else
            {
                string team = rating.TeamNumber == 1 ? "Alpha" : "Bravo";
                string message = $"{GetLeague(rating.League)}" +
                                 $" {GetDivision(rating.Division)}" +
                                 $" ({rating.DivisionRating})";
                
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
            .WithColor(new Color(Convert.ToInt32(general.ClanView.Clan.Color.Trim('#'), 16)))
            .WithFields(field);

        //await Context.Interaction.SendResponseAsync(
        //  InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([embed])));

        await Context.Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
    }

    public static string GetLeague(int league) => league switch
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

    public static string GetProgress(string[] arr)
    {
        string[] progress = [" ⬛ ", " ⬛ ", " ⬛ ", " ⬛ ", " ⬛ "];

        for (int p = 0; p < arr.Length; p++)
            progress[p] = arr[p] == "victory" ? " 🟩 " : " 🟥 ";

        string str = "";
        foreach (var result in progress)
            str = string.Concat(str, result);
        return str;
    }

    public static string GetLeagueColor(int league) => league switch
    {
        0 => "cda4ff", // Hurricane
        1 => "bee7bd", // Typhoon
        2 => "e3d6a0", // Storm
        3 => "cce4e4", // Gale
        4 => "cc9966", // Squall
        _ => "ffffff"  // Undefined
    };

    public static string GetRegionCodesClansApi(string region) => region switch
    {
        "eu" or "EU" => "eu",
        "asia" or "ASIA" => "sg",
        "com" => "us",
        _ => "undefined"
    };
}