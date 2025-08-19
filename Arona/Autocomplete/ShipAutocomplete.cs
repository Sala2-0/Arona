using System.Globalization;
using System.Text;
using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.ApiModels;

namespace Arona.Autocomplete;

internal class ShipAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        using HttpClient client = new HttpClient();

        string input = option.Value ?? string.Empty;
        input = RemoveSpecialChars(input);

        var res = await client.GetAsync("https://clans.worldofwarships.eu/api/encyclopedia/vehicles_info/");
        Dictionary<long, VehicleInfo> shipMetadatas = JsonSerializer.Deserialize<Dictionary<long, VehicleInfo>>(await res.Content.ReadAsStringAsync())!;
        
        res = await client.GetAsync("https://api.wows-numbers.com/personal/rating/expected/json/");
        JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        
        List<ShipStructure> ships = [];

        foreach (var ship in shipMetadatas)
        {
            string normalizedName = RemoveSpecialChars(ship.Value.Name);
            
            if (!normalizedName.StartsWith(input, StringComparison.InvariantCultureIgnoreCase)) continue;
            if (!doc.TryGetProperty(ship.Key.ToString(), out var stats)) continue;
            if (stats.ValueKind == JsonValueKind.Array) continue;
            if (ship.Value.ShortName.Contains("(old)")) continue;
            
            double avgDmg = stats.GetProperty("average_damage_dealt").GetDouble();
            double avgKills = stats.GetProperty("average_frags").GetDouble();
            double winRate = stats.GetProperty("win_rate").GetDouble();
            
            ships.Add(new ShipStructure(ship.Value.Name, ship.Value.Id.ToString(), avgDmg, avgKills, winRate));
        }
        var choices = ships
            .Take(8)
            .Select(s => new ApplicationCommandOptionChoiceProperties(
                    name: s.Name,
                    stringValue: $"{s.Id}|{s.Name}|{s.AverageDamageDealt}|{s.AverageKills}|{s.WinRate}"
                )
            )
            .ToArray();

        return choices;
    }

    private static string RemoveSpecialChars(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var stringBuilder = new StringBuilder();

        foreach (var c in normalized)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }
        
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }
}

internal class ShipStructure(string name, string id, double avgDmg, double avgKills, double winRate)
{
    public readonly string Name = name;
    public readonly string Id = id;
    public readonly double AverageDamageDealt = avgDmg;
    public readonly double AverageKills = avgKills;
    public readonly double WinRate = winRate;
}