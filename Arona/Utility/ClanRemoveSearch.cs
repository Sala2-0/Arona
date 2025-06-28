namespace Arona.Utility;
using System.Text.Json;
using NetCord.Services.ApplicationCommands;
using NetCord;
using NetCord.Rest;
using System.Diagnostics;

public class ClanRemoveSearch : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        string? guildName = context.Interaction.Guild?.Name;
        string? guildId = context.Interaction.GuildId.ToString();
        
        ProcessStartInfo psi = JsUtility.StartJs("ClanList.js", guildId + " " + guildName);
        
        using var process = Process.Start(psi);
        string? output = process?.StandardOutput.ReadToEnd();
        
        if (output == null)
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("Internal command error", "undefined")
            ]);
        
        // No clans in database
        if (output.Contains("C#: No clans"))
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("No clans in database", "undefined")
            ]);
        
        // No database for guild
        if (output.Contains("C#: No database"))
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("No database exists for this server. Add a clan to initialize one.", "undefined")
            ]);
        
        JsonElement doc = JsonDocument.Parse(output).RootElement;
        
        List<ClanSearchStructure> clans = new List<ClanSearchStructure>();

        foreach (JsonProperty clan in doc.EnumerateObject())
            clans.Add(new ClanSearchStructure(clan.Value.GetProperty("clan_tag").ToString(),
                clan.Value.GetProperty("clan_name").GetString()!, clan.Name,
                clan.Value.GetProperty("region").GetString()!));

        var choices = clans
            .Take(10)
            .Select(s =>
                new ApplicationCommandOptionChoiceProperties(
                    $"[{s.ClanTag}] {s.ClanName} ({ClanSearchStructure.GetRegionCode(s.Region)})", $"{s.ClanId}"));
        
        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}