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
        [SlashCommandParameter(Name = "ship", Description = "The ship to calculate PR for", AutocompleteProviderType = typeof(ShipAutocomplete))] string shipData,
        [SlashCommandParameter(Name = "damage", Description = "Damage dealt")] int damage,
        [SlashCommandParameter(Name = "kills", Description = "Number of kills", MinValue = 0, MaxValue = 12)] int kills,
        [SlashCommandParameter(Name = "outcome", Description = "Win or loss")] GameOutcome outcome
    )
    {
        string[] split = shipData.Split('|');
        
        string id = split[0];
        string name = split[1];
        string tier = split[2];
        double avgDamage = double.Parse(split[3]);
        double avgKills = double.Parse(split[4]);
        double winRate = double.Parse(split[5]);
        
        using var client = new HttpClient();
        var res = await client.GetAsync($"https://api.worldofwarships.eu/wows/encyclopedia/ships/?application_id={Config.WgApi}&ship_id={id}");
        JsonElement data = JsonDocument.Parse(await res.Content.ReadAsStringAsync())
            .RootElement
            .GetProperty("data")
            .GetProperty(id);

        string imageUrl = data.GetProperty("images").GetProperty("small").GetString()!;
        
        var normalization = new
        {
            damage = Math.Max(0, damage / avgDamage - 0.4) / (1 - 0.4),
            kills = Math.Max(0, kills / avgKills - 0.1) / (1 - 0.1),
            win = Math.Max(0, (outcome == GameOutcome.Victory ? 1 : 0) / (winRate / 100) - 0.7) / (1 - 0.7)
        };
                
        int pr = (int)Math.Round((700 * normalization.damage) + (300 * normalization.kills) + (150 * normalization.win));

        var embed = new EmbedProperties()
            .WithTitle($"{tier} {name}")
            .WithColor(new Color(Convert.ToInt32(PersonalRatingColors.GetColor(pr), 16)))
            .AddFields(
                new EmbedFieldProperties()
                    .WithName("PR")
                    .WithValue($"**{pr}**")
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
                    .WithName("Outcome")
                    .WithValue(outcome.ToString())
                    .WithInline()
            )
            .WithThumbnail(new EmbedThumbnailProperties(imageUrl));
                
        var props = new InteractionMessageProperties()
            .WithEmbeds([ embed ]);

        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(props));
    }
    
    public enum GameOutcome
    {
        Victory,
        Defeat
    }
}