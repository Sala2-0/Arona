using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Database;
using Arona.Utility;

namespace Arona.Autocomplete;

internal class ClanRemoveAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context
    )
    {
        string guildId = context.Interaction.GuildId.ToString()!;

        var guild = Collections.Guilds.FindOne(g => g.Id == guildId);

        if (guild == null)
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("No database exists for this server. Add a clan to initialize one.", "undefined")
            ]);

        if (guild.Clans.Count == 0)
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([
                new ApplicationCommandOptionChoiceProperties("No clans in database", "undefined")
            ]);

        List<Clan> clans = [];

        foreach (long clanId in guild.Clans)
        {
            Clan dbClan = Collections.Clans.FindOne(c => c.Id == clanId);
            
            clans.Add(dbClan);
        }

        var choices = clans
            .Take(20)
            .Select(clan =>
                new ApplicationCommandOptionChoiceProperties(
                    name: $"[{clan.ClanTag}] {clan.ClanName} ({ClanSearchStructure.GetRegionCode(clan.Region)})",
                    stringValue: clan.Id.ToString()
                )
            );

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}