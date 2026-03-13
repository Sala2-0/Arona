using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using LiteDB;
using Arona.Models.DB;
using Arona.Services;

namespace Arona;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Config.Initialize();

        var builder = new ApplicationBuilder(args);
        builder.SetDatabase();
        builder.SetApiService();
        var host = builder.Build();
        
        var apiService = host.Services.GetRequiredService<IApiService>();
        if (args is ["--port", var portStr, ..] && short.TryParse(portStr, out var port))
        {
            apiService.SetServicePort(port);
        }   

        Console.WriteLine("Service API port set to " + apiService.ServicePort);

        await Task.Delay(3000);
        if (!await apiService.IsServiceOnline()) return;
        
        var database = host.Services.GetRequiredService<LiteDatabase>();
        var emojiService = host.Services.GetRequiredService<EmojiService>();
        await emojiService.InitializeAsync();
        
        Repository.Initialize(database);
        
        await host.RunAsync();
    }
}