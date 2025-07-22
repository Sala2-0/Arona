namespace Arona.Commands;
using Utility;
using NetCord.Services.ApplicationCommands;
using System.Text.Json;
using NetCord.Rest;
using NetCord;
using System.Threading.Tasks;
using ApiModels;
using System.Globalization;

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

        try
        {
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

                string team = rating.TeamNumber == 1 ? "Alpha" : "Bravo";

                string successFactor = SuccessFactor.Calculate(rating.PublicRating, rating.BattlesCount, GetLeagueExponent(rating.League))
                    .ToString("0.##", CultureInfo.InvariantCulture);

                string winRate = rating.BattlesCount > 0
                    ? $"{Math.Round((double)rating.WinsCount / rating.BattlesCount * 100, 2)}%"
                    : "N/A";

                var obj = new RatingsStructure
                {
                    Team = team,
                    BattlesCount = rating.BattlesCount.ToString(),
                    WinRate = winRate,
                    SuccessFactor = successFactor
                };

                if (rating.Stage != null)
                {
                    string type = rating.Stage.Type == "promotion"
                        ? "Qualification for"
                        : "Qualification to stay in";

                    string message = $"{type} {GetLeague(rating.Stage.TargetLeague - (type == "Qualification for" ? 0 : 1))} {GetDivision(rating.Stage.TargetDivision)}" +
                                     $"\n{GetProgress(rating.Stage.Progress)}";

                    obj.Message = message;
                }
                else
                {
                    string message = $"{GetLeague(rating.League)}" +
                                     $" {GetDivision(rating.Division)}" +
                                     $" ({rating.DivisionRating})";

                    obj.Message = message;
                }

                ratings.Add(obj);
            }

            if (ratings.Count == 0)
                field.Add(new EmbedFieldProperties()
                    .WithName("Clan doesn't play clan battles."));

            else
            {
                ratings.Sort((a, b) => string.Compare(a.Team, b.Team, StringComparison.Ordinal));

                foreach (var r in ratings)
                {
                    field.Add(new EmbedFieldProperties()
                        .WithName(r.Team)
                        .WithValue(r.Message)
                        .WithInline());

                    field.Add(new EmbedFieldProperties()
                        .WithName("Battles")
                        .WithValue(r.BattlesCount)
                        .WithInline());

                    field.Add(new EmbedFieldProperties()
                        .WithName("Win rate")
                        .WithValue(r.WinRate)
                        .WithInline());

                    field.Add(new EmbedFieldProperties()
                        .WithName("S/F")
                        .WithValue(r.SuccessFactor)
                        .WithInline());

                    field.Add(new EmbedFieldProperties()
                        .WithInline(false)
                        .WithName("-----------------------------------------------------------------------------------"));
                }

                // Hämta klanens ranking
                var globalRankDoc = JsonSerializer.Deserialize<LadderStructure[]>(results[1])!;

                foreach (var clan in globalRankDoc)
                {
                    if (clan.Id != int.Parse(clanId)) continue;

                    field.Add(new EmbedFieldProperties()
                        .WithName("Global ranking")
                        .WithValue($"#{clan.Rank}")
                        .WithInline());
                    break;
                }

                var regionRankDoc = JsonSerializer.Deserialize<LadderStructure[]>(results[2])!;

                foreach (var clan in regionRankDoc)
                {
                    if (clan.Id != int.Parse(clanId)) continue;

                    field.Add(new EmbedFieldProperties()
                        .WithName("Region ranking")
                        .WithValue($"#{clan.Rank}")
                        .WithInline());
                    break;
                }
            }

            string tag = general.ClanView.Clan.Tag;
            string name = general.ClanView.Clan.Name;

            var embed = new EmbedProperties()
                .WithTitle($"`[{tag}] {name}` ({ClanSearchStructure.GetRegionCode(region)})")
                .WithColor(new Color(Convert.ToInt32(general.ClanView.Clan.Color.Trim('#'), 16)))
                .WithFields(field);

            await Context.Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            Program.ApiError(ex);
            await Context.Interaction.ModifyResponseAsync(options => options.Content = "❌ Error fetching clan data from API.");
        }
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

    public static double GetLeagueExponent(int league) => league switch
    {
        0 => 1.0, // Hurricane
        1 => 0.8, // Typhoon
        2 => 0.6, // Storm
        3 => 0.4, // Gale
        4 => 0.2, // Squall
        _ => 0    // Undefined
    };
}

internal class RatingsStructure()
{
    public string Team { init; get; }
    public string Message { set; get; }
    public string BattlesCount { init; get; }
    public string WinRate { init; get; }
    public string SuccessFactor { init; get; }
}