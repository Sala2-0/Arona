using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using LiteDB;
using NetCord;
using NetCord.Gateway;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using NetCord.Hosting.Services.Commands;
using NetCord.Hosting.Services.ComponentInteractions;
using NetCord.Services.ComponentInteractions;
using Arona.Events.Handlers;
using Arona.Models.DB;
using Arona.Services;
using Arona.Services.BackgroundTask;
using Arona.Services.Message;

namespace Arona;

public class ApplicationBuilder
{
    public HostApplicationBuilder Builder { get; }

    public ApplicationBuilder(string[] args)
    {
        Builder = Host.CreateApplicationBuilder(args);
        
        // Register primary discord services
        Builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = Config.GetToken();
                options.Intents = GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildUsers | GatewayIntents.Guilds;
            })
            .AddApplicationCommands()
            .AddCommands(options => options.Prefix = Debugger.IsAttached ? "?" : "arona?")
            .AddComponentInteractions<ButtonInteraction, ButtonInteractionContext>()
            .AddComponentInteractions<ModalInteraction, ModalInteractionContext>();
        
        // Register event handlers
        Builder.Services
            .AddHostedService<BattleDetectedHandler>()
            .AddHostedService<ClanBattleSessionStartedHandler>()
            .AddHostedService<ClanBattleSessionEndedHandler>();
        
        // Message services
        Builder.Services
            .AddSingleton<ChannelMessageService>();

        // Background services
        Builder.Services
            .AddHostedService<UpdateClanData>()
            .AddHostedService<UpdateLeaderboardTask>();
        
        // Other services
        Builder.Services
            .AddSingleton<ErrorService>()
            .AddSingleton<EmojiService>();
    }

    public IHost Build()
    {
        if (Builder == null)
        {
            throw new InvalidOperationException("ApplicationBuilder has not been initialized.");
        }
        
        var host = Builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        AddGatewayClientEvents(host.Services.GetRequiredService<GatewayClient>());
        
        return host;
    }

    public void SetApiService(IApiService? apiService = null)
    {
        if (apiService == null)
        {
            Builder.Services.AddSingleton<IApiService, ApiService>();
        }
        else
        {
            Builder.Services.AddSingleton(apiService);
        }
    }

    public void SetDatabase(LiteDatabase? database = null)
    {
        if (database == null)
        {
            Builder.Services.AddSingleton<LiteDatabase>(_ => new LiteDatabase(Path.Combine(AppContext.BaseDirectory, Config.Database)));
        }
        else
        {
            Builder.Services.AddSingleton(database);
        }
    }

    private static void AddGatewayClientEvents(GatewayClient client)
    {
        // Delete guild from DB when bot is removed
        client.GuildDelete += (e) =>
        {
            Repository.Guilds.Delete(e.GuildId.ToString());
            return ValueTask.CompletedTask;
        };

        client.Ready += _ => client.UpdatePresenceAsync(new PresenceProperties(statusType: UserStatusType.Online)
        {
            Activities = [
                new UserActivityProperties(Config.PrecenseStr, UserActivityType.Playing)
            ],
        });
    }
}