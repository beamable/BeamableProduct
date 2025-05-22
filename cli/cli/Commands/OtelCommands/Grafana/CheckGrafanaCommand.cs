using cli.OtelCommands.Grafana;

namespace cli.Commands.OtelCommands.Grafana;

public class CheckGrafanaCommandArgs : CommandArgs
{
    
}

public class CheckGrafanaCommandResults
{
    public bool isRunning;
    public string url;
    public string containerName;
}

public class CheckGrafanaCommand : AtomicCommand<CheckGrafanaCommandArgs, CheckGrafanaCommandResults>
{
    public CheckGrafanaCommand() : base("ps", "Check if a local Grafana is running for the project")
    {
    }

    public override void Configure()
    {
        
    }

    public override async Task<CheckGrafanaCommandResults> GetResult(CheckGrafanaCommandArgs args)
    {
        var isRunning = await GrafanaCommand.IsGrafanaRunning(args.AppContext);
        return new CheckGrafanaCommandResults
        {
            isRunning = isRunning,
            url = GrafanaCommand.GetGrafanaUrl(args.AppContext),
            containerName = GrafanaCommand.GetGrafanaContainerName(args.AppContext)
        };
    }
}