using System.Diagnostics;
using System.Runtime.InteropServices;
using Beamable.Server;
using CliWrap;
using Spectre.Console;

namespace cli.BackendCommands;

public class SetupOpenJDKCommandArgs : CommandArgs
{
    
}

public class SetupOpenJDKCommandResults
{
    
}

public class SetupOpenJDKCommand : AppCommand<SetupOpenJDKCommandArgs>
{
    public SetupOpenJDKCommand() : base("setup-jdk", "Set up the openJDK for this machine.")
    {
    }

    public override void Configure()
    {
    }

    public override async Task Handle(SetupOpenJDKCommandArgs args)
    {
        const string downloadSite = "https://adoptium.net/temurin/releases/?version=8";

        // var client 
        // CollectorManager.DownloadAndDecompressGzip()

        Log.Information("Checking for java on your system...");
        var hasJava = await CheckJavaOnTerminal(args);

        if (hasJava)
        {
            Log.Information(" Java was found on your system. Run 'java -version' to verify.");
            return;
        }
        
        Log.Information(" Java was not found.");
        var link = GetJavaDownloadLink();
        string installerFileName = Path.GetFileName(link);
        var downloadPath = Path.Combine(Path.GetTempPath(), installerFileName);
        var installerExists = File.Exists(downloadPath);

        if (!installerExists)
        {
            var download = args.Quiet || AnsiConsole.Confirm("Would you like to download and open the java installer?");
            if (!download)
            {
                Log.Information("Please install java manually. ");
                Log.Information($" temurin: {downloadSite}");
                Log.Information($" direct link: {link}");
                return;
            }
            
            var client = new HttpClient();
            await AnsiConsole.Status().Spinner(Spinner.Known.Aesthetic).StartAsync("Downloading...", async ctx =>
            {
                await CollectorManager.DownloadAndDecompressGzip(client, link, downloadPath, true, decompress: false);
            });
            Log.Information("Finished downloading...");
        }
        
        var runInstall = args.Quiet || AnsiConsole.Confirm("Would you like to run the installer?");
        if (!runInstall)
        {
            Log.Information("Please install java manually. ");
            Log.Information($" temurin: {downloadSite}");
            Log.Information($" direct link: {link}");
            Log.Information($" installer path: {downloadPath}");
            return;
        }

        Log.Information("Running java installer...");
        Log.Information(" please complete the installation and re-run this command to verify");
        await RunInstaller(downloadPath);
        
        Log.Information("all done!");
    }

    public static string GetJavaDownloadLink()
    {
        // mac
        const string macDownload =
            "https://github.com/adoptium/temurin8-binaries/releases/download/jdk8u462-b08/OpenJDK8U-jdk_x64_mac_hotspot_8u462b08.pkg";

        // win
        const string winDownload =
            "https://github.com/adoptium/temurin8-binaries/releases/download/jdk8u462-b08/OpenJDK8U-jdk_x64_windows_hotspot_8u462b08.msi";

        var os = CollectorManager.GetCurrentPlatform();
        if (os == OSPlatform.Windows)
        {
            return winDownload;
        } else if (os == OSPlatform.OSX)
        {
            return macDownload;
        }
        else
        {
            throw new CliException(
                $"The CLI only supports getting java download links on windows and osx, but the current os=[{os}]");
        }
    }

    public static async Task RunInstaller(string installerPath)
    {
        var startInfo = new ProcessStartInfo(installerPath);
        startInfo.UseShellExecute = true; // open it as if
        
        var process = Process.Start(startInfo);
        await process.WaitForExitAsync();
    }

    public static async Task<bool> CheckJavaOnTerminal(CommandArgs args)
    {
        
        var command = CliWrap.Cli
            .Wrap(args.AppContext.JavaPath)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x =>
            {
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(x =>
            {
            }))
            .WithArguments("-version")
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