using Microsoft.Extensions.Hosting;
using Arona.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Arona;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Config.Initialize();

        var builder = new ApplicationBuilder(args);
        var host = builder.Build();
        
        var apiClient = host.Services.GetRequiredService<IApiClient>();
        if (args.Length >= 2 && args[0] == "--port" && short.TryParse(args[1], out var port))
        {
            apiClient.SetServicePort(port);
        }

        Console.WriteLine("Service API port set to " + apiClient.ServicePort);

        await Task.Delay(3000);
        if (!await apiClient.IsServiceOnlineAsync()) return;
        
        await host.RunAsync();
    }
}