using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Database;
using Arona.Utility;
using Account = Arona.Database.Account;

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

        var userAccount = Collections.Accounts.FindOne(a => a.Id == userData.AccountId);

        if (userAccount == null)
        {
            await deferredMessage.EditAsync(
                "Account data not available."
            );

            Console.WriteLine("Trying to create account...");

            await Account.CreateAsync(userData.AccountId, userData.AccountRegion);

            return;
        }

        // Placeholder for future implementation
        await deferredMessage.EditAsync("This command is not yet implemented.");
    }
}