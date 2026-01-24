using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using Arona.Commands.Autocomplete;
using Arona.Models.DB;
using Arona.Services.Message;

namespace Arona.Commands;

[SlashCommand("builds", "Store WoWs builds for ease of access later on")]
public class Builds : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("add", "Add a ship build to server database in form of WoWs-ShipBuilder link")]
    public async Task BuildsAddAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name")]
        string name,

        [SlashCommandParameter(Name = "link", Description = "ShipBuilder link")]
        string link,

        [SlashCommandParameter(Name = "description", Description = "Short description for the build")]
        string? description = null,

        [SlashCommandParameter(Name = "color", Description = "Optional color. HEX-format only")]
        string? color = null
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var guild = Guild.Find(Context.Interaction);

        await Program.WaitForWriteAsync(guild.Id);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guild.Id);

        if (guild.Builds.Count >= 25)
        {
            await deferredMessage.EditAsync("❌ Maximum number of builds reached (25).");

            Program.ActiveWrites.Remove(guild.Id);
            return;
        }

        if (guild.Builds.Exists(build => build.Name == name))
        {
            await deferredMessage.EditAsync($"❌ Build with name `{name}` already exists.");

            Program.ActiveWrites.Remove(guild.Id);
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

            Program.ActiveWrites.Remove(guild.Id);
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
        Program.ActiveWrites.Remove(guild.Id);
    }

    [SubSlashCommand("remove", "Remove a build from server database")]
    public async Task BuildsRemoveAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name", AutocompleteProviderType = typeof(BuildAutocomplete))]
        string name
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var guild = Guild.Find(Context.Interaction);

        await Program.WaitForWriteAsync(guild.Id);
        await Program.WaitForUpdateAsync();

        Program.ActiveWrites.Add(guild.Id);

        if (guild.Builds.Count == 0)
        {
            await deferredMessage.EditAsync("❌ No builds found in the database.");

            Program.ActiveWrites.Remove(guild.Id);
            return;
        }

        var build = guild.Builds.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? null;
        if (build == null)
        {
            await deferredMessage.EditAsync($"❌ Build `{name}` not found.");

            Program.ActiveWrites.Remove(guild.Id);
            return;
        }

        guild.Builds.Remove(build);
        Collections.Guilds.Update(guild);

        await deferredMessage.EditAsync($"✅ Removed build: `{name}`");
        Program.ActiveWrites.Remove(guild.Id);
    }

    [SubSlashCommand("get", "Get a build from server database")]
    public async Task BuildsGetAsync(
        [SlashCommandParameter(Name = "name", Description = "Build name", AutocompleteProviderType = typeof(BuildAutocomplete))]
        string name,

        [SlashCommandParameter(Name = "info", Description = "Get metadata or image of the build. Default: Image")]
        BuildData data = BuildData.Image
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var guild = Guild.Find(Context.Interaction);

        if (guild.Builds.Count == 0)
        {
            await deferredMessage.EditAsync("❌ No builds found in the database.");
            return;
        }

        var build = guild.Builds.FirstOrDefault(b => b.Name.Equals(name, StringComparison.OrdinalIgnoreCase)) ?? null;
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

            string parsedName = build.Name.ToLower().Replace(" ", "_");
        
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(imageBytes);

            await deferredMessage.Interaction.SendFollowupMessageAsync(
                new InteractionMessageProperties()
                    .WithAttachments([new AttachmentProperties($"{guild.Id}_{parsedName}.png", stream)])
            );
        }
        else if (data == BuildData.Metadata)
        {
            var embed = new EmbedProperties
            {
                Title = build.Name,
                Fields = new List<EmbedFieldProperties>
                {
                    new() { Name = "Description", Value = build.Description ?? "No description" },
                    new() { Name = "Link", Value = build.Link },
                    new() { Name = "Creator", Value = build.CreatorName }
                }
            };

            if (!string.IsNullOrEmpty(build.Color))
                embed.Color = new Color(Convert.ToInt32(build.Color, 16));

            await deferredMessage.EditAsync(embed);
        }
    }
    
    public enum BuildData
    {
        Image,
        Metadata
    }   
}