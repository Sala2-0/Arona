using NetCord.Services.ApplicationCommands;
using Arona.Autocomplete;
using Arona.Database;
using Arona.Utility;

namespace Arona.Commands;

public class Link : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("link", "Link your Discord account to your WoWS account")]
    public async Task LinkAsync(
        [SlashCommandParameter(Name = "username", Description = "World of Warships username", AutocompleteProviderType = typeof(PlayerAutocomplete))]
        string playerInfo
    )
    {
        var deferredMessage = new DeferredMessage{ Interaction = Context.Interaction };
        
        await deferredMessage.SendAsync();
        
        var split = playerInfo.Split('|');
        int playerId = int.Parse(split[0]);
        string region = split[1];
        string username = split[2];
        
        string userId = Context.Interaction.User.Id.ToString();
        
        var user = Collections.Users.FindOne(u => u.Id == userId);

        if (user != null)
        {
            user.AccountId = playerId;
            user.AccountRegion = region;

            Collections.Users.Update(user);
        }
        else
        {
            Collections.Users.Insert(new User
            {
                Id = userId,
                AccountId = playerId,
                AccountRegion = region,
            });
        }

        await deferredMessage.EditAsync($"✅ Linked with player `{username}` ({region.ToUpper()})");
    }
}