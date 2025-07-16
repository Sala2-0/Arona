namespace Arona.Commands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

public class Help : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("help", "Get help with the bot commands")]
    public async Task HelpFn()
    {
        string helpMessage = "== Clan Monitor ==\n" +
                             "`/clan_monitor_add` - Add a clan to the server database\n" +
                             "`/clan_monitor_remove` - Remove a clan from the server database\n" +
                             "`/clan_monitor_list` - List of clans currently monitored in server\n" +
                             "\n== Other ==\n" +
                             "`/prime_time` - Get a clans clan battle activity of today\n" +
                             "`/pr_calculator` - Calculate PR of any ship\n" +
                             "`/ratings` - Get detailed information of a clan's ratings\n" +
                             "`/help` - Get info for commands available\n" +
                             "`/leaderboard` - Leaderboard for latest clan battles season\n" +
                             "\nCheck out source code at https://github.com/Sala2-0/Arona";


        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(helpMessage));
    }
}