using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Arona.Autocomplete;

internal class ShipAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        HttpClient client = new HttpClient();

        var input = option.Value ?? string.Empty;

        var res = client.GetAsync("https://ntt-community.com/api/ships").Result.Content.ReadAsStringAsync().Result;
        JsonElement doc = JsonDocument.Parse(res).RootElement;

        List<ShipStructure> ships = new List<ShipStructure>();

        foreach (JsonElement ship in doc.EnumerateArray())
        {
            string name = ship.GetProperty("name").GetString()!;

            if (name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
                ships.Add(new ShipStructure(name, ship.GetProperty("_id").ToString()));
        }

        var choices = ships
            .Where(s => s.Name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
            .Take(8)
            .Select(s => new ApplicationCommandOptionChoiceProperties(s.Name, s.Id))
            .ToArray();

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}

internal class ShipStructure(string name, string id)
{
    public string Name = name;
    public string Id = id;
}