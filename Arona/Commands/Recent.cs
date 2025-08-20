using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Database;
using Arona.Utility;

namespace Arona.Commands;

public class Recent : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("recent", "Get recent stats")]
    public async Task RecentAsync()
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        string userId = Context.User.Id.ToString();

        var userData = Collections.Users.FindOne(u => u.Id == userId);

        if (userData == null)
        {
            await deferredMessage.EditAsync(
                "You haven't linked your World of Warships account yet." +
                "\nPlease use `/link` so Arona knows your account."
            );
            return;
        }

        // Placeholder for future implementation
        await deferredMessage.EditAsync("This command is not yet implemented.");
    }
}