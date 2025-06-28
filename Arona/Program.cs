namespace Arona;
using System.Text.Json;
using Config;
using Microsoft.Extensions.Hosting;
using NetCord.Hosting.Gateway;
using NetCord.Hosting.Services;
using NetCord.Hosting.Services.ApplicationCommands;
using Utility;
using System.Timers;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;

class Program
{
    static async Task Main(string[] args)
    {
        var config = JsonSerializer.Deserialize<BotConfig>(BotConfig.GetConfigFilePath());

        var builder = Host.CreateApplicationBuilder(args);
        builder.Services
            .AddDiscordGateway(options =>
            {
                options.Token = config!.Token;
            })
            .AddApplicationCommands();
        
        var host = builder.Build();
        host.AddModules(typeof(Program).Assembly);
        host.UseGatewayHandlers();
        
        var client = host.Services.GetRequiredService<GatewayClient>();
        
        // Varje 5 minuter, hämta API och kolla klanaktiviteter
        Timer MONITOR_CLANS = new Timer(60000); // 300000
        MONITOR_CLANS.Elapsed += async (sender, e) =>
        {
            // Skript för att kolla klanaktiviteter
            var psi = JsUtility.StartJs("UpdateClan.js");
            var process = Process.Start(psi);
            
            string output = await process!.StandardOutput.ReadToEndAsync();
            JsonElement doc = JsonDocument.Parse(output).RootElement;

            Console.WriteLine(doc);
            
            foreach (JsonElement guild in doc.EnumerateArray())
            {
                ulong channelId = ulong.Parse(guild.GetProperty("channel_id").GetString()!);
            
                if (guild.GetProperty("type").GetString() == "Started playing")
                    await client.Rest.SendMessageAsync(channelId, guild.GetProperty("message").GetString()!);
                
                else if (guild.GetProperty("type").GetString() == "Finished battle")
                {
                    var embed = new EmbedProperties()
                        .WithTitle(guild.GetProperty("message").GetProperty("title").GetString()!)
                        .WithColor(new Color(Convert.ToInt32(guild.GetProperty("message").GetProperty("color").GetString()!, 16)))
                        .AddFields(
                            new EmbedFieldProperties()
                                .WithName("Time")
                                .WithValue($"<t:{guild.GetProperty("message").GetProperty("time").GetInt64().ToString()}:f>")
                                .WithInline(false),
                            new EmbedFieldProperties()
                                .WithName("Team")
                                .WithValue(guild.GetProperty("message").GetProperty("team").GetString()!)
                                .WithInline(false),
                            new EmbedFieldProperties()
                                .WithName(guild.GetProperty("message").GetProperty("game_result").GetString()!)
                                .WithValue(guild.GetProperty("message").GetProperty("points").GetString()!)
                                .WithInline(false),
                            new EmbedFieldProperties()
                                .WithName("Result")
                                .WithValue(guild.GetProperty("message").GetProperty("result").GetString()!)
                                .WithInline(false)
                        );

                    await client.Rest.SendMessageAsync(channelId, new MessageProperties()
                        .WithEmbeds([embed]));
                }
                
                else if (guild.GetProperty("type").GetString() == "Members playing")
                {
                    var embed = new EmbedProperties()
                        .WithTitle(guild.GetProperty("message").GetProperty("title").GetString()!)
                        .AddFields(
                            new EmbedFieldProperties()
                                .WithValue(guild.GetProperty("message").GetProperty("players").GetString()!)
                                .WithInline(false)
                        );
                    
                    await client.Rest.SendMessageAsync(channelId, new MessageProperties()
                        .WithEmbeds([embed]));
                }
            }
        };
        MONITOR_CLANS.AutoReset = true;
        MONITOR_CLANS.Enabled = true;
        MONITOR_CLANS.Start();
        
        await host.RunAsync();
    }
}