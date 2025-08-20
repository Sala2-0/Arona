using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;

namespace Arona.Autocomplete;

public class PlayerAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context
    )
    {
        HttpClient client = new();

        string input = option.Value ?? string.Empty;

        string region = "eu";
        if (input.Contains(' '))
        {
            var split = input.Split(' ');
            region = split[0];
            input = split[1];
            
            if (region is "NA" or "na")
                region = "com";
            
            region = region.ToLower();
        }
        
        if (string.IsNullOrEmpty(input))
            return null;
        
        IEnumerable<ApplicationCommandOptionChoiceProperties>? choices = null;

        try
        {
            var res = await client.GetAsync($"https://api.worldofwarships.{region}/wows/account/list/?application_id={Config.WgApi}&search={input}");
            var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
            var dataElement = doc.RootElement.GetProperty("data");
            
            var players = JsonSerializer.Deserialize<PlayerList[]>(dataElement.GetRawText())!;

            choices = players
                .Take(5)
                .Select(s =>
                    new ApplicationCommandOptionChoiceProperties(
                        name: $"({region.ToUpper()}) {s.Nickname}",
                        stringValue: $"{s.AccountId}|{region}|{s.Nickname}"
                    )
                );
        }
        catch (Exception ex)
        {
            Program.Error(ex);
        }
        
        return choices;
    }
}