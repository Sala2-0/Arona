using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Utility;
using Arona.Models.Api.Official;
using Arona.Services;

namespace Arona.Commands.Autocomplete;

internal class ClanAutocomplete(ErrorService errorService, IApiService apiService): IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
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

        if (string.IsNullOrEmpty(input) || input.Length < 2 || !IsValidRegion(region))
        {
            return [
                new ApplicationCommandOptionChoiceProperties("Ex: NTT", "500205591,eu"),
                new ApplicationCommandOptionChoiceProperties("Ex: NA RESIN", "1000048416,com"),
                new ApplicationCommandOptionChoiceProperties("Ex: EU SEIA", "500256050,eu")
            ];
        }

        try
        {
            var data = await new ClanListItemQuery(apiService.HttpClient).GetAsync(new ClanListItemRequest(region, input));
            var clans = data.Data.Select(c => new { Tag = c.Tag, Name = c.Name, Id = c.ClanId, Region = region }).ToList();

            var choices = clans
                .Take(8)
                .Select(s =>
                    new ApplicationCommandOptionChoiceProperties(
                        $"[{s.Tag}] {s.Name} ({ClanUtils.GetHumanRegion(s.Region)})", $"{s.Id},{s.Region}"));

            return choices;
        }
        catch (Exception ex)
        {
            await errorService.PrintErrorAsync(ex, $"Error in {nameof(ClanAutocomplete)}");
            return [];
        }
    }

    private static bool IsValidRegion(string region) => 
        region is "eu" or "EU" or "asia" or "ASIA" or "com";
}