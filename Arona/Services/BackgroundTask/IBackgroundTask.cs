using Microsoft.Extensions.Hosting;

namespace Arona.Services.BackgroundTask;

public interface IBackgroundTask
{
    public Task RunAsync();
}