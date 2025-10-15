using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Autocomplete;
using Arona.Models;

namespace Arona.Commands;

public class BaseExp : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("bxp_add", "Add Base XP data of a ship from a game (use to help with a mod)")]
    public async Task BaseExpAddAsync(
        [SlashCommandParameter(Name = "ship", AutocompleteProviderType = typeof(ShipAutocomplete))]
        string shipData,

        [SlashCommandParameter(Name = "base_xp", Description = "Base XP amount")]
        int baseExp
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var user = deferredMessage.Interaction.User.Username;
        var userId = deferredMessage.Interaction.User.Id;

        var split = shipData.Split(',');
        long shipId = long.Parse(split[0]);
        string shipName = split[1];

        using var client = new HttpClient();

        try
        {
            // Servern/databasen finns på samma IP
            var res = await client.GetAsync($"http://localhost:4000/add?shipId={shipId}&baseExp={baseExp}");
            res.EnsureSuccessStatusCode();

            await Program.Client!.Rest.SendMessageAsync(
                channelId: Config.BackdoorChannel, 
                message: new MessageProperties()
                    .WithContent($"`{user} ({userId})` used `/bxp_add`: **{shipName}** - **{baseExp}** BXP")
            );
        }
        catch (Exception ex)
        {
            await deferredMessage.EditAsync("There was an error saving the data");
            return;
        }

        await deferredMessage.EditAsync("Saved your data. Thank youu for helping out!!");
    }
}