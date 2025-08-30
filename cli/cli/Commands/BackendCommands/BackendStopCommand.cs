using System.CommandLine;
using cli.DockerCommands;

namespace cli.BackendCommands;

public class BackendStopCommandArgs : CommandArgs, IBackendCommandArgs
{
    public string backendHome;
    public string[] tools;

    public bool stopInfra;
    public bool stopEssentialTools;

    public int processId;
    public bool noDeps;
    
    // TODO: add --profile support
    public string BackendHome => backendHome;
}

public class BackendStopCommand 
    : AppCommand<BackendStopCommandArgs>
    , IResultSteam<BackendRunPlanResultChannel, BackendRunPlan>
    , ISkipManifest
    ,IStandaloneCommand

{
    public BackendStopCommand() : base("stop", "stop local beamable components")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<bool>(new string[] { "--infra", "-i" }, "Specifies to run the infrastructure"),
            (args, i) => args.stopInfra = i);

        AddOption(new Option<bool>(new string[] { "--essential", "--essentials", "-e" }, "Stop the essential tools"),
            (args, i) => args.stopEssentialTools = i);
        
        AddOption(new Option<string[]>(new string[]{"--tool", "--tools", "-t"}, "Specifies which tools to be stopped")
        {
            Arity = ArgumentArity.OneOrMore
        }, (args, i) =>
        {
            var allTools = i.SelectMany(tools => tools.Split(new char[] { ',', ';' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToArray();
            args.tools = allTools;
        });
        
        AddOption(new Option<int>(new string[] { "--process-id", "--proc" }, "Stop a given process id"),
            (args, i) => args.processId = i);

        
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override async Task Handle(BackendStopCommandArgs args)
    {
        await DockerStatusCommand.RequireDocker(args);

        var toolInfo = BackendListToolsCommand.GatherToolList(args.backendHome);
        var infraStatusTask = BackendPsCommand.CheckInfraStatus(toolInfo, args);
        var toolStatus = await BackendPsCommand.CheckToolStatus(toolInfo, args);
        var infraStatus = await infraStatusTask;

        if (!args.stopInfra && !args.stopEssentialTools && args.tools.Length == 0 && args.processId <= 0)
        {
            args.stopInfra = true;
            args.tools = toolStatus.tools.Select(t => t.toolName).ToArray();
        }
        
        var plan = GetStopPlan(args, infraStatus, toolStatus, toolInfo);

        
        this.LogResult(plan);
        this.SendResults(plan);

        if (args.Dryrun)
        {
            // don't actually do anything.
            return;
        }

        await BackendRunCommand.RunPlan(args, plan, toolInfo);

    }

    public static BackendRunPlan GetStopPlan(
        BackendStopCommandArgs args, 
        BackendPsInfraStatus infraStatus,
        BackendPsToolStatus toolStatus, 
        BackendToolList list)
    {
        var plan = new BackendRunPlan();
        if (args.stopInfra)
        {
            plan.stopInfra = true;
        }

        if (args.stopEssentialTools)
        {
            var essentials = list.tools
                .Where(t => t.profiles.Contains("essential"))
                .Select(t => t.name)
                .ToArray();
            plan.toolPhases.Add(CreatePhase(essentials));

        }

        if (args.processId > 0)
        {
            var matching = toolStatus.tools
                .Where(t => t.processId == args.processId)
                .Select(t => t.toolName)
                .ToArray();
            plan.toolPhases.Add(CreatePhase(matching));
        }
        
        if (args.tools.Length > 0)
        {
            plan.toolPhases.Add(CreatePhase(args.tools));
        }

        BackendRunPlanToolCollection CreatePhase(string[] tools)
        {
            var phase = new BackendRunPlanToolCollection();

            foreach (var toolName in tools)
            {
                var tool = list.tools.FirstOrDefault(t => toolName == t.name);
                if (tool == null)
                {
                    throw new CliException($"Cannot find tool=[{toolName}]");
                }
                var existingProcesses = toolStatus.tools.Where(t => t.isRunning && t.toolName == tool.name).ToList();
                var stopPids = existingProcesses.Select(x => x.processId).ToArray();

                phase.actions.Add(new BackendRunPlanTool
                {
                    toolName = toolName,
                    stopPids = stopPids
                });
            }

            return phase;
        }

        return plan;
    }
}