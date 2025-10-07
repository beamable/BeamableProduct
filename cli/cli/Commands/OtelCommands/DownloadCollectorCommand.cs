using System.CommandLine;
using System.Runtime.InteropServices;
using cli.Services;

namespace cli.OtelCommands;


public class DownloadCollectorCommandArgs : CommandArgs
{
    public string platform;
    public string arch;
}

public class DownloadCollectorCommandResults
{
    public string path;
    public string configPath;
}

public class DownloadCollectorCommand : AtomicCommand<DownloadCollectorCommandArgs, DownloadCollectorCommandResults>, IIgnoreLogin
{
    public const string OS_LINUX = "lin";
    public const string ARCH_X64 = "x64";
    
    public DownloadCollectorCommand() : base("get", "Downloads the otel collector")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<string>("--platform", "The platform for the collector executable. [osx, win, lin] or defaults to system"),
            (args, i) => args.platform = i);
        
        AddOption(new Option<string>("--arch", "The architecture for the collector executable. [arm64, x64] or defaults to system"),
            (args, i) => args.arch = i);
    }

    public override async Task<DownloadCollectorCommandResults> GetResult(DownloadCollectorCommandArgs args)
    {

        var platform = args.platform switch
        {
            OS_LINUX => OSPlatform.Linux,
            "linux" => OSPlatform.Linux,
            "win" => OSPlatform.Windows,
            "windows" => OSPlatform.Windows,
            "mac" => OSPlatform.OSX,
            "osx" => OSPlatform.OSX,
            _ => CollectorManager.GetCurrentPlatform()
        };
        var arch = args.arch switch
        {
            ARCH_X64 => Architecture.X64,
            "arm64" => Architecture.Arm64,
            _ =>  RuntimeInformation.OSArchitecture
        };
        
        var basePath = CollectorManager.GetCollectorBasePathForCli();
        var info = await CollectorManager.ResolveCollector(basePath, true, platform, arch);

        return new DownloadCollectorCommandResults
        {
            path = info.filePath,
            configPath = info.configFilePath
        };
    }
}
