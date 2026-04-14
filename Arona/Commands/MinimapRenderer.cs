using System.Diagnostics;
using System.Text.RegularExpressions;
using Arona.Services.Message;
using NetCord;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;

namespace Arona.Commands;

public class MinimapRenderer : ApplicationCommandModule<ApplicationCommandContext>
{
    [SlashCommand("render", "Render a WoWs replay file into a minimap video")]
    public async Task RenderAsync(Attachment replayFile)
    {
        var fileExtension = replayFile.FileName.Split('.').Last();
        if (fileExtension != "wowsreplay")
        {
            await Context.Interaction.SendResponseAsync(InteractionCallback.Message("Not a replay file."));
        }
        
        var deferredMessage = await DeferredMessage.CreateAsync(Context.Interaction);
        
        var tempDir = Path.GetTempPath();
        var tempFile = Path.Combine(tempDir, $"{Guid.NewGuid()}.wowsreplay");
        
        await File.WriteAllBytesAsync(tempFile, await GetReplayByteData(replayFile.Url));
        await using var outputFileStream = await RenderAsync(deferredMessage, tempFile);

        if (outputFileStream == null)
        {
            return;
        }
        
        await deferredMessage.EditAsync(new MessageProperties()
            .WithAttachments([new AttachmentProperties("Replay.mp4", outputFileStream)]));
        
        if (File.Exists(Path.Combine(AppContext.BaseDirectory, "Replay.mp4")))
        {
            File.Delete(Path.Combine(AppContext.BaseDirectory, "Replay.mp4"));
        }
    }

    private static async Task<FileStream?> RenderAsync(DeferredMessage deferredMessage, string filePath)
    {
        try
        {
            var gameVersionsPath = Debugger.IsAttached
                ? Path.Combine(AppContext.BaseDirectory, "GameVersions")
                : Config.GameVersionsPath;
            
            var psi = new ProcessStartInfo
            {
                FileName = Path.Combine(AppContext.BaseDirectory, "minimap_renderer"),
                Arguments = $"--cpu --extracted-dir {gameVersionsPath} --output Replay.mp4 {filePath}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };
            
            using var process = new Process { StartInfo = psi };
            process.Start();
            
            _ = Task.Run(async () =>
            {
                var timer = new Stopwatch();
                timer.Start();
                
                while (!process.HasExited)
                {
                    await deferredMessage.EditAsync(new MessageProperties()
                        .WithContent($"**Rendering...** ({Math.Round(timer.Elapsed.TotalSeconds, 1)} s)"));
                    
                    await Task.Delay(500);
                }
            });
            await process.WaitForExitAsync();

            if (!File.Exists(Path.Combine(AppContext.BaseDirectory, "Replay.mp4")))
            {
                var errorOutput = await process.StandardError.ReadToEndAsync();
                const string pattern = "\"([^\"]*)\"";
                
                var match = Regex.Match(errorOutput, pattern);

                if (match.Success)
                {
                    var extracted = match.Groups[1].Value;
                    await deferredMessage.EditAsync(new MessageProperties()
                        .WithContent($"**Error** :x:\n{extracted}"));
                }
                else
                {
                    await deferredMessage.EditAsync(new MessageProperties()
                        .WithContent("**Error** :x:\nInternal Error"));
                }

                return null;
            }

            await Task.Delay(500);
            await deferredMessage.EditAsync(new MessageProperties().WithContent("**Done!** :white_check_mark:"));
            
            return new FileStream(Path.Combine(AppContext.BaseDirectory, "Replay.mp4"), FileMode.Open);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
        finally
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        
        return null;
    }

    private static async Task<byte[]> GetReplayByteData(string replayUrl)
    {
        using var httpClient = new HttpClient();
        
        return await httpClient.GetByteArrayAsync(replayUrl);
    }
}