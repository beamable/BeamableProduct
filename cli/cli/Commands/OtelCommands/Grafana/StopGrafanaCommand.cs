using cli.OtelCommands.Grafana;

namespace cli.Commands.OtelCommands.Grafana;

public class StopGrafanaCommandArgs : CommandArgs
{
    
}

public class StopGrafanaCommand : AtomicCommand<StopGrafanaCommandArgs, CheckGrafanaCommandResults>
{
    public StopGrafanaCommand() : base("stop", "Stop the locally running Grafana")
    {
    }

    public override void Configure()
    {
        
    }

    public override async Task<CheckGrafanaCommandResults> GetResult(StopGrafanaCommandArgs args)
    {
        await GrafanaCommand.StopGrafana(args.AppContext);
        var isRunning = await GrafanaCommand.IsGrafanaRunning(args.AppContext);
        return new CheckGrafanaCommandResults
        {
            isRunning = isRunning,
            url = GrafanaCommand.GetGrafanaUrl(args.AppContext),
            containerName = GrafanaCommand.GetGrafanaContainerName(args.AppContext)
        };
    }
}