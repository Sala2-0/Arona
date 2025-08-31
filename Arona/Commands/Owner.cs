using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using NetCord.Services.Commands;
using Arona.ApiModels;
using Arona.Database;
using Arona.Utility;

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
                    await PrivateMessage.NoPermissionMessage(ulong.Parse(guild.Id), channel!.Name);
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
                await PrivateMessage.NoAccessMessage(ulong.Parse(guild.Id), guild.ChannelId);
            }
        }
    }

    [Command("guilds")]
    public async Task GuildsAsync()
    {
        if (!Owner.Check(Context.User.Id)) return;

        string message = $"**{Program.Client!.Cache.Guilds.Count}** servrar har Arona tillagd\n\nServrar:";

        foreach (var guild in Program.Client.Cache.Guilds.Values)
            message += $"\n`{guild.Name}` (ID: {guild.Id})";

        await Context.Message.ReplyAsync(message);
    }

    [Command("avgsf")]
    public async Task AverageSuccessFactorAsync(int league = 0, int division = 1, int? season = null)
    {
        if (!Owner.Check(Context.User.Id)) return;

        var leagueExponent = Ratings.GetLeagueExponent(league);
        List<double> sf = new();

        try
        {
            using var client = new HttpClient();

            var res = await client.GetAsync(LadderStructure.GetUrl(season, league, division));
            res.EnsureSuccessStatusCode();

            var data = JsonSerializer.Deserialize<LadderStructure[]>(await res.Content.ReadAsStringAsync());

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
            return;
        }
    }
}

public class OwnerAppCommands : ApplicationCommandModule<ApplicationCommandContext>
{

}

internal class Owner
{
    internal static bool Check(ulong userId) => userId == 1203783301580460032;
}