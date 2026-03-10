using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Models.DB;
using Arona.Services;

namespace Arona.Commands;

public class Help(IDatabaseRepositoryService<Guild> repositoryService) : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("help", "Get help with the bot commands")]
    public async Task HelpAsync()
    {
        repositoryService.GetOrCreate(Context.Guild.Id!.ToString());

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message("Commands list can be found at https://github.com/Sala2-0/Arona")
        );
    }
}