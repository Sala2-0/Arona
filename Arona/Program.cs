using System.Diagnostics;
using ConfigSerializer = System.Text.Json.JsonSerializer;
using Timer = System.Timers.Timer;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using LiteDB;
using Arona.Database;
using Arona.Utility;

namespace Arona;

internal class Program
{
    public static LiteDatabase DB;
    public static GatewayClient? Client { get; private set; }
    public static bool UpdateProgress { get; set; } = false;
    public static readonly List<string> ActiveWrites = [];
    
    private static async Task Main(string[] args)
    {
        Config.Initialize();

        DB = new LiteDatabase(Path.Combine(AppContext.BaseDirectory, "Arona_DB.db"));
        Collections.Initialize(DB);

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Debugger.IsAttached
                    ? Config.DevToken
                    : Config.Token;
            })
            .AddApplicationCommands();
        
        var host = builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        Client = host.Services.GetRequiredService<GatewayClient>();

        // Varje minut, hämta API och kolla klan aktiviteter
        Timer clanMonitorTask = new(10000); // 300000
        clanMonitorTask.Elapsed += async (sender, e) =>
        {
            await UpdateClan.UpdateClansAsync();
        };
        clanMonitorTask.AutoReset = true;
        clanMonitorTask.Enabled = true;
        clanMonitorTask.Start();

        await host.RunAsync();
    }

    public static void Error(Exception ex) =>
        Console.WriteLine("Error: " + ex.Message);

    // Väntar på att UpdateClansAsync slutförs om det pågår en
    // Används bara för kommandon som skriver till databasen
    public static async Task WaitForUpdateAsync()
    {
        while (UpdateProgress)
        {
            Console.WriteLine("Waiting for update to finish...");
            
            await Task.Delay(1000);
        }
    }

    // Vänta tills pågående databasskrivning för en guild är klar
    public static async Task WaitForWriteAsync(string guildId)
    {
        while (ActiveWrites.Contains(guildId))
        {
            Console.WriteLine("Waiting for database write to finish...");

            await Task.Delay(1000);
        }
    }
}