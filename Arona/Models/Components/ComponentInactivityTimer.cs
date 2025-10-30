using System.Collections.Concurrent;
using NetCord.Rest;

namespace Arona.Models.Components;

internal static class ComponentInactivityTimer
{
    public static ConcurrentDictionary<ulong, CancellationTokenSource> Timers = new();

    public static async Task StartAsync(RestMessage message, TimeSpan timeout, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(timeout, cts.Token);
            await message.ModifyAsync(x => x.Components = []);
            Timers.TryRemove(message.Id, out _);

            RecentInteractions.AccountSeasonDataCommands.Remove(message.Id);
        }
        catch (TaskCanceledException)
        {
        }
    }
}