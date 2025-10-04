using System.Globalization;
using System.Text;
using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Database;

namespace Arona.Autocomplete;

internal class ShipAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        using HttpClient client = new();

        string input = option.Value ?? string.Empty;
        input = RemoveSpecialChars(input);

        var cachedShips = Collections.Ships.FindAll();
        
        var res = await client.GetAsync("https://api.wows-numbers.com/personal/rating/expected/json/");
        JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        
        List<ShipStructure> ships = [];

        foreach (var ship in cachedShips)
        {
            string normalizedName = RemoveSpecialChars(ship.Name);
            
            if (!normalizedName.Contains(input, StringComparison.InvariantCultureIgnoreCase)) continue;
            if (!doc.TryGetProperty(ship.Id.ToString(), out var stats)) continue;
            if (stats.ValueKind == JsonValueKind.Array) continue;
            if (ship.ShortName.Contains("(old)")) continue;
            
            double avgDmg = stats.GetProperty("average_damage_dealt").GetDouble();
            double avgKills = stats.GetProperty("average_frags").GetDouble();
            double winRate = stats.GetProperty("win_rate").GetDouble();
            
            ships.Add(new ShipStructure(ship.Name, ship.Id.ToString(), GetRomanTier(ship.Tier), avgDmg, avgKills, winRate));
        }
        var choices = ships
            .Take(8)
            .Select(s => new ApplicationCommandOptionChoiceProperties(
                    name: $"{s.Tier} {s.Name}",
                    stringValue: $"{s.Id}|{s.Name}|{s.Tier}|{s.AverageDamageDealt}|{s.AverageKills}|{s.WinRate}"
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

    private static string GetRomanTier(int tier) => tier switch
    {
        1 => "I",
        2 => "II",
        3 => "III",
        4 => "IV",
        5 => "V",
        6 => "VI",
        7 => "VII",
        8 => "VIII",
        9 => "IX",
        10 => "X",
        11 => "XI",
        _ => "undefined"
    };
}

internal class ShipStructure(string name, string id, string tier, double avgDmg, double avgKills, double winRate)
{
    public readonly string Name = name;
    public readonly string Id = id;
    public readonly string Tier = tier;
    public readonly double AverageDamageDealt = avgDmg;
    public readonly double AverageKills = avgKills;
    public readonly double WinRate = winRate;
}