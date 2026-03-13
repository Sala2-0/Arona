using NetCord;
using NetCord.Rest;

namespace Arona.Services.Message;

/// <summary>
/// Wrapper for sending deferred response after a command is used
/// </summary>
internal class DeferredMessage
{
    public ApplicationCommandInteraction Interaction { get; init; }

    private DeferredMessage(ApplicationCommandInteraction interaction)
    {
        Interaction = interaction;
    }

    /// <summary>
    /// Always use before using <see cref="EditAsync"/>
    /// </summary>
    private async Task SendDeferredAsync() =>
        await Interaction.SendResponseAsync(InteractionCallback.DeferredMessage());

    public async Task EditAsync(MessageProperties properties) =>
        await Interaction.ModifyResponseAsync(options =>
        {
            options.Content = properties.Content;
            options.Embeds = properties.Embeds;
            options.Attachments = properties.Attachments;
            options.Components = properties.Components;
        });

    /// <summary>
    /// Creates and automatically sends deferred message and returns the object
    /// </summary>
    /// <param name="interaction"></param>
    /// <returns></returns>
    public static async Task<DeferredMessage> CreateAsync(ApplicationCommandInteraction interaction)
    {
        var obj = new DeferredMessage(interaction);
        await obj.SendDeferredAsync();
        return obj;
    }
}
