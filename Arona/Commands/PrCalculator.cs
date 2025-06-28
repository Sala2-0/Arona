namespace Arona.Commands;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Utility;

public class ShipStructure
{
    public string Name;
    public string Id;

    public ShipStructure(string name, string id)
    {
        Name = name;
        Id = id;
    }
}

public class Victory : IChoicesProvider<ApplicationCommandContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(SlashCommandParameter<ApplicationCommandContext> param)
    {
        var choices = new[]
        {
            new ApplicationCommandOptionChoiceProperties("True", "true"),
            new ApplicationCommandOptionChoiceProperties("False", "false")
        };
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}

public class ShipSearch : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        HttpClient client = new HttpClient();
        
        var input = option.Value ?? string.Empty;
        
        var res = client.GetAsync("https://ntt-community.com/api/ships").Result.Content.ReadAsStringAsync().Result;
        JsonElement doc = JsonDocument.Parse(res).RootElement.GetProperty("data");
        
        List<ShipStructure> ships = new List<ShipStructure>();

        foreach (JsonProperty ship in doc.EnumerateObject())
        {
            string name = ship.Value.GetProperty("name").GetString()!;
            
            if (name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
            {
                ships.Add(new ShipStructure(name, ship.Name));
            }
        }
        
        var choices = ships
            .Where(s => s.Name.StartsWith(input, StringComparison.InvariantCultureIgnoreCase))
            .Take(8)
            .Select(s => new ApplicationCommandOptionChoiceProperties(s.Name, s.Id))
            .ToArray();
        
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}

public class PrCalculator : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("pr_calculator", "Calculate PR of any ship")]
    public async Task PrCalculatorFn(
        [SlashCommandParameter(Name = "ship", Description = "The ship to calculate PR for", AutocompleteProviderType = typeof(ShipSearch))] string selectedShipId,
        [SlashCommandParameter(Name = "damage", Description = "Damage dealt")] int damage,
        [SlashCommandParameter(Name = "kills", Description = "Number of kills", MinValue = 0, MaxValue = 12)] int kills,
        [SlashCommandParameter(Name = "victory", Description = "Win or loss", ChoicesProviderType = typeof(Victory))] string victory
        )
    {
        HttpClient client = new HttpClient();
        var res = client.GetAsync("https://ntt-community.com/api/ships").Result.Content.ReadAsStringAsync().Result;
        JsonElement doc = JsonDocument.Parse(res).RootElement.GetProperty("data");

        if (!doc.TryGetProperty(selectedShipId, out JsonElement _))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ Ship database error: Ship id does not exist.")
            );
            return;
        }
        
        string name = doc.GetProperty(selectedShipId).GetProperty("name").GetString()!;
        var stats = doc.GetProperty(selectedShipId).GetProperty("stats");
        
        var normalization = new
        {
            damage = Math.Max(0, (damage / stats.GetProperty("average_damage_dealt").GetDouble()) - 0.4) / (1 - 0.4),
            kills = Math.Max(0, (kills / stats.GetProperty("average_kills").GetDouble()) - 0.1) / (1 - 0.1),
            win = Math.Max(0, ((victory == "true" ? 1 : 0) / (stats.GetProperty("win_rate").GetDouble() / 100)) - 0.7) / (1 - 0.7)
        };
                
        int pr = (int)((700 * normalization.damage) + (300 * normalization.kills) + (150 * normalization.win));

        var embed = new EmbedProperties()
            .WithTitle(name)
            .WithColor(new Color(Convert.ToInt32(PersonalRatingColors.GetColor(pr), 16)))
            .AddFields(
                new EmbedFieldProperties()
                    .WithName("PR")
                    .WithValue($"**{pr.ToString()}**")
                    .WithInline(false),
                new EmbedFieldProperties()
                    .WithName("Damage")
                    .WithValue(damage.ToString())
                    .WithInline(), // True, WithInline() förvald parameter är true
                new EmbedFieldProperties()
                    .WithName("Kills")
                    .WithValue(kills.ToString())
                    .WithInline(),
                new EmbedFieldProperties()
                    .WithName("Victory")
                    .WithValue(victory)
                    .WithInline()
            );
                
        var props = new InteractionMessageProperties()
            .WithEmbeds([ embed ]);

        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(props));
    }
}