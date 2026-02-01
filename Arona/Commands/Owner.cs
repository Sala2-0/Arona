using System.Text.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using LiteDB;
using Arona.Utility;
using Arona.Models;
using Arona.Models.DB;
using Arona.Models.Api.Clans;
using Arona.Services.Message;

namespace Arona.Commands;

public class OwnerCommands : CommandModule<CommandContext>
{
    [Command("announce")]
    public async Task OwnerAsync(
        [CommandParameter(Remainder = true)] string content
    )
    {
        if (!Owner.Check(Context.User.Id)) return;

        var self = await Program.Client!.Rest.GetGuildUserAsync(
            guildId: Context.Guild!.Id,
            userId: (await Program.Client.Rest.GetCurrentUserAsync()).Id
        );

        var guilds = Collections.Guilds.FindAll().ToList();

        var attachments = Context.Message.Attachments;

        foreach (var guild in guilds)
        {
            try
            {
                var parsedChannelId = ulong.Parse(guild.ChannelId);

                var channel = await Program.Client.Rest.GetChannelAsync(parsedChannelId) as TextGuildChannel;

                var permissions = self.GetChannelPermissions(
                    guild: await Program.Client.Rest.GetGuildAsync(Context.Guild.Id),
                    channel: channel!
                );

                // Arona har inte tillstånd att skicka meddelanden
                if ((permissions & Permissions.SendMessages) == 0)
                {
                    await PrivateMessageService.SendNoPermissionMessageAsync(ulong.Parse(guild.Id), channel!.Name);
                    continue;
                }

                if (attachments.Count > 0)
                {
                    using var client = new HttpClient();
                    var files = new List<AttachmentProperties>();

                    foreach (var a in attachments)
                    {
                        var data = await client.GetByteArrayAsync(a.Url);
                        files.Add(new AttachmentProperties(a.FileName, new MemoryStream(data)));
                    }

                    var msgProperties = new MessageProperties()
                        .WithContent(content)
                        .WithAttachments(files);

                    await Program.Client.Rest.SendMessageAsync(
                        channelId: parsedChannelId,
                        message: msgProperties
                    );
                }
                else
                    await Program.Client.Rest.SendMessageAsync(
                        channelId: parsedChannelId,
                        message: content
                    );
            }
            // Arona har inte tillgång/kan inte se kanalen
            catch (Exception ex)
            {
                await PrivateMessageService.SendNoAccessMessageAsync(ulong.Parse(guild.Id), ulong.Parse(guild.ChannelId));
            }
        }
    }

    [Command("guilds")]
    public async Task GuildsAsync()
    {
        if (!Owner.Check(Context.User.Id)) return;

        var app = await Program.Client!.Rest.GetApplicationAsync(Config.ApplicationId);

        var totalMembers = Program.Client.Cache.Guilds.Values.Sum(g => g.UserCount);

        var message = $"**Statistik för Arona:**\n" +
                         $"- Servrar i cache: {Program.Client.Cache.Guilds.Count}\n" +
                         $"- Officiellt antal servrar: {app.ApproximateGuildCount ?? 0}\n" +
                         $"- Användarinstallationer: {app.ApproximateUserInstallCount ?? 0}\n" +
                         $"- Total räckvidd (medlemmar): {totalMembers}\n\n" +
                         $"**Servrar:**";

        foreach (var guild in Program.Client.Cache.Guilds.Values)
            message += $"\n`{guild.Name}` (ID: {guild.Id}) - {guild.UserCount} medlemmar";

        await Context.Message.ReplyAsync(message);
    }

