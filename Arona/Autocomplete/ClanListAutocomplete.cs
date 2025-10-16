using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Database;
using Arona.Utility;

namespace Arona.Autocomplete;

internal class ClanListAutocomplete : IAutocompleteProvider<AutocompleteInteractionContext>
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

        var clans = Collections.Clans.Find(c => guild.Clans.Contains(c.Clan.Id)).ToList();

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(clans
            .Take(20)
            .Select(clan =>
                new ApplicationCommandOptionChoiceProperties(
                    name: $"[{clan.Clan.Tag}] {clan.Clan.Name} ({ClanUtils.GetRegionCode(clan.ExternalData.Region)})",
                    stringValue: clan.Clan.Id.ToString()
                )
            )
        );
    }
}