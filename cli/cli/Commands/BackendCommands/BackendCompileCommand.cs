using CliWrap;

namespace cli.BackendCommands;

public class BackendCompileCommandArgs : CommandArgs
{
    public string backendHome;
    
}

public class BackendCompileCommand 
    : AppCommand<BackendCompileCommandArgs>
        , ISkipManifest
        ,IStandaloneCommand
{
    public BackendCompileCommand() : base("compile", "Compile a scala project")
    {
    }

    public override void Configure()
    {
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override async Task Handle(BackendCompileCommandArgs args)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(args.backendHome);
        await CompileProject(args.backendHome);
    }


    public static async Task CompileProject(string projectPath)
    {
        await JavaUtility.RunMaven("install -DskipTests=true", projectPath);
    }

    public static bool CheckSourceAndTargets(string projectPath, out DateTimeOffset latestSourceTime)
    {
        var srcFolder = Path.Combine(projectPath, "src");
        var targetFolder = Path.Combine(projectPath, "target", "classes");

        latestSourceTime = GetLatestFileWriteTimeFromFolder(srcFolder);
        var latestBuildTime = GetLatestFileWriteTimeFromFolder(targetFolder);

        if (latestSourceTime > latestBuildTime)
        {
            // likely need to re-build.
            //  note, this won't care if a file is the same content as before; but its better than nothing.
            return true;
        }

        return false;
    }
    
    public static DateTimeOffset GetLatestFileWriteTimeFromFolder(string folder)
    {
        var latestWriteTime = Directory
            .EnumerateFiles(folder, "*", SearchOption.AllDirectories)
            .Select(File.GetLastWriteTimeUtc)
            .DefaultIfEmpty(DateTime.MinValue)
            .Max();
        return new DateTimeOffset(latestWriteTime);
    }
}