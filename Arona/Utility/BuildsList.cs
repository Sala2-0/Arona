﻿namespace Arona.Utility;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Database;
using MongoDB.Driver;

internal class BuildsList : IAutocompleteProvider<AutocompleteInteractionContext>
{
    public ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?> GetChoicesAsync(
        ApplicationCommandInteractionDataOption option,
        AutocompleteInteractionContext context)
    {
        var guildId = context.Interaction.GuildId.ToString();

        var guild = Program.GuildCollection.Find(g => g.Id == guildId).FirstOrDefault();

        if (guild == null || guild.Builds.Count == 0)
        {
            return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>([]);
        }

        var choices = guild.Builds
            .Take(25)
            .Select(build => new ApplicationCommandOptionChoiceProperties(name: build.Name, stringValue: build.Name));

        return new ValueTask<IEnumerable<ApplicationCommandOptionChoiceProperties>?>(choices);
    }
}