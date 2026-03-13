using Arona.Events;

namespace Arona.Services;

public static class EventBus
{
    public static event Func<ClanBattleSessionEnded, Task>? SessionEnded;
    public static event Func<ClanBattleSessionStarted, Task>? SessionStarted;
    public static event Func<BattleDetected, Task>? BattleDetected;

    public static Task OnSessionEnded(ClanBattleSessionEnded evt) =>
        SessionEnded?.Invoke(evt) ?? Task.CompletedTask;

    public static Task OnSessionStarted(ClanBattleSessionStarted evt) =>
        SessionStarted?.Invoke(evt) ?? Task.CompletedTask;

    public static Task OnBattleDetected(BattleDetected evt) =>
        BattleDetected?.Invoke(evt) ?? Task.CompletedTask;
}
