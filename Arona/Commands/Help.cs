using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Models.DB;

namespace Arona.Commands;

public class Help : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("help", "Get help with the bot commands")]
    public async Task HelpAsync()
    {
        Guild.Exists(Context.Interaction);

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message("Commands list can be found at https://github.com/Sala2-0/Arona")
        );
    }
}