using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Database;
using Arona.Utility;

namespace Arona.Autocomplete;

internal class ShipAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        using HttpClient client = new();

        string input = option.Value ?? string.Empty;
        input = Text.Normalize(input);

        var cachedShips = Collections.Ships.FindAll();
        
        var res = await client.GetAsync("https://api.wows-numbers.com/personal/rating/expected/json/");
        JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        
        List<ShipStructure> ships = [];

        foreach (var ship in cachedShips)
        {
            string normalizedName = Text.Normalize(ship.Name);
            
            if (!normalizedName.Contains(input, StringComparison.InvariantCultureIgnoreCase)) continue;
            if (!doc.TryGetProperty(ship.Id.ToString(), out var stats)) continue;
            if (stats.ValueKind == JsonValueKind.Array) continue;
            if (ship.ShortName.Contains("(old)")) continue;
            
            double avgDmg = stats.GetProperty("average_damage_dealt").GetDouble();
            double avgKills = stats.GetProperty("average_frags").GetDouble();
            double winRate = stats.GetProperty("win_rate").GetDouble();
            
            ships.Add(new ShipStructure(ship.Name, ship.Id.ToString(), Text.GetRomanTier(ship.Tier), avgDmg, avgKills, winRate));
        }
        var choices = ships
            .Take(8)
            .Select(s => new ApplicationCommandOptionChoiceProperties(
                    name: $"{s.Tier} {s.Name}",
                    stringValue: $"{s.Id},{s.Name},{s.Tier},{s.AverageDamageDealt},{s.AverageKills},{s.WinRate}"
                )
            )
            .ToArray();

        return choices;
    }
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