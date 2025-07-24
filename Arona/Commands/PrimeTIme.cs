namespace Arona.Commands;
using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord;
using Utility;

public class PrimeTime : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("prime_time", "Get a clans active clan battle regions")]
    public async Task PrimeTimeAsync(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag search for",
            AutocompleteProviderType = typeof(ClanSearch))] string clanIdAndRegion)
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());

        HttpClient client = new HttpClient();
        
        var split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];

        var res = client.GetAsync($"https://clans.worldofwarships.{region}/api/clanbase/{clanId}/claninfo/")
            .Result.Content.ReadAsStringAsync().Result;
        JsonElement doc = JsonDocument.Parse(res).RootElement.GetProperty("clanview").GetProperty("wows_ladder");
        JsonElement clanDoc = JsonDocument.Parse(res).RootElement.GetProperty("clanview").GetProperty("clan");

        int? primeTime = doc.GetProperty("prime_time").ValueKind != JsonValueKind.Null ? doc.GetProperty("prime_time").GetInt16() : null;
        int? plannedPrimeTime = doc.GetProperty("planned_prime_time").ValueKind != JsonValueKind.Null ? doc.GetProperty("planned_prime_time").GetInt16() : null;

        var embed = new EmbedProperties()
            .WithTitle($"`[{clanDoc.GetProperty("tag")}] {clanDoc.GetProperty("name")}`")
            .WithColor(new Color(Convert.ToInt32("a4fff7", 16)))
            .AddFields(
                new EmbedFieldProperties()
                    .WithName("Selected region")
                    .WithValue(plannedPrimeTime != null ? GetPrimeTimeRegions(plannedPrimeTime) : "Not selected")
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Active region")
                    .WithValue(primeTime != null ? GetPrimeTimeRegions(primeTime) : "Not playing")
            );
        
        //var props = new InteractionMessageProperties()
        //    .WithEmbeds([ embed ]);
        
        //await Context.Interaction.SendResponseAsync(
        //    InteractionCallback.Message(props)
        //);

        await Context.Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
    }

    private static string GetPrimeTimeRegions(int? primeTimeId) => primeTimeId switch
    {
        0 or 1 or 2 or 3 => "ASIA",
        4 or 5 or 6 or 7 => "EU",
        8 or 9 or 10 or 11 => "NA",
        _ => "undefined",
    };
}