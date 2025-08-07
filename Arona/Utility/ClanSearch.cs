using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Arona.Utility;

internal class ClanSearch: IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        HttpClient client = new HttpClient();
        
        var input = option.Value ?? string.Empty;
        
        string region = "eu"; // Förvalt region is EU
        if (input.Contains(' ')) // Förväntad exempel: ASIA SALEM
        {
            var split = input.Split(' ');
            region = split[0];
            input = split[1];
            
            if (region is "NA" or "na")
                region = "com";
            
            region = region.ToLower();
        }

        if (String.IsNullOrEmpty(input) || input.Length < 2 || !ValidRegion(region))
        {
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("Ex: NTT", "500205591|eu"),
                new ApplicationCommandOptionChoiceProperties("Ex: NA RESIN", "1000048416|com"),
            ]);
        }

        List<ClanSearchStructure> clans = [];

        var res = client.GetAsync($"https://api.worldofwarships.{region}/wows/clans/list/?application_id={Program.Config!.WgApi}&search={input}")
            .Result.Content.ReadAsStringAsync().Result;
        JsonElement doc = JsonDocument.Parse(res).RootElement.GetProperty("data");

        foreach (JsonElement clan in doc.EnumerateArray())
        {
            // Console.WriteLine(clan); // Debug
            clans.Add(new ClanSearchStructure(clan.GetProperty("tag").GetString()!, clan.GetProperty("name").GetString()!, clan.GetProperty("clan_id").GetInt32().ToString(), region));
        }

        var choices = clans
            .Take(8)
            .Select(s =>
                new ApplicationCommandOptionChoiceProperties($"[{s.ClanTag}] {s.ClanName} ({ClanSearchStructure.GetRegionCode(s.Region)})", $"{s.ClanId}|{s.Region}"));
        
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }

    public static bool ValidRegion(string region) => 
        region is "eu" or "EU" or "asia" or "ASIA" or "com";
}