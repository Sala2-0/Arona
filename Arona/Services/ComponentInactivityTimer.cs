using System.Collections.Concurrent;
using NetCord.Rest;

namespace Arona.Services;

internal static class ComponentInactivityTimer
{
    public static readonly ConcurrentDictionary<string, CancellationTokenSource> Timers = new();

    public static async Task StartAccountClanBattleSeasonDataInteractionsTimerAsync(RestMessage message, TimeSpan timeout, CancellationTokenSource cts)
    {
        try
        {
            await Task.Delay(timeout, cts.Token);
            await message.ModifyAsync(x => x.Components = []);
            Timers.TryRemove(message.Id.ToString(), out _);

            RecentInteractions.AccountClanBattleSeasonDataInteractions.Remove(message.Id, out _);
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

            RecentInteractions.LineUpData.Remove(battleId, out _);
        }
        catch (TaskCanceledException)
        {
        }
    }
}