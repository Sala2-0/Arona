using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Models.DB;
using Arona.Utility;

namespace Arona.Commands.Autocomplete;

internal class ShipAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public async ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        string input = option.Value ?? string.Empty;
        input = Text.Normalize(input);

        var cachedShips = Collections.Ships.FindAll();
        
        var res = await ApiClient.Instance.GetAsync("https://api.wows-numbers.com/personal/rating/expected/json/");
        JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");
        
        List<ShipStructure> ships = [];

        foreach (var ship in cachedShips)
        {
            string normalizedName = Text.Normalize(ship.Name);

            if (!normalizedName.Contains(input, StringComparison.InvariantCultureIgnoreCase)) continue;
            if (!doc.TryGetProperty(ship.Id.ToString(), out var stats)) continue;
            if (stats.ValueKind == JsonValueKind.Array) continue;
            if (ship.ShortName.Contains("(old)")) continue;
            
            double avgDmg = stats.GetProperty("average_damage_dealt").GetDouble(),
                avgKills = stats.GetProperty("average_frags").GetDouble(),
                winRate = stats.GetProperty("win_rate").GetDouble();
            
            ships.Add(new ShipStructure(ship.Name, ship.Id.ToString(), ship.Tier, avgDmg, avgKills, winRate));
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

internal class ShipStructure(string name, string id, Text.Tier tier, double avgDmg, double avgKills, double winRate)
{
    public readonly string Name = name;
    public readonly string Id = id;
    public readonly Text.Tier Tier = tier;
    public readonly double AverageDamageDealt = avgDmg;
    public readonly double AverageKills = avgKills;
    public readonly double WinRate = winRate;
}