using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Arona.Services;

namespace Arona.Commands.Components;

public class ButtonModule : ComponentInteractionModule<ButtonInteractionContext>
{
    [ComponentInteraction("button-id")]
    public async Task Button(string data)
    {
        await Context.Interaction.SendResponseAsync(InteractionCallback.ModifyMessage(message =>
        {
            var embed = new EmbedProperties().WithTitle(data);

            // Update the message content
            message.Embeds = [embed];

            // Optionally, remove all components
            message.Components = null;
        }));
    }

    [ComponentInteraction("account season data")]
    public async Task AccountSeasonDataAsync(ulong userId, string title, int page)
    {
        var self = await Program.Client!.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        if (ComponentInactivityTimer.Timers.TryGetValue(Context.Message.Id, out var existingCts))
        {
            await existingCts.CancelAsync();
            existingCts.Dispose();
        }

        await Context.Interaction.SendResponseAsync(InteractionCallback.ModifyMessage(message =>
        {
            var data = RecentInteractions.AccountClanBattleSeasonDataInteractions[Context.Message.Id];

            var buttons = new List<ButtonProperties>();

            if (data.Keys.Contains(page + 1))
                buttons.Add(new ButtonProperties($"account season data:{Context.User.Id}:{title}:{page + 1}", ">", ButtonStyle.Secondary));
            if (data.Keys.Contains(page - 1))
                buttons.Add(new ButtonProperties($"account season data:{Context.User.Id}:{title}:{page - 1}", "<", ButtonStyle.Secondary));

            var embed = new EmbedProperties
            {
                Author = new EmbedAuthorProperties { Name = "Arona's intelligence report", IconUrl = botIconUrl },
                Title = title,
                Fields = data[page].Select(s => new EmbedFieldProperties
                    {
                        Name = $"S{s.Id} {s.Name}",
                        Value = $"`{s.BattlesCount}` BTL -> `{Math.Round((double)s.WinsCount / s.BattlesCount * 100, 2).ToString(System.Globalization.CultureInfo.InvariantCulture)}%` W/B\n"
                    })
                    .ToList()
            };

            message.Embeds = [embed];
            message.Components = [new ActionRowProperties { Buttons = buttons }];
        }));

        var timeout = TimeSpan.FromSeconds(30);
        var cts = new CancellationTokenSource();
        ComponentInactivityTimer.Timers[Context.Message.Id] = cts;
        await ComponentInactivityTimer.StartAsync(Context.Message, timeout, cts);
    }
}