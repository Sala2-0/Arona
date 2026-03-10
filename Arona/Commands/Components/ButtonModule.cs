using System.Text.Json;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ComponentInteractions;
using Arona.Services;
using NetCord.Gateway;

namespace Arona.Commands.Components;

public class ButtonModule(GatewayClient client, IApiClient apiClient, IErrorService errorService) : ComponentInteractionModule<ButtonInteractionContext>
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
        var self = await client.Rest.GetCurrentUserAsync();
        var botIconUrl = self.GetAvatarUrl()!.ToString();

        if (ComponentInactivityTimer.Timers.TryGetValue(Context.Message.Id.ToString(), out var existingCts))
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
        ComponentInactivityTimer.Timers[Context.Message.Id.ToString()] = cts;
        await ComponentInactivityTimer.StartAccountClanBattleSeasonDataInteractionsTimerAsync(Context.Message, timeout, cts);
    }

    [ComponentInteraction("battle result lineup data")]
    public async Task BattleResultLineupDataAsync(string battleId)
    {
        RecentInteractions.LineUpData.TryGetValue(battleId, out var lineupData);

        if (lineupData == null)
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties
            {
                Content = "Lineup data unavailable"
            }));

            return;
        }
        
        try
        {
            var response = await apiClient.PostToServiceAsync("Lineup", JsonSerializer.Serialize(lineupData));
            response.EnsureSuccessStatusCode();
                    
            var imageBytes = await response.Content.ReadAsByteArrayAsync();
            using var stream = new MemoryStream(imageBytes);
            
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties
            {
                Attachments = [new AttachmentProperties("Lineup.png", stream)],
            }));
        }
        catch (Exception ex)
        {
            await errorService.LogErrorAsync(ex);
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties
            {
                Content = "Internal Error >_<"
            }));
        }
    }
}