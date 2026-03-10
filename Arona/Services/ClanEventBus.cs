using Arona.ClanEvents;

namespace Arona.Services;

public interface IClanEventBus
{
    public event Func<ClanSessionEnded, Task> SessionEnded;
    public event Func<ClanSessionStarted, Task> SessionStarted;
    public event Func<BattleDetected, Task> BattleDetected;
    

    public Task OnSessionEnded(ClanSessionEnded evt);
    public Task OnSessionStarted(ClanSessionStarted evt);
    public Task OnBattleDetected(BattleDetected evt);
}

public class ClanEventBus : IClanEventBus
{
    public event Func<ClanSessionEnded, Task>? SessionEnded;
    public event Func<ClanSessionStarted, Task>? SessionStarted;
    public event Func<BattleDetected, Task>? BattleDetected;

    public Task OnSessionEnded(ClanSessionEnded evt) =>
        SessionEnded?.Invoke(evt) ?? Task.CompletedTask;

    public Task OnSessionStarted(ClanSessionStarted evt) =>
        SessionStarted?.Invoke(evt) ?? Task.CompletedTask;

    public Task OnBattleDetected(BattleDetected evt) =>
        BattleDetected?.Invoke(evt) ?? Task.CompletedTask;
}
