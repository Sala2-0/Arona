namespace Arona.Utility;
using System.Diagnostics;

public class JsUtility
{
    public static ProcessStartInfo StartJs(string scriptName, string args = "") // args kan vara flera argument separerad med blanksteg
    {
        string path = Path.Combine("..", "..", "..", "Database", "dist", "Database", "Scripts", scriptName);
        
        return new ProcessStartInfo
        {
            FileName = "node",
            Arguments = $"{path} {args}",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = System.Text.Encoding.UTF8,
        };
    }

    public static async Task<bool> CheckJsErrorAsync<T>(T? obj, Func<Task> errorHandler)
    {
        // T obj bör vara en node process eller utmatning från en node process
        if (obj == null)
        {
            await errorHandler();
            return true; // Error uppstod
        }
        return false;
    }
}