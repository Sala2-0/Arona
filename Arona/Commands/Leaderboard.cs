using System.Globalization;
using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;
using Arona.Utility;

namespace Arona.Commands;

public class Leaderboard : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("leaderboard", "Latest clan battles season leaderboard. Default: Hurricane I (Global) [Ratings]")]
    public async Task LeaderboardAsync(
        [SlashCommandParameter(Name = "league", Description = "League", AutocompleteProviderType = typeof(League))]
        int league = 0,
        [SlashCommandParameter(Name = "division", Description = "Division",
            AutocompleteProviderType = typeof(Division))]
        int division = 1,
        [SlashCommandParameter(Name = "region", Description = "Region", AutocompleteProviderType = typeof(Realm))]
        string realm = "global",
        [SlashCommandParameter(Name = "type", Description = "The clan parameter rankings will base on", AutocompleteProviderType = typeof(LeaderboardType))]
        string leaderboardType = "ratings"
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        if (league == 0 && division is 2 or 3)
        {
            await deferredMessage.EditAsync($"❌ Hurricane {Ratings.GetDivision(division)} doesn't exist.");
            return;
        }

        using HttpClient client = new();
        string apiUrl = LadderStructure.GetApiGeneralUrl(league, division, realm);

        try
        {
            var res = await client.GetAsync(apiUrl);

            var structure = JsonSerializer.Deserialize<LadderStructure[]>(await res.Content.ReadAsStringAsync());

            if (structure == null || structure.Length == 0)
            {
                await deferredMessage.EditAsync("❌ No data found for the specified league and division.");
                return;
            }

            if (leaderboardType == "ratings")
            {
                var embed = new EmbedProperties()
                    .WithTitle(
                        $"Leaderboard - {Ratings.GetLeague(league)} {Ratings.GetDivision(division)} ({LadderStructure.ConvertRealm(realm)}) [Ratings]")
                    .WithColor(new Color(Convert.ToInt32(Ratings.GetLeagueColor(league).TrimStart('#'), 16)));

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

            else if (leaderboardType == "success_factor")
            {
                foreach (var clan in structure)
                {
                    clan.SuccessFactor = Math.Round(
                        SuccessFactor.Calculate(clan.PublicRating, clan.BattlesCount, Ratings.GetLeagueExponent(league)),
                        2
                    );
                }

                var embed = new EmbedProperties()
                    .WithTitle(
                        $"Leaderboard - {Ratings.GetLeague(league)} {Ratings.GetDivision(division)} ({LadderStructure.ConvertRealm(realm)}) [S/F]")
                    .WithColor(new Color(Convert.ToInt32(Ratings.GetLeagueColor(league), 16)));

                var fields = new List<EmbedFieldProperties>();

                var sortedStructure = structure.OrderByDescending(s => s.SuccessFactor).ToList();

                for (int i = 0; i < sortedStructure.Count; i++)
                {
                    var clan = sortedStructure[i];
                    var successFactor = clan.SuccessFactor?.ToString(CultureInfo.InvariantCulture);

                    fields.Add(
                        new EmbedFieldProperties()
                            .WithName($"**#{i + 1}** ({LadderStructure.ConvertRealm(clan.Realm)}) `[{clan.Tag}]` ({clan.DivisionRating}) `S/F: {successFactor}` `BTL: {clan.BattlesCount}`")
                    );
                }

                embed.WithFields(fields);

                await deferredMessage.EditAsync(embed);
            }
        }
        catch (Exception ex)
        {
            Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching leaderboard data from API.");
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

internal class LeaderboardType : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var types = new[]
        {
            new ApplicationCommandOptionChoiceProperties("Ratings", "ratings"),
            new ApplicationCommandOptionChoiceProperties("S/F", "success_factor")
        };
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(types);
    }
}