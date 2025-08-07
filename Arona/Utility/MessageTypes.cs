using NetCord;
using NetCord.Rest;

namespace Arona.Utility;

internal class DeferredMessage
{
    public required ApplicationCommandInteraction Interaction { get; init; }

    // Använd alltid SendAsync först innan EditAsync används
    public async Task SendAsync() =>
        await Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

    public async Task EditAsync(string message) =>
        await Interaction.ModifyResponseAsync(options => options.Content = message);

    public async Task EditAsync(EmbedProperties embed) =>
        await Interaction.ModifyResponseAsync(options => options.Embeds = [embed]);
}