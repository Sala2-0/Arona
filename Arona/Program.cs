﻿namespace Arona;
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

internal class Program
{
    public static BotConfig? Config = JsonSerializer.Deserialize<BotConfig>(BotConfig.GetConfigFilePath());
    public static MongoClient? DatabaseClient { get; private set; }
    public static IMongoCollection<Database.Guild>? Collection { get; private set; }
    public static GatewayClient? Client { get; private set; }
    public static bool UpdateProgress { get; set; } = false;
    private static async Task Main(string[] args)
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
        Collection = DatabaseClient.GetDatabase("Arona").GetCollection<Database.Guild>("servers");

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

    public static void ApiError(Exception ex) =>
        Console.WriteLine("Error med hämtning av API data: " + ex.Message);

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
}