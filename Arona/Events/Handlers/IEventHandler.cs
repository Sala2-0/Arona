namespace Arona.Events.Handlers;

public interface IEventHandler<TEvent>
{
    public Task OnEventAsync(TEvent evt);
}