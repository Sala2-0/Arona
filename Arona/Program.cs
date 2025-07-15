namespace Arona;
using System.Text.Json;
using Config;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Gateway;
using MongoDB.Driver;
using Utility;

class Program
{
    public static BotConfig? Config = JsonSerializer.Deserialize<BotConfig>(BotConfig.GetConfigFilePath());
    public static MongoClient? DatabaseClient { get; private set; }
    public static GatewayClient? Client { get; private set; }
    static async Task Main(string[] args)
    {
        if (Config == null)
            throw new Exception("Configuration file not found or invalid.");

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Config.Token; // Använd Config.DevToken för utvecklingsläge
            })
            .AddApplicationCommands();
        
        var host = builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        Client = host.Services.GetRequiredService<GatewayClient>();

        DatabaseClient = new MongoClient(Config.Database);

        // Varje minut, hämta API och kolla klanaktiviteter
        Timer clanMonitorTask = new Timer(60000); // 300000
        clanMonitorTask.Elapsed += async (sender, e) =>
        {
            await UpdateClan.UpdateClansAsync();
        };
        clanMonitorTask.AutoReset = true;
        clanMonitorTask.Enabled = true;
        clanMonitorTask.Start();
        
        await host.RunAsync();
    }
}