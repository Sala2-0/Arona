namespace Arona.Utility;
using NetCord.Services.ApplicationCommands;
using NetCord;
using NetCord.Rest;
using Database;
using MongoDB.Driver;

internal class ClanRemoveSearch : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        string? guildName = context.Interaction.Guild?.Name;
        string? guildId = context.Interaction.GuildId.ToString();

        var collection = Program.DatabaseClient!.GetDatabase("Arona")
            .GetCollection<Guild>("servers");

        var guild = collection.Find(g => g.Id == guildId).FirstOrDefault();

        if (guild == null)
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("No database exists for this server. Add a clan to initialize one.", "undefined")
            ]);

        if (guild.Clans.Count == 0)
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("No clans in database", "undefined")
            ]);

        var choices = guild.Clans
            .Take(5)
            .Select(clan =>
                new ApplicationCommandOptionChoiceProperties(
                    name: $"[{clan.Value.ClanTag}] {clan.Value.ClanName} ({ClanSearchStructure.GetRegionCode(clan.Value.Region)})", stringValue: clan.Key));

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}