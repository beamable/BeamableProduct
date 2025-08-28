using System.CommandLine;
using Beamable.Common.BeamCli;
using Beamable.Server;

namespace cli.BackendCommands;

public class BackendInspectToolCommandArgs : CommandArgs, IBackendCommandArgs
{
    public string searchTerm;
    public string backendHome;

    public string BackendHome => backendHome;
}

public class BackendInspectToolResults
{
    public BackendToolInfo tool;
    public List<BackendToolRuntimeInfo> instances = new List<BackendToolRuntimeInfo>();
}
public class BackendInspectInfraResults
{
    public BackendInfraInfo infra;
    public List<BackendDockerContainerInfo> containers = new List<BackendDockerContainerInfo>();
}

public class BackendInspectResults
{
    public BackendInspectToolResults tool;
    public BackendInspectInfraResults infra;
}

public class BackendInspectToolResultChannel : IResultChannel
{
    public string ChannelName => "toolResult";
}
public class BackendInspectInfraResultChannel : IResultChannel
{
    public string ChannelName => "infraResult";
}

public class BackendInspectToolCommand
    : AppCommand<BackendInspectToolCommandArgs>
    , ISkipManifest
    , IStandaloneCommand
    , IResultSteam<BackendInspectToolResultChannel, BackendInspectToolResults>
    , IResultSteam<BackendInspectInfraResultChannel, BackendInspectInfraResults>
{
    public BackendInspectToolCommand() : base("inspect", "inspect a backend component")
    {
    }

    public override void Configure()
    {
        AddArgument(
            new Argument<string>("component", "the process id, container id, or name of the component to inspect"),
            (args, i) => args.searchTerm = i);
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);

    }

    public override async Task Handle(BackendInspectToolCommandArgs args)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(args.backendHome);
        var list = BackendListToolsCommand.GatherToolList(args.backendHome);

        var res = await Inspect(list, args, args.searchTerm);

        if (res.tool != null)
        {
            this.LogResult(res.tool);
            this.SendResults<BackendInspectToolResultChannel, BackendInspectToolResults>(res.tool);
        }
        if (res.infra != null)
        {
            this.LogResult(res.infra);
            this.SendResults<BackendInspectInfraResultChannel, BackendInspectInfraResults>(res.infra);
        }

        if (res.tool == null && res.infra == null)
        {
            Log.Information("No component found. ");
        }
    }

    public static async Task<BackendInspectResults> Inspect(BackendToolList list, CommandArgs args, string term)
    {
        var results = new BackendInspectResults();
        var matchedInfra = list.infra.FirstOrDefault(i => i.name == term);
        BackendPsInfraStatus infraStatus = null;
        
        // first try to match by infra name.
        if (matchedInfra != null)
        {
            infraStatus = await BackendPsCommand.CheckInfraStatus(list, args);
            var containers = infraStatus.coreServices.Where(t => t.service == matchedInfra.name).ToList();
            results.infra = new BackendInspectInfraResults
            {
                infra = matchedInfra,
                containers = containers
            };
            return results;
        }

        // then, assume its likely a tool, and get the tool status
        var toolStatus = await BackendPsCommand.CheckToolStatus(list, args);

        // try matching by the tool name
        var matchedTool = list.tools.FirstOrDefault(i => i.name == term);
        if (matchedTool != null)
        {
            var instances = toolStatus.tools.Where(t => t.toolName == term).ToList();
            results.tool = new BackendInspectToolResults
            {
                tool = matchedTool,
                instances = instances
            };
            return results;
        }

        // and try matching by the tool process id
        var matchedStatus = toolStatus.tools.FirstOrDefault(t => t.processId.ToString() == term);
        if (matchedStatus != null)
        {
            matchedTool = list.tools.FirstOrDefault(t => t.name == matchedStatus.toolName);
            results.tool = new BackendInspectToolResults
            {
                tool = matchedTool,
                instances = new List<BackendToolRuntimeInfo> { matchedStatus }
            };
            return results;
        }
        
        // and if those didn't work, the last case is a container id, so get the container status
        infraStatus = await BackendPsCommand.CheckInfraStatus(list, args);
        var matchedInfraStatus = infraStatus.coreServices.FirstOrDefault(c => c.containerId.StartsWith(term));
        if (matchedInfraStatus != null)
        {
            results.infra = new BackendInspectInfraResults
            {
                infra = list.infra.FirstOrDefault(i => i.name == matchedInfraStatus.service),
                containers = new List<BackendDockerContainerInfo>{matchedInfraStatus}
            };
            return results;
        }
        
        // otherwise there is nothing :(
        
        return results;
    }

}