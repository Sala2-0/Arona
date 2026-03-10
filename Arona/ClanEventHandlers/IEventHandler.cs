namespace Arona.ClanEventHandlers;

public interface IEventHandler<T>
{
    public Task OnEventAsync(T evt);
}