namespace cli.BackendCommands;

public class BackendCompileCommandArgs : CommandArgs
{
    public string backendHome;
    
}

public class BackendCompileCommand : AppCommand<BackendCompileCommandArgs>
{
    //mvn install -DskipTests=true
    // 
    public BackendCompileCommand() : base("compile", "Compile a scala project")
    {
    }

    public override void Configure()
    {
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override Task Handle(BackendCompileCommandArgs args)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(args.backendHome);

        var core = Path.Combine(args.backendHome, "core");
        var needsToRebuild = CheckSourceAndTargets(core);
        
        throw new NotImplementedException();
    }

    public static bool CheckSourceAndTargets(string projectPath)
    {
        var srcFolder = Path.Combine(projectPath, "src");
        var targetFolder = Path.Combine(projectPath, "target");

        var latestSrc = GetLatestFileWriteTimeFromFolder(srcFolder);
        var latestBuild = GetLatestFileWriteTimeFromFolder(targetFolder);

        if (latestSrc > latestBuild)
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