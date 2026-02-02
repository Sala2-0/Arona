using Arona.ClanEvents;

namespace Arona.Services;

public static class ClanEventBus
{
    public static event Func<ClanSessionEnded, Task>? SessionEnded;
    public static event Func<ClanSessionStarted, Task>? SessionStarted;
    public static event Func<BattleDetected, Task>? BattleDetected;

    public static Task OnSessionEnded(ClanSessionEnded evt) =>
        SessionEnded?.Invoke(evt) ?? Task.CompletedTask;

    public static Task OnSessionStarted(ClanSessionStarted evt) =>
        SessionStarted?.Invoke(evt) ?? Task.CompletedTask;

    public static Task OnBattleDetected(BattleDetected evt) =>
        BattleDetected?.Invoke(evt) ?? Task.CompletedTask;
}
