using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Autocomplete;
using Arona.Utility;

namespace Arona.Commands;

public class PrCalculator : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("pr_calculator", "Calculate PR of any ship")]
    public async Task PrCalculatorAsync(
        [SlashCommandParameter(Name = "ship", Description = "The ship to calculate PR for", AutocompleteProviderType = typeof(ShipAutocomplete))] string selectedShipId,
        [SlashCommandParameter(Name = "damage", Description = "Damage dealt")] int damage,
        [SlashCommandParameter(Name = "kills", Description = "Number of kills", MinValue = 0, MaxValue = 12)] int kills,
        [SlashCommandParameter(Name = "victory", Description = "Win or loss", ChoicesProviderType = typeof(Victory))] string victory
        )
    {
        HttpClient client = new HttpClient();
        var res = client.GetAsync("https://ntt-community.com/api/ships").Result.Content.ReadAsStringAsync().Result;
        JsonElement doc = JsonDocument.Parse(res).RootElement;
        JsonElement? targetShip = null;

        foreach (JsonElement ship in doc.EnumerateArray())
        {
            if (ship.GetProperty("_id").ToString() != selectedShipId) continue;

            targetShip = ship;
            break;
        }

        if (targetShip == null)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ Error: Ship not found."));
            return;
        }

        string name = targetShip.Value.GetProperty("name").GetString()!;
        var stats = new
        {
            AverageDamageDealt = targetShip.Value.GetProperty("average_damage").GetDouble(),
            AverageKills = targetShip.Value.GetProperty("average_kills").GetDouble(),
            WinRate = targetShip.Value.GetProperty("win_rate").GetDouble()
        };
        
        var normalization = new
        {
            damage = Math.Max(0, damage / stats.AverageDamageDealt - 0.4) / (1 - 0.4),
            kills = Math.Max(0, kills / stats.AverageKills - 0.1) / (1 - 0.1),
            win = Math.Max(0, (victory == "true" ? 1 : 0) / (stats.WinRate / 100) - 0.7) / (1 - 0.7)
        };
                
        int pr = (int)Math.Round((700 * normalization.damage) + (300 * normalization.kills) + (150 * normalization.win));

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

internal class Victory : IChoicesProvider<ApplicationCommandContext>
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