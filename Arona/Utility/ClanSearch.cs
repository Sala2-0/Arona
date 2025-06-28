namespace Arona.Utility;
using Config;
using System.Text.Json;
using System.Net.Http;
using NetCord.Services.ApplicationCommands;
using NetCord;
using NetCord.Rest;

public class ClanSearch: IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var config = JsonSerializer.Deserialize<BotConfig>(BotConfig.GetConfigFilePath());
        HttpClient client = new HttpClient();
        
        var input = option.Value ?? string.Empty;
        
        string region = "eu"; // Förvalt region is EU
        if (input.Contains(' ')) // Förväntad exempel: ASIA SALEM
        {
            var split = input.Split(' ');
            region = split[0];
            input = split[1];
            
            // Hantera NA kod
            if (region is "NA" or "na")
                region = "com";
            
            // Gemenera all text
            region = region.ToLower();
        }

        if (String.IsNullOrEmpty(input) || input.Length < 2)
        {
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("Ex: NTT", "500205591|eu"),
                new ApplicationCommandOptionChoiceProperties("Ex: NA RESIN", "1000048416|com"),
            ]);
        }
        
        List<ClanSearchStructure> clans = new List<ClanSearchStructure>();
        
        var res = client.GetAsync($"https://api.worldofwarships.{region}/wows/clans/list/?application_id={config!.WgApi}&search={input}")
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
}