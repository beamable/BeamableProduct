namespace cli.BackendCommands;

public class SetupBackendCommandArgs : CommandArgs
{
    public string repoPath;
}

public class SetupBackendCommandResults
{
    
}


public class SetupBackendCommand 
    : StreamCommand<SetupBackendCommandArgs, SetupBackendCommandResults>
    , ISkipManifest
{
    public SetupBackendCommand() : base("setup", "Set up the backend project")
    {
    }

    public override void Configure()
    {
        // AddArgument()
    }

    public override Task Handle(SetupBackendCommandArgs args)
    {
        // TODO: checkout the source code?
        
        // TODO: validate dependencies.
        // - OpenJDK
        
        return Task.CompletedTask;
    }
}