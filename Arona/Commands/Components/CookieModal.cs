using System.Security.Authentication;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Arona.Models.Api.Clans;
using Arona.Models.DB;

using Role = Arona.Models.Role;

namespace Arona.Commands.Components;

public class Modals : ComponentInteractionModule<ModalInteractionContext>
{
    [ComponentInteraction("cookie form")]
    public async Task CookieFormAsync(long accountId, string region, int clanId)
    {
        var guild = Guild.Find(Context.Interaction);

        var cookieInput = Context.Components
            .OfType<TextInput>()
            .FirstOrDefault(input => input.CustomId == "cookie");

        try
        {
            var data = await AccountInfoSync.GetAsync(cookieInput!.Value, region);

            if (data.AccountId != accountId)
                throw new InvalidCredentialException("Cookie does not belong to specified account.");
            if (data.ClanId != clanId)
                throw new InvalidCredentialException("Player is not a member of specified clan.");
            if (data.Rank < Role.LineOfficer)
                throw new InvalidCredentialException("Player is too high ranking.");

            guild.Cookies[clanId] = cookieInput.Value;
            Collections.Guilds.Update(guild);
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message($"Cookies set for clan `{clanId}`"));
        }
        catch (Exception ex)
        {
            await Program.LogError(ex);
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message($"LogError >_<\n\n`{ex.Message}`"));
        }
        finally
        {
            Program.ActiveWrites.Remove(guild.Id);
        }
    }
}