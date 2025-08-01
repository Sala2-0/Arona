﻿namespace Arona.Commands;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Database;
using MongoDB.Driver;
using Utility;
using NetCord;

public class Builds : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("builds_add", "Add a ship build to server database in form of WoWs-ShipBuilder link")]
    public async Task BuildsAddAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name")] string name,
        [SlashCommandParameter(Name = "link", Description = "ShipBuilder link")] string link,
        [SlashCommandParameter(Name = "description", Description = "Short description for the build")] string? description = null,
        [SlashCommandParameter(Name = "color", Description = "Optional color. HEX-format only")] string? color = null)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        await Program.WaitForUpdateAsync();

        var guild = await Program.GuildCollection!.Find(g => g.Id == Context.Interaction.GuildId.ToString()).FirstOrDefaultAsync();

        if (guild == null)
        {
            guild = new Guild
            {
                Id = Context.Interaction.GuildId.ToString()!,
                ChannelId = Context.Interaction.Channel.Id.ToString()
            };
            await Program.GuildCollection!.InsertOneAsync(guild);
        }

        if (guild.Builds.Count >= 25)
        {
            await deferredMessage.EditAsync("❌ Maximum number of builds reached (25).");
            return;
        }

        foreach (var build in guild.Builds)
        {
            if (build.Name != name) continue;
            
            await deferredMessage.EditAsync($"❌ Build with name `{name}` already exists.");
            return;
        }

        guild.Builds.Add(new Build
        {
            Name = name,
            Description = description,
            Link = link,
            CreatorName = Context.Interaction.User.Username,
            Color = color?.TrimStart('#')
        });

        var res = await Program.GuildCollection!.ReplaceOneAsync(g => g.Id == guild.Id, guild);

        if (!res.IsAcknowledged)
        {
            await deferredMessage.EditAsync("❌ Error adding build to database.");
            return;
        }

        await deferredMessage.EditAsync($"✅ Added build: `{name}`");
    }

    [SlashCommand("builds_remove", "Remove a build from server database")]
    public async Task BuildsRemoveAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name", AutocompleteProviderType = typeof(BuildsList))] string name)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        await Program.WaitForUpdateAsync();

        var guild = await Program.GuildCollection!.Find(g => g.Id == Context.Interaction.GuildId.ToString()).FirstOrDefaultAsync();
        if (guild == null || guild.Builds.Count == 0)
        {
            await deferredMessage.EditAsync("❌ No builds found in the database.");
            return;
        }

        var build = guild.Builds.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (build == null)
        {
            await deferredMessage.EditAsync($"❌ Build `{name}` not found.");
            return;
        }

        guild.Builds.Remove(build);

        var res = await Program.GuildCollection!.ReplaceOneAsync(g => g.Id == guild.Id, guild);
        if (!res.IsAcknowledged)
        {
            await deferredMessage.EditAsync("❌ Error removing build from database.");
            return;
        }

        await deferredMessage.EditAsync($"✅ Removed build: `{name}`");
    }

    [SlashCommand("builds_get", "Get a build from server database")]
    public async Task BuildsGetAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name", AutocompleteProviderType = typeof(BuildsList))] string name)
    {
        var guild = await Program.GuildCollection!.Find(g => g.Id == Context.Interaction.GuildId.ToString()).FirstOrDefaultAsync();
        if (guild == null || guild.Builds.Count == 0)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message("❌ No builds found in the database.")
            );
            return;
        }

        var build = guild.Builds.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (build == null)
        {
            await Context.Interaction.SendResponseAsync(
                InteractionCallback.Message($"❌ Build with name `{name}` not found.")
            );
            return;
        }

        var embed = new EmbedProperties()
            .WithTitle(build.Name)
            .WithFields(
                new List<EmbedFieldProperties>
                {
                    new EmbedFieldProperties()
                        .WithName("Description")
                        .WithValue(build.Description ?? "No description")
                        .WithInline(false),
                    new EmbedFieldProperties()
                        .WithName("Link")
                        .WithValue(build.Link)
                        .WithInline(false),
                    new EmbedFieldProperties()
                        .WithName("Creator")
                        .WithValue(build.CreatorName)
                        .WithInline(false)
                });

        if (!string.IsNullOrEmpty(build.Color))
            embed.WithColor(new Color(Convert.ToInt32(build.Color, 16)));

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties()
                .WithEmbeds([embed])
            )
        );
    }
}