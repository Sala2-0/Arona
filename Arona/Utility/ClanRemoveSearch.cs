using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using MongoDB.Driver;
using Arona.Database;

namespace Arona.Utility;

internal class ClanRemoveSearch : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context
    )
    {
        string guildId = context.Interaction.GuildId.ToString()!;

        var guild = Program.GuildCollection.Find(g => g.Id == guildId).FirstOrDefault();

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
            Clan dbClan = Program.ClanCollection!.Find(c => c.Id == clanId).FirstOrDefault();
            
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