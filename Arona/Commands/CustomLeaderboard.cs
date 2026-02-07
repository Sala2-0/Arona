using Arona.Models;
using Arona.Models.Api.Clans;
using Arona.Models.Api.Official;
using NetCord.Services.ApplicationCommands;
using Arona.Models.DB;
using Arona.Services.Message;
using Arona.Services;
using Arona.Utility;
using NetCord.Rest;

namespace Arona.Commands;

[SlashCommand("custom_leaderboard", "User defined leaderboard used to compare specific clans")]
public class CustomLeaderboardCommand : ApplicationCommandModule<ApplicationCommandContext>
{
    [SubSlashCommand("set", "Set the region for the leaderboard")]
    public async Task SetAsync(
        [SlashCommandParameter(Name = "region")] CustomLeaderboardCommandRegion region
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var userId = Context.User.Id.ToString();

        await DatabaseService.WaitForWriteAsync(userId);
        await DatabaseService.WaitForUpdateAsync();

        using var key = new DatabaseService.DatabaseWriteKey(userId);

        var user = User.Find(userId);

        try
        {
            var targetRegion = region switch
            {
                CustomLeaderboardCommandRegion.Europe => Region.Eu,
                CustomLeaderboardCommandRegion.NorthAmerica => Region.Na,
                CustomLeaderboardCommandRegion.Asia => Region.Asia,
                _ => throw new InvalidDataException("Invalid region")
            };

            user.CustomLeaderboard = new CustomLeaderboard
            {
                Region = targetRegion,
                Clans = []
            };

            Collections.Users.Update(user);

            await deferredMessage.EditAsync($"Created a new leaderboard for region '{targetRegion.ToString().ToUpper()}'");
        }
        catch (Exception ex)
        {
            await Program.LogError(ex);
            await deferredMessage.EditAsync($"❌ Error >_<\n\n{ex.Message}");
        }
    }

    [SubSlashCommand("bulk_add", "Add multiple clans to the leaderboard")]
    public async Task BulkAddAsync(
        [SlashCommandParameter(Name = "clan_tags", Description = "List of clan tags separated by spacebar")]
        string clanTags
    )
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var userId = Context.Interaction.User.Id.ToString();

        await DatabaseService.WaitForWriteAsync(userId);
        await DatabaseService.WaitForUpdateAsync();

        using var key = new DatabaseService.DatabaseWriteKey(userId);

        var user = User.Find(userId);

        if (user.CustomLeaderboard == null)
        {
            await deferredMessage.EditAsync("You need to set a region for the leaderboard first.\nDo that with `/custom_leaderboard set`");
            return;
        }

        var clansAdded = new List<string>();
        var clansFailedToAdd = new List<string>();

        try
        {
            var clanTagList = clanTags.Split(' ').ToList();

            var query = new ClanListItemQuery(ApiClient.Instance);

            foreach (var tag in clanTagList)
            {
                var response = await query.GetAsync(new ClanListItemRequest(user.CustomLeaderboard.Region.ToString(), tag));

                var targetClan = response.Data.FirstOrDefault(c => c.Tag == tag) ?? null;

                if (targetClan == null || user.CustomLeaderboard.Clans.Exists(id => id == targetClan.ClanId))
                {
                    clansFailedToAdd.Add(tag);
                }
                else
                {
                    user.CustomLeaderboard.Clans.Add(targetClan.ClanId);
                    clansAdded.Add(tag);
                }

                await Task.Delay(500);
            }

            Collections.Users.Update(user);

            var formattedMessage = string.Empty;
            if (clansAdded.Count > 0)
            {
                formattedMessage = "✅ Clans successfully added: ";
                foreach (var clan in clansAdded)
                {
                    formattedMessage += $"`{clan}` ";
                }
            }

            if (clansFailedToAdd.Count > 0)
            {
                formattedMessage += "\n❌ Clans failed to add:";
                foreach (var clan in clansFailedToAdd)
                {
                    formattedMessage += $"`{clan}` ";
                }
            }

            await deferredMessage.EditAsync(formattedMessage);
        }
        catch (Exception ex)
        {
            await Program.LogError(ex);
            await deferredMessage.EditAsync($"❌ Error >_<\n\n{ex.Message}");
        }
    }

    [SubSlashCommand("display", "Display the leaderboard")]
    public async Task DisplayAsync()
    {
        var deferredMessage = new DeferredMessage { Interaction = Context.Interaction };
        await deferredMessage.SendAsync();

        var user = User.Find(Context.Interaction.User.Id.ToString());

        if (user.CustomLeaderboard == null || user.CustomLeaderboard.Clans.Count == 0)
        {
            await deferredMessage.EditAsync("You currently have no clans saved on your leaderboard!");
            return;
        }

        var clanInfo = new List<ClanView>();

        var clanViewQuery = new ClanViewQuery(ApiClient.Instance);
        foreach (var clanId in user.CustomLeaderboard.Clans)
        {
            var root = await clanViewQuery.GetAsync(new ClanViewRequest(
                ClanId: clanId,
                Region: user.CustomLeaderboard.Region.ToString()));

            clanInfo.Add(root.ClanView);

            await Task.Delay(500);
        }

        clanInfo = clanInfo.OrderByDescending(c => c.WowsLadder.PublicRating).ToList();

        var embed = new EmbedProperties
        {
            Title = $"Custom Leaderboard ({user.CustomLeaderboard.Region.ToString().ToUpper()})"
        };

        var fields = new List<EmbedFieldProperties>();

        for (var i = 0; i < clanInfo.Count; i++)
        {
            var clan = clanInfo[i];
            var successFactor = Math.Round(
                SuccessFactor.Calculate(clan.WowsLadder.PublicRating, clan.WowsLadder.BattlesCount, ClanUtils.GetLeagueExponent(clan.WowsLadder.League)),
                2
            );

            fields.Add(new EmbedFieldProperties
            {
                Name = $"#{i + 1} `[{clan.Clan.Tag}]` ({clan.WowsLadder.League} {clan.WowsLadder.Division} - {clan.WowsLadder.DivisionRating}) `BTL: {clan.WowsLadder.BattlesCount}` `SF: {successFactor}`"
            });
        }

        embed.WithFields(fields);

        await deferredMessage.EditAsync(embed);
    }

    public enum CustomLeaderboardCommandRegion
    {
        [SlashCommandChoice(Name = "EU")] Europe,
        [SlashCommandChoice(Name = "NA")] NorthAmerica,
        [SlashCommandChoice(Name = "ASIA")] Asia,
    }
}