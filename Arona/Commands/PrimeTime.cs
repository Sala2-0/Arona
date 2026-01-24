using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Services.Message;
using Arona.Models.DB;
using Arona.Models.Api.Clans;

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
            var clan = await ClanViewQuery.GetSingleAsync(new ClanViewRequest(region, clanId));

            int? primeTime = clan.ClanView.WowsLadder.PrimeTime,
                plannedPrimeTime = clan.ClanView.WowsLadder.PlannedPrimeTime;

            await deferredMessage.EditAsync(new EmbedProperties
            {
                Title = $"`[{clan.ClanView.Clan.Tag}] {clan.ClanView.Clan.Name}`",
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