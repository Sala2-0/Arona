using NetCord.Rest;

using Timer = System.Timers.Timer;

namespace Arona.Services.UpdateTasks;

public static partial class UpdateTasks
{
    public static async Task SendMessageAsync(ulong channelId, EmbedProperties embed)
    {
        try
        {
            await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties { Embeds = [embed] });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
        }
    }

    public static async Task SendMessageAsync(ulong channelId, EmbedProperties embed, long battleTimeSeconds)
    {
        battleTimeSeconds += 300;
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var timeDifference = battleTimeSeconds - currentTime;
        if (timeDifference <= 0)
        {
            try
            {
                await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties { Embeds = [embed] });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }

            return;
        }

        var timer = new Timer(timeDifference * 1000d);
        timer.AutoReset = false;
        timer.Elapsed += async (_, _) =>
        {
            timer.Dispose();
            try
            {
                await Program.Client!.Rest.SendMessageAsync(channelId, new MessageProperties { Embeds = [embed] });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending message: {ex.Message}");
            }
        };
        timer.Enabled = true;
        timer.Start();
    }

}