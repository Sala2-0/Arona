namespace Arona.Commands;
using System.Text.Json;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Utility;
using System.Diagnostics;

public class ClanMonitor : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("clan_monitor_add", "Add a clan to server database")]
    public async Task ClanMonitorAdd(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to add", AutocompleteProviderType = typeof(ClanSearch))] string clanIdAndRegion)
    {
        // Skicka inledande svar
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.DeferredMessage());
        
        string[] split = clanIdAndRegion.Split('|');
        string region = split[1];
        string clanId = split[0];
        string? guildId = Context.Interaction.GuildId.ToString();

        if (string.IsNullOrEmpty(guildId))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ Error: Guild id is null."));
            return;
        }
        
        ProcessStartInfo psi = JsUtility.StartJs("AddClan.js", clanId + " " + region + " " + guildId);
        
        var process = Process.Start(psi);
        if (await JsUtility.CheckJsErrorAsync(process, InternalCommandErr)) return;

        string output = await process!.StandardOutput.ReadToEndAsync();
        if (await JsUtility.CheckJsErrorAsync(output, InternalCommandErr)) return;
        
        // await Context.Interaction.SendResponseAsync(
        //     InteractionCallback.Message(output));

        await Context.Interaction.ModifyResponseAsync(options => options.Content = output);
    }
    
    [SlashCommand("clan_monitor_remove", "Remove a clan from server database")]
    public async Task ClanMonitorRemove(
        [SlashCommandParameter(Name = "clan_tag", Description = "The clan tag to remove", AutocompleteProviderType = typeof(ClanRemoveSearch))] string clanId)
    {
        if (clanId == "undefined")
        {
            await InternalCommandErr();
            return;
        }
        
        string? guildName = Context.Interaction.Guild?.Name;
        string? guildId = Context.Interaction.GuildId.ToString();
        
        if (string.IsNullOrEmpty(guildId) || string.IsNullOrEmpty(guildName))
        {
            await InternalCommandErr();
            return;
        }
        
        ProcessStartInfo psi = JsUtility.StartJs("RemoveClan.js", clanId + " " + guildId + " " + guildName);
        
        var process = Process.Start(psi);
        if (await JsUtility.CheckJsErrorAsync(process, InternalCommandErr)) return;

        string output = await process!.StandardOutput.ReadToEndAsync();
        if (await JsUtility.CheckJsErrorAsync(output, InternalCommandErr)) return;
        
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(output));
    }

    [SlashCommand("clan_monitor_list", "List all clans in server database")]
    public async Task ClanMonitorList()
    {
        string? guildName = Context.Interaction.Guild?.Name;
        string? guildId = Context.Interaction.GuildId.ToString();
        
        if (string.IsNullOrEmpty(guildId) || string.IsNullOrEmpty(guildName))
        {
            await InternalCommandErr();
            return;
        }
        
        ProcessStartInfo psi = JsUtility.StartJs("ClanList.js", guildId + " " + guildName);
        
        var process = Process.Start(psi);
        if (await JsUtility.CheckJsErrorAsync(process, InternalCommandErr)) return;

        string output = await process!.StandardOutput.ReadToEndAsync();
        if (await JsUtility.CheckJsErrorAsync(output, InternalCommandErr)) return;

        if (output.Contains("C#: No database"))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ No database exists for this server. **Add a clan to initialize one.**"));
            return;
        }

        if (output.Contains("C#: No clans"))
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message($"No clans currently monitored in `{guildName}`"));
            return;
        }
        
        JsonElement doc = JsonDocument.Parse(output).RootElement;
        
        List<string> clans = new List<string>();

        foreach (JsonProperty clan in doc.EnumerateObject())
            clans.Add($"`[{clan.Value.GetProperty("clan_tag").GetString()}] {clan.Value.GetProperty("clan_name").GetString()}` " +
                      $"({ClanSearchStructure.GetRegionCode(clan.Value.GetProperty("region").GetString()!)})");
        
        var field = new List<EmbedFieldProperties>();

        foreach (string clanName in clans)
        {
            field.Add(new EmbedFieldProperties()
                .WithName(clanName)
                .WithInline(false));
        }
        
        var embed = new EmbedProperties()
            .WithTitle($"Clans currently monitored in `{guildName}`")
            .WithFields(field);
        
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(new InteractionMessageProperties().WithEmbeds([ embed ])));
    }

    // Error visningsfunktion när något fel händer internt
    private async Task InternalCommandErr()
    {
        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message("❌ Internal command error"));
    }
}