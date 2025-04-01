using System.Diagnostics;
using Serilog;

namespace cli.Services;

public interface IFileOpenerService
{
    Task OpenFileWithDefaultApp(string filePath);
}
public class FileOpenerService : IFileOpenerService
{
    public async Task OpenFileWithDefaultApp(string filePath)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true // Important for launching with default app
                }
            };
            
            var success = process.Start();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            Log.Information("Opened : " + success);
        }
        catch (Exception ex)
        {
            Log.Error("Failed to open program: " +ex.Message);
        }
    }
}