using System.Collections.Concurrent;
using NetCord.Rest;

namespace Arona.Services;

internal static class ComponentInactivityTimer
{
    public static readonly ConcurrentDictionary<string, CancellationTokenSource> Timers = new();

    public static async Task StartAsync(RestMessage message, TimeSpan timeout, CancellationTokenSource cts)
    {
        try
        {
            var stringId = message.Id.ToString();
            
            await Task.Delay(timeout, cts.Token);
            await message.ModifyAsync(x => x.Components = []);
            Timers.TryRemove(stringId, out _);

            RecentInteractions.AccountClanBattleSeasonDataInteractions.Remove(stringId, out _);
        }
        catch (TaskCanceledException)
        {
        }
    }
    
    public static async Task StartLineupDataTimerAsync(string battleId, TimeSpan timeout, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(timeout, cts.Token);
            Timers.TryRemove(battleId, out _);

            RecentInteractions.LineupData.Remove(battleId, out _);
        }
        catch (TaskCanceledException)
        {
        }
    }
}