    [Command("avgsf")]
    public async Task AverageSuccessFactorAsync(int league = 0, int division = 1, int? season = null)
    {
        if (!Owner.Check(Context.User.Id)) return;

        var leagueExponent = ClanUtils.GetLeagueExponent((League)league);
        List<double> sf = [];

        try
        {
            using var client = new HttpClient();

            var data = await LadderStructureBySeasonQuery.GetSingleAsync(
                new LadderStructureBySeasonRequest(season, league, division)
            );

            foreach (var clan in data!)
            {
                var successFactor = SuccessFactor.Calculate(
                    clan.PublicRating,
                    clan.BattlesCount,
                    leagueExponent
                );

                sf.Add(successFactor);
            }

            var averageSuccessFactor = sf.Count > 0 ? Math.Round(sf.Average(), 2) : 0;
            await Context.Message.ReplyAsync($"Genomsnittlig framgångsfaktor (S/F): {averageSuccessFactor}");
        }
        catch (Exception ex)
        {
            await Context.Message.ReplyAsync($"Något gick fel: {ex.Message}");
        }
    }

    [Command("dbcopy")]
    public async Task DatabaseCopyAsync()
    {
        if (!Owner.Check(Context.User.Id)) return;

        await Program.WaitForWriteAsync();
        await Program.WaitForUpdateAsync();

        Program.UpdateProgress = true;

        try
        {
            Program.DB.Dispose();

            await using var fileStream = File.OpenRead(Config.Database);
            using var memoryStream = new MemoryStream();

            await fileStream.CopyToAsync(memoryStream);

            memoryStream.Position = 0;

            await Context.Message.ReplyAsync(new ReplyMessageProperties()
                .WithAttachments([new AttachmentProperties("data.db", memoryStream)])
            );
        }
        catch (Exception ex)
        {
            await Context.Message.ReplyAsync($"Något gick fel: {ex.Message}");
        }
        finally
        {
            Program.DB = new LiteDatabase(Path.Combine(AppContext.BaseDirectory, Config.Database));
            Collections.Initialize(Program.DB);

            Program.UpdateProgress = false;
        }
    }

    [Command("cacheShips")]
    public async Task CacheShipsAsync()
    {
        if (!Owner.Check(Context.User.Id)) return;

        await Program.WaitForWriteAsync();
        await Program.WaitForUpdateAsync();

        Program.UpdateProgress = true;

        try
        {
            Collections.Ships.DeleteAll();

            using var client = new HttpClient();
            var res = await client.GetAsync("https://clans.worldofwarships.eu/api/encyclopedia/vehicles_info/");
            res.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<Dictionary<long, VehicleInfo>>(await res.Content.ReadAsStringAsync())!;

            res = await client.GetAsync("https://api.wows-numbers.com/personal/rating/expected/json/");
            JsonElement doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetProperty("data");

            Dictionary<long, JsonElement> filtered = doc.EnumerateObject()
                .Where(p => p.Value.ValueKind != JsonValueKind.Array)
                .ToDictionary(p => long.Parse(p.Name), p => p.Value);

            int count = 0;

            foreach (var ship in data)
            {
                if (!filtered.ContainsKey(ship.Key)) continue;

                Collections.Ships.Insert(new Ship
                {
                    Id = ship.Value.Id,
                    Name = ship.Value.Name,
                    ShortName = ship.Value.ShortName,
                    Tier = ship.Value.Tier
                });

                ++count;
            }
            
            await Context.Message.ReplyAsync($"Cache för {count} fartyg uppdaterad.");
        }
        catch (Exception ex)
        {
            await Context.Message.ReplyAsync($"Något gick fel: `{ex.Message}`");
        }
        finally
        {
            Program.UpdateProgress = false;
        }
    }
}

public class OwnerAppCommands : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("test", "test command")]
    public async Task TestAsync()
    {
        var button = new ButtonProperties("button-id:TEST", ">", ButtonStyle.Success);
        var prevButton = new ButtonProperties("button-id-2", "<", ButtonStyle.Success);

        var row = new ActionRowProperties
        {
            Buttons = [prevButton, button]
        };

        await Context.Interaction.SendResponseAsync(
            InteractionCallback.Message(
                new InteractionMessageProperties
                {
                    Content = "Test",
                    Components = [ row ]
                }
            )
        );
    }
}

internal static class Owner
{
    internal static bool Check(ulong userId) => userId == 1203783301580460032;
}