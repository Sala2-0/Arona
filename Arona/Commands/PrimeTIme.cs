using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;
using Arona.Autocomplete;
using Arona.Utility;

namespace Arona.Commands;

public class PrimeTime : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("prime_time", "Get a clans active clan battle regions")]
    public async Task PrimeTimeAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for",
            AutocompleteProviderType = typeof(ClanAutocomplete))] string clanIdAndRegion)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        using HttpClient client = new HttpClient();
        
        var split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        try
        {
            var res = await client.GetAsync($"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/");

            Clanbase clan = JsonSerializer.Deserialize<Clanbase>(await res.Content.ReadAsStringAsync())!;

            int? primeTime = clan.ClanView.WowsLadder.PrimeTime;
            int? plannedPrimeTime = clan.ClanView.WowsLadder.PlannedPrimeTime;

            string tag = clan.ClanView.Clan.Tag;
            string name = clan.ClanView.Clan.Name;

            var embed = new EmbedProperties()
                .WithTitle($"`[{tag}] {name}`")
                .WithColor(new Color(Convert.ToInt32("a4fff7", 16)))
                .AddFields(
                    new EmbedFieldProperties()
                        .WithName("Selected region")
                        .WithValue(
                            plannedPrimeTime != null
                                ? GetPrimeTimeRegions(plannedPrimeTime)
                                : "Not selected"
                        )
                        .WithInline(false),
                    new EmbedFieldProperties()
                        .WithName("Active region")
                        .WithValue(
                            primeTime != null
                                ? GetPrimeTimeRegions(primeTime)
                                : "Not playing"
                        )
                );

            await deferredMessage.EditAsync(embed);
        }
        catch (Exception ex)
        {
            Program.Error(ex);
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