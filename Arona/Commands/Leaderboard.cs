using Arona.Utility;

namespace Arona.Commands;
using ApiModels;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using System.Text.Json;
using System.Globalization;

public class Leaderboard : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("leaderboard", "Latest clan battles season leaderboard. Default: Hurricane I (Global)")]
    public async Task LeaderboardAsync(
        [SlashCommandParameter(Name = "league", Description = "League", AutocompleteProviderType = typeof(League))]
        int league = 0,
        [SlashCommandParameter(Name = "division", Description = "Division",
            AutocompleteProviderType = typeof(Division))]
        int division = 1,
        [SlashCommandParameter(Name = "region", Description = "Region", AutocompleteProviderType = typeof(Realm))]
        string realm = "global"
    )
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());

        if (league == 0 && division is 2 or 3)
        {
            await Context.Interaction.ModifyResponseAsync(options =>
                options.Content = $"❌ Hurricane {Ratings.GetDivision(division)} doesn't exist.");
            return;
        }

        HttpClient client = new HttpClient();
        string apiUrl = LadderStructure.GetApiGeneralUrl(league, division, realm);

        try
        {
            var response = await client.GetAsync(apiUrl);

            var structure = JsonSerializer.Deserialize<LadderStructure[]>(await response.Content.ReadAsStringAsync());

            if (structure == null || structure.Length == 0)
            {
                await Context.Interaction.ModifyResponseAsync(options =>
                    options.Content = "❌ No data found for the specified league and division.");
                return;
            }

            var embed = new EmbedProperties()
                .WithTitle(
                    $"Leaderboard - {Ratings.GetLeague(league)} {Ratings.GetDivision(division)} ({LadderStructure.ConvertRealm(realm)})")
                .WithColor(new Color(Convert.ToInt32(Ratings.GetLeagueColor(league), 16)));

            var fields = new List<EmbedFieldProperties>();

            foreach (var clan in structure)
            {
                string successFactor = SuccessFactor.Calculate(clan.PublicRating, clan.BattlesCount, Ratings.GetLeagueExponent(league))
                    .ToString("0.##", CultureInfo.InvariantCulture);

                fields.Add(new EmbedFieldProperties()
                    .WithName(
                        $"**#{clan.Rank}** ({LadderStructure.ConvertRealm(clan.Realm)}) `[{clan.Tag}]` ({clan.DivisionRating}) `BTL: {clan.BattlesCount}` `S/F: {successFactor}`"));
            }

            embed.WithFields(fields);

            await Context.Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
        }
        catch (Exception ex)
        {
            Program.ApiError(ex);
            await Context.Interaction.ModifyResponseAsync(options => options.Content = "❌ Error fetching leaderboard data from API.");
        }
    }
}

internal class League : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var leagues = new[]
        {
            new ApplicationCommandOptionChoiceProperties("Hurricane", 0),
            new ApplicationCommandOptionChoiceProperties("Typhoon", 1),
            new ApplicationCommandOptionChoiceProperties("Storm", 2),
            new ApplicationCommandOptionChoiceProperties("Gale", 3),
            new ApplicationCommandOptionChoiceProperties("Squall", 4)
        };
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(leagues);
    }
}

internal class Division : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var divisions = new[]
        {
            new ApplicationCommandOptionChoiceProperties("I", 1),
            new ApplicationCommandOptionChoiceProperties("II", 2),
            new ApplicationCommandOptionChoiceProperties("III", 3)
        };
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(divisions);
    }
}

internal class Realm : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var realms = new[]
        {
            new ApplicationCommandOptionChoiceProperties("Global", "global"),
            new ApplicationCommandOptionChoiceProperties("EU", "eu"),
            new ApplicationCommandOptionChoiceProperties("NA", "us"),
            new ApplicationCommandOptionChoiceProperties("ASIA", "sg")
        };
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(realms);
    }
}