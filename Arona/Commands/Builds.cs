using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Autocomplete;
using Arona.Database;
using Arona.Utility;

namespace Arona.Commands;

[SlashCommand("builds", "Store WoWs builds for ease of access later on")]
public class Builds : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "Add a ship build to server database in form of WoWs-ShipBuilder link")]
    public async Task BuildsAddAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name")] string name,
        [SlashCommandParameter(Name = "link", Description = "ShipBuilder link")] string link,
        [SlashCommandParameter(Name = "description", Description = "Short description for the build")] string? description = null,
        [SlashCommandParameter(Name = "color", Description = "Optional color. HEX-format only")] string? color = null)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        string guildId = Context.Interaction.GuildId.ToString()!;

        await Program.WaitForWriteAsync(guildId);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guildId);

        var guild = Collections.Guilds.FindOne(g => g.Id == Context.Interaction.GuildId.ToString());

        if (guild == null)
        {
            guild = new Guild
            {
                Id = Context.Interaction.GuildId.ToString()!,
                ChannelId = Context.Interaction.Channel.Id.ToString()
            };
            Collections.Guilds.Insert(guild);
        }

        if (guild.Builds.Count >= 25)
        {
            await deferredMessage.EditAsync("❌ Maximum number of builds reached (25).");

            Program.ActiveWrites.Remove(guildId);
            return;
        }

        if (guild.Builds.Exists(build => build.Name == name))
        {
            await deferredMessage.EditAsync($"❌ Build with name `{name}` already exists.");

            Program.ActiveWrites.Remove(guildId);
            return;
        }
        
        using var client = new HttpClient();
        
        string body = $"{{\"link\":\"{link}\"}}";
        using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
        content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var response = await client.PostAsync("http://localhost:3000/verify", content);

        if (!response.IsSuccessStatusCode)
        {
            await deferredMessage.EditAsync("❌ Invalid build link!");

            Program.ActiveWrites.Remove(guildId);
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

        Collections.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Added build: `{name}`");

        Program.ActiveWrites.Remove(guildId);
    }

    [SubSlashCommand("remove", "Remove a build from server database")]
    public async Task BuildsRemoveAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name", AutocompleteProviderType = typeof(BuildAutocomplete))] string name)
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };

        await deferredMessage.SendAsync();

        string guildId = Context.Interaction.GuildId.ToString()!;

        await Program.WaitForWriteAsync(guildId);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guildId);

        var guild = Collections.Guilds.FindOne(g => g.Id == Context.Interaction.GuildId.ToString());
        if (guild == null || guild.Builds.Count == 0)
        {
            await deferredMessage.EditAsync("❌ No builds found in the database.");

            Program.ActiveWrites.Remove(guildId);
            return;
        }

        var build = guild.Builds.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (build == null)
        {
            await deferredMessage.EditAsync($"❌ Build `{name}` not found.");

            Program.ActiveWrites.Remove(guildId);
            return;
        }

        guild.Builds.Remove(build);

        var res = Collections.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Removed build: `{name}`");

        Program.ActiveWrites.Remove(guildId);
    }

    [SubSlashCommand("get", "Get a build from server database")]
    public async Task BuildsGetAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name", AutocompleteProviderType = typeof(BuildAutocomplete))] string name,
        [SlashCommandParameter(Name = "info", Description = "Get metadata or image of the build. Default: Image")] BuildData data = BuildData.Image
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        
        await deferredMessage.SendAsync();
        
        var guild = Collections.Guilds.FindOne(g => g.Id == Context.Interaction.GuildId.ToString());
        if (guild == null || guild.Builds.Count == 0)
        {
            await deferredMessage.EditAsync("❌ No builds found in the database.");
            return;
        }

        var build = guild.Builds.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (build == null)
        {
            await deferredMessage.EditAsync($"❌ Build with name `{name}` not found.");
            return;
        }

        if (data == BuildData.Image)
        {
            using var client = new HttpClient();

            string body = $"{{\"link\":\"{build.Link}\"}}";
            using var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            var response = await client.PostAsync("http://localhost:3000/build", content);

        
        
            string guildId = Context.Interaction.GuildId.ToString()!;
            string parsedName = build.Name.ToLower().Replace(" ", "_");
        
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(imageBytes);
            await deferredMessage.Interaction.SendFollowupMessageAsync(
                new InteractionMessageProperties()
                    .WithAttachments([new AttachmentProperties($"{guildId}_{parsedName}.png", stream)])
            );
        }
        else if (data == BuildData.Metadata)
        {
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
            
            await deferredMessage.EditAsync(embed);
        }
    }
    
    public enum BuildData
    {
        Image,
        Metadata
    }   
}