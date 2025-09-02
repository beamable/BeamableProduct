using System.Diagnostics;
using System.Runtime.InteropServices;
using Beamable.Server;
using CliWrap;
using Spectre.Console;

namespace cli.BackendCommands;

public static class BrewUtility
{
    public static async Task EnsureBrew(CommandArgs args)
    {
        var os = CollectorManager.GetCurrentPlatform();
        if (os != OSPlatform.OSX) return;

        var hasBrew = await CheckBrew();
        if (hasBrew) return;

        await InstallBrew(args);
    }

    public static async Task RunBrew(string args)
    {
        await RunBrew("brew", args);
    }
    public static async Task RunBrew(string program, string args)
    {
        Log.Information($"Running `{program} {args}`");
        await Cli.Wrap(program)
            .WithArguments(args)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Information("\t" + line);
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Error("\t" + line);
            }))
            .ExecuteAsync();
    }
    
    public static async Task InstallBrew(CommandArgs args)
    {
        var os = CollectorManager.GetCurrentPlatform();
        if (os != OSPlatform.OSX) throw new CliException("Brew can only install on OSX");
        
        const string downloadPath = "https://github.com/Homebrew/brew/releases/download/4.6.4/Homebrew-4.6.4.pkg";

        var fileName = Path.GetFileName(downloadPath);
        var targetFilePath = Path.Combine(Path.GetTempPath(), fileName);

        if (!File.Exists(targetFilePath))
        {
            var shouldDownload = args.Quiet || AnsiConsole.Confirm("Would you like to download the Brew installer?");
            if (!shouldDownload)
            {
                Log.Information("Please install brew manually");
                Log.Information($" download link: {downloadPath}");
                return;
            }
            await AnsiConsole.Status().Spinner(Spinner.Known.Default).StartAsync("Downloading Brew...", async _ =>
            {
                await CollectorManager.DownloadAndDecompressGzip(new HttpClient(), downloadPath, targetFilePath, true, false);
            });
        }

        var shouldInstall = args.Quiet || AnsiConsole.Confirm("Would you like to run the Brew installer?");
        if (!shouldInstall)
        {
            Log.Information("Please install brew manually");
            Log.Information($" download link: {downloadPath}");
            Log.Information($" file: {targetFilePath}");
            return;
        }

        if (!await CheckBrewDeps())
        {
            Log.Information("Brew requires the XCode Command Line Tools... ");
            return;
        }
        
        Log.Information("Running Brew installer. Please re-run the command when it is complete.");
        var proc = Process.Start(new ProcessStartInfo
        {
            FileName = targetFilePath,
            UseShellExecute = true
        });
        await proc.WaitForExitAsync();
    }

    public static async Task<bool> CheckBrewDeps()
    {
        var psi = new ProcessStartInfo
        {
            FileName = "bash",
            Arguments = "-c \"pkgutil --pkg-info=com.apple.pkg.CLTools_Executables >/dev/null 2>&1\"",
            RedirectStandardOutput = false,
            RedirectStandardError = false,
            UseShellExecute = false
        };

        var proc = Process.Start(psi);
        await proc.WaitForExitAsync();
        var hasCLT = proc.ExitCode == 0;

        if (!hasCLT)
        {
            Log.Information("Please install the XCode Command Line Tools and try again.");
            Process.Start(new ProcessStartInfo
            {
                FileName = "xcode-select",
                Arguments = "--install",
                UseShellExecute = true
            });
        }
        return hasCLT;
    }

    public static async Task<bool> CheckBrew()
    {
        var command = Cli.Wrap("brew")
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x =>
            {
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(x =>
            {
            }))
            .WithArguments("--version")
            .WithValidation(CommandResultValidation.None);
        
        
        try
        {
            var res = command.ExecuteAsync();
            await res;
            var success = res.Task.Result.ExitCode == 0;
            if (!success)
            {
                return false;
            }

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}