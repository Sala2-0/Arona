using Arona.Services.Message;
using NetCord.Rest;

using Timer = System.Timers.Timer;

namespace Arona.Services.UpdateTasks;

public static partial class UpdateTasks
{
    public static async Task SendMessageAsync(ulong guildId, ulong channelId, EmbedProperties embed, long battleTimeSeconds)
    {
        battleTimeSeconds += 300;
        var currentTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        var timeDifference = battleTimeSeconds - currentTime;
        if (timeDifference <= 0)
        {
            await ChannelMessageService.SendAsync(guildId, channelId, embed);

            return;
        }

        var timer = new Timer(timeDifference * 1000d);
        timer.AutoReset = false;
        timer.Elapsed += async (_, _) =>
        {
            timer.Dispose();
            await ChannelMessageService.SendAsync(guildId, channelId, embed);
        };
        timer.Enabled = true;
        timer.Start();
    }

}