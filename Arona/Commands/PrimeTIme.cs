using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;
using Arona.Autocomplete;
using Arona.Database;
using Arona.Models;

namespace Arona.Commands;

public class PrimeTime : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("prime_time", "Get a clans active clan battle regions")]
    public async Task PrimeTimeAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for", AutocompleteProviderType = typeof(ClanAutocomplete))]
        string clanIdAndRegion
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        Guild.Exists(Context.Interaction);

        var split = clanIdAndRegion.Split('|');
        string region = split[1];
        int clanId = int.Parse(split[0]);

        try
        {
            var clan = await ClanBase.GetAsync(clanId, region);

            int? primeTime = clan.WowsLadder.PrimeTime;
            int? plannedPrimeTime = clan.WowsLadder.PlannedPrimeTime;

            await deferredMessage.EditAsync(new EmbedProperties
            {
                Title = $"`[{clan.Clan.Tag}] {clan.Clan.Name}`",
                Color = new Color(Convert.ToInt32("a4fff7", 16)),
                Fields = [
                    new EmbedFieldProperties{ Name = "Selected region", Value = plannedPrimeTime != null ? GetPrimeTimeRegions(plannedPrimeTime) : "Not selected" },
                    new EmbedFieldProperties{ Name = "Active region", Value = primeTime != null ? GetPrimeTimeRegions(primeTime) : "Not playing" }
                ]
            });
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("❌ Error fetching clan data from API.");
        }
    }

    private static string GetPrimeTimeRegions(int? primeTimeId) => primeTimeId switch
    {
        0 or 1 or 2 or 3 => "ASIA",
        4 or 5 or 6 or 7 => "EU",
        8 or 9 or 10 or 11 => "NA",
        _ => "undefined",
    };
}