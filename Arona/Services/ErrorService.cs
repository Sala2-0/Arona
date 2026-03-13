using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

namespace Arona.Services;

public class ErrorService(GatewayClient gatewayClient)
{
    public async Task PrintErrorAsync(Exception ex, string? customMessage = null)
    {
        Console.WriteLine(ex.Message);
        Console.WriteLine(ex.StackTrace);

        if (customMessage != null)
        {
            Console.WriteLine("Message: " + ex.Message);
        }

        try
        {
            var embed = new EmbedProperties()
                .WithTitle("Application Error!")
                .AddFields(new EmbedFieldProperties
                {
                    Name = "Exception Information",
                    Value = ex.ToString()
                });

            if (customMessage != null)
            {
                embed.AddFields(new EmbedFieldProperties
                {
                    Name = "Message",
                    Value = customMessage
                });
            }
            
            await gatewayClient.Rest.SendMessageAsync(
                channelId: Config.BackdoorChannel,
                message: new MessageProperties().AddEmbeds(embed));
        }
        catch (Exception e)
        {
            Console.WriteLine("Failed to send message to discord lol\n");
            Console.WriteLine(e);
        }
    }
    
    public async Task NotifyUserOfErrorAsync(Interaction interaction, Exception ex, bool deferredMode = false, string? customMessage = null)
    {
        var embed = new EmbedProperties()
            .WithTitle("Application Error!")
            .AddFields(new EmbedFieldProperties()
                .WithName("Exception Message")
                .WithValue(ex.Message));
        if (customMessage != null)
        {
            embed.AddFields(new EmbedFieldProperties()
                .WithName("Message")
                .WithValue(customMessage));
        }
        
        if (deferredMode)
        {
            await interaction.ModifyResponseAsync(options =>
            {
                options.AddEmbeds(embed);
            });
        }
        else
        {
            await interaction.SendResponseAsync(InteractionCallback.Message(new InteractionMessageProperties()
                .AddEmbeds(embed)));
        }
    }

    public async Task NotifyUserOfErrorAsync(ulong channelId, Exception ex, string? customMessage = null)
    {
        var embed = new EmbedProperties()
            .WithTitle("Application Error!")
            .AddFields(new EmbedFieldProperties()
                .WithName("Exception Message")
                .WithValue(ex.Message));
        if (customMessage != null)
        {
            embed.AddFields(new EmbedFieldProperties()
                .WithName("Message")
                .WithValue(customMessage));
        }
        
        await gatewayClient.Rest
            .SendMessageAsync(
                channelId: channelId,
                message: new MessageProperties().AddEmbeds(embed));
    }
}