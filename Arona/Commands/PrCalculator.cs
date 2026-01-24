using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Services.Message;
using Arona.Models.DB;
using Arona.Utility;

namespace Arona.Commands;

[SlashCommand("pr_calculator", "Calculate PR")]
public class PrCalculator : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("single", "PR of one ship for 1 game")]
    public async Task SingleAsync(
        [SlashCommandParameter(Name = "ship", Description = "The ship to calculate PR for", AutocompleteProviderType = typeof(ShipAutocomplete))]
        string shipData,

        [SlashCommandParameter(Name = "damage", Description = "Damage dealt")]
        int damage,

        [SlashCommandParameter(Name = "kills", Description = "Number of kills", MinValue = 0, MaxValue = 12)]
        int kills,

        [SlashCommandParameter(Name = "outcome", Description = "Win or loss")]
        GameOutcome outcome
    )
    {
        Guild.Exists(Context.Interaction);

        string[] split = shipData.Split(',');
        
        string id = split[0],
            name = split[1],
            tier = split[2];
        double avgDamage = double.Parse(split[3]),
            avgKills = double.Parse(split[4]),
            winRate = double.Parse(split[5]);
        
        var res = await ApiClient.Instance.GetAsync($"https://api.worldofwarships.eu/wows/encyclopedia/ships/?application_id={Config.WgApi}&ship_id={id}");
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

        await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties{ Embeds = [ new EmbedProperties
        {
            Title = $"{tier} {name}",
            Color = new Color(Convert.ToInt32(PersonalRatingColors.GetColor(pr), 16)),
            Thumbnail = imageUrl,
            Description = $"**PR:** {pr}\n\n" +
                          $"**Damage:** {damage}\n" +
                          $"**Kills:** {kills}\n" +
                          $"**Outcome:** {outcome}"
        }] }));
    }

    [SubSlashCommand("session", "Detailed session PR. Input guide is in github repository (/help)")]
    public async Task SessionAsync(
        [SlashCommandParameter(Name = "input_str", Description = "Input string of your session. Full guide on github repository")]
        string sessionStr
    )
    {
        var deferredMessage = new DeferredMessage{ Interaction = Context.Interaction};
        await deferredMessage.SendAsync();

        Guild.Exists(Context.Interaction);

        string[] sessionData = sessionStr.Split('_');

        List<EmbedFieldProperties> games = [];
        int totalPr = 0;

        try
        {
            if (sessionData.Length == 0) throw new InvalidDataException("No game data provided.");

            using HttpClient client = new();
            var res = await client.GetAsync("https://api.wows-numbers.com/personal/rating/expected/json/");
            JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            List<Ship> allShips = Collections.Ships.FindAll().ToList();

            if (allShips.Count == 0)
                throw new ApplicationException("Ship cache is empty, contact developer for update.");

            foreach (var game in sessionData)
            {
                string[] gameData = game.Split(',');
                if (gameData.Length != 4) throw new InvalidDataException("Each game data must contain exactly 4 values separated by commas and each game must be separated by underscores");

                string name = Text.Normalize(gameData[0]).ToLower();
                int damage = int.Parse(gameData[1]),
                    kills = int.Parse(gameData[2]);
                GameOutcome outcome = gameData[3].ToLower() switch
                {
                    "win" => GameOutcome.Victory,
                    "loss" => GameOutcome.Defeat,
                    _ => throw new InvalidDataException("Outcome must be either 'win' or 'loss'.")
                };

                Ship? targetShip = allShips
                    .OrderByDescending(ship =>
                    {
                        string normalized = Text.Normalize(ship.Name).ToLower();

                        if (normalized == name) return 2;
                        if (normalized.Contains(name)) return 1;

                        return 0;
                    })
                    .FirstOrDefault(ship =>
                    {
                        string normalized = Text.Normalize(ship.Name).ToLower();
                        return normalized.Contains(name);
                    });

                if (targetShip == null)
                    throw new InvalidDataException($"No ship with name '{name}' found");

                if (!doc.TryGetProperty(targetShip.Id.ToString(), out var stats) || stats.ValueKind == JsonValueKind.Array)
                    throw new InvalidDataException($"No data for ship {name} [{targetShip.Id}] exists.");

                double avgDmg = stats.GetProperty("average_damage_dealt").GetDouble(),
                    avgKills = stats.GetProperty("average_frags").GetDouble(),
                    winRate = stats.GetProperty("win_rate").GetDouble();

                Normalization normalization = new()
                {
                    Damage = Math.Max(0, damage / avgDmg - 0.4) / (1 - 0.4),
                    Kills = Math.Max(0, kills / avgKills - 0.1) / (1 - 0.1),
                    Win = Math.Max(0, (outcome == GameOutcome.Victory ? 1 : 0) / (winRate / 100) - 0.7) / (1 - 0.7)
                };

                int pr = (int)Math.Round((700 * normalization.Damage) + (300 * normalization.Kills) + (150 * normalization.Win));
                totalPr += pr;

                games.Add(new EmbedFieldProperties()
                    .WithName($"{targetShip.Tier} {targetShip.Name}")
                    .WithValue($"{pr.ToString()} PR")
                    .WithInline(false)
                );
            }

            int averagePr = totalPr / games.Count;

            var embed = new EmbedProperties()
                .WithTitle("Session")
                .WithDescription($"Average PR - {averagePr}")
                .WithColor(new Color(Convert.ToInt32(PersonalRatingColors.GetColor(averagePr), 16)))
                .WithFields(games);

            await deferredMessage.EditAsync(embed);
        }
        catch(ApplicationException appEx)
        {
            await Program.Error(appEx);
            await deferredMessage.EditAsync($"Application error.\n\n`{appEx.Message}`");
        }
        catch (InvalidDataException invalidEx)
        {
            await Program.Error(invalidEx);
            await deferredMessage.EditAsync($"Invalid input format.\n\n`{invalidEx.Message}`");
        }
        catch (Exception ex)
        {
            await Program.Error(ex);
            await deferredMessage.EditAsync("API Error! >_<");
        }
    }
    
    public enum GameOutcome
    {
        Victory,
        Defeat
    }

    private struct Normalization
    {
        public required double Damage;
        public required double Kills;
        public required double Win;
    }
}