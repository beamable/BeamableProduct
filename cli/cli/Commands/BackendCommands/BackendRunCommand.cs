using System.CodeDom.Compiler;
using System.CommandLine;
using System.Diagnostics;
using Beamable.Common;
using Beamable.Common.BeamCli;
using Beamable.Server;
using Beamable.Server.Common;
using cli.DockerCommands;
using cli.Utils;
using CliWrap;

namespace cli.BackendCommands;

public class BackendRunToolCommandArgs : CommandArgs, IBackendCommandArgs
{
    public string backendHome;
    public string[] tools;

    public bool runInfra;
    public bool runEssentialTools;
    public string runProfile;

    public bool reset;
    public bool noDeps;
    public bool noStop;
    
    // TODO: add --profile support
    // TODO: add --use-dev-env
    public string BackendHome => backendHome;
}

public class BackendRunPlanResultChannel : IResultChannel
{
    public string ChannelName => "plan";
} 

public class BackendRunCommand 
    : AppCommand<BackendRunToolCommandArgs>
    , IResultSteam<BackendRunPlanResultChannel, BackendRunPlan>
    , ISkipManifest
, IStandaloneCommand
{
    public BackendRunCommand() : base("run", "Run a named tool")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<bool>(new string[] { "--infra", "-i" }, "Specifies to run the infrastructure"),
            (args, i) => args.runInfra = i);

        AddOption(new Option<bool>(new string[] { "--essential", "--essentials", "-e" }, "Run the essential tools"),
            (args, i) => args.runEssentialTools = i);
        
        AddOption(new Option<string[]>(new string[]{"--tool", "--tools", "-t"}, "Specifies which tools to be run")
        {
            Arity = ArgumentArity.OneOrMore
        }, (args, i) =>
        {
            var allTools = i.SelectMany(tools => tools.Split(new char[] { ',', ';' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToArray();
            args.tools = allTools;
        });
        
        AddOption(new Option<bool>(new string[] { "--reset", "-r" }, "Reset any specified components"),
            (args, i) => args.reset = i);
        
        AddOption(new Option<bool>(new string[] { "--no-deps", "-nd" }, "Do not attempt to start the dependencies of the requested components"),
            (args, i) => args.noDeps = i);

        AddOption(new Option<bool>(new string[] { "--no-stop", "-ns" }, "Do not stop existing components"),
            (args, i) => args.noStop = i);
        
        AddOption(new Option<string>(new string[] { "--profile", "-p" }, "Run a specific profile from the docker-compose"),
            (args, i) => args.runProfile = i);
        
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override async Task Handle(BackendRunToolCommandArgs args)
    {
        await DockerStatusCommand.RequireDocker(args);
        // if (!args.Dryrun) throw new NotImplementedException("can only preview changes");
        // var toolPath = Path.Combine(args.backendHome, args.tool);
        // var needsRecompile = BackendCompileCommand.CheckSourceAndTargets(toolPath);
        var toolInfo = BackendListToolsCommand.GatherToolList(args.backendHome);
        var infraStatusTask = BackendPsCommand.CheckInfraStatus(toolInfo, args);
        var toolStatus = await BackendPsCommand.CheckToolStatus(toolInfo, args);
        var infraStatus = await infraStatusTask;

        // if the user doesn't specify anything, default to essentials.
        if (args.tools.Length == 0 && !args.runInfra)
        {
            args.runEssentialTools = true;
        }
        
        var plan = GetRunPlan(args, infraStatus, toolStatus, toolInfo);
        
        this.LogResult(plan);
        this.SendResults(plan);

        if (args.Dryrun)
        {
            // don't actually do anything.
            return;
        }
        
        await RunPlan(args, plan, toolInfo);
    }

    public const string JVM_BEAM_OUT_PROPERTY = "-Dcom.beamable.stdOutRedirection";
    public const string JVM_BEAM_ERR_PROPERTY = "-Dcom.beamable.stdErrRedirection";
    
    public static async Task RunPlan<TArgs>(TArgs args, BackendRunPlan plan, BackendToolList list)
        where TArgs : CommandArgs, IBackendCommandArgs
    {
        if (plan.stopInfra)
        {
            await StopInfra();
        }
        
        if (plan.startInfra)
        {
            await StartInfra();
        }

        if (plan.compileCore)
        {
            Log.Information("Compiling core...");
            Log.Information(" " + list.coreProjectPath);
            await BackendCompileCommand.CompileProject(list.coreProjectPath);
            Log.Information("Finished compiling core.");
        }

        foreach (var phase in plan.toolPhases)
        {
            var tasks = new List<Task>();
            foreach (var action in phase.actions)
            {
                tasks.Add(HandleTool(action));
            }

            await Task.WhenAll(tasks); // TODO: error catching.
        }
        
        Log.Information("all done");

        async Task HandleTool(BackendRunPlanTool action)
        {
            if (action.compile)
            {
                await Compile();
            }

            if (action.generateClassPath)
            {
                await GenerateClassPath();
            }

            if (action.stopPids != null)
            {
                foreach (var pid in action.stopPids)
                {
                    StopTool(pid);
                }
            }

            if (action.start)
            {
                await RunTool();
            }

            async Task Compile()
            {
                Log.Information($"Compiling {action.toolName}...");
                await BackendCompileCommand.CompileProject(action.projectPath);
            }

            async Task GenerateClassPath()
            {
                Directory.CreateDirectory(action.TempPath);
                Log.Information($"Getting classpath for {action.toolName}");
                await JavaUtility.RunMaven($"dependency:build-classpath -Dmdep.outputFile={action.ClassPathFilePath}", 
                    action.projectPath);
            }

            async Task RunTool()
            {
                var classPath = await File.ReadAllTextAsync(action.ClassPathFilePath);
                var separator = ':'; // TODO: on windows this is a ;

                classPath = classPath + separator + "target/classes";
                
                Log.Information(" found classpath");

                Log.Information($"Running {action.toolName}");
                
                // nohup java -cp "target/classes:target/dependency/*" MyClass > myclass.out 2>&1 &

                Directory.CreateDirectory(action.LogPath);
                var logPath = Path.Combine(action.LogPath, $"{DateTimeOffset.Now.ToFileTime()}.log");
                var logErrPath = Path.ChangeExtension(logPath, ".err.log");
                var serverConf = "server.conf"; // TODO: Make this configurable
                var maxMemoryMb = 512;
                var initialMemoryMb = 256;
                var procCount = 2;
                var debugPort = PortUtil.FreeTcpPort();
                var toolArgs = $"sh -c \"{args.AppContext.JavaPath} " +

                               // try to tell the JVM to shut up.
                               $"-Xmx{maxMemoryMb}m " +
                               $"-Xms{initialMemoryMb}m " +
                               $"-XX:ActiveProcessorCount={procCount} " +

                               // allow us to pick when env we connect to?
                               $"-DCom.kickstand.defaultServerConf={serverConf} " +

                               // pass the log redirection
                               $"{JVM_BEAM_OUT_PROPERTY}='{logPath}' " +
                               $"{JVM_BEAM_ERR_PROPERTY}='{logErrPath}' " +
                               
                               // enable the debugger
                               $"-agentlib:jdwp=transport=dt_socket,server=y,suspend=n,address={debugPort} " +
                               
                               // pass in the class path
                               $"-cp '{classPath}' " +
                               
                               // entry point main class
                               $"{action.mainClassName} " +
                               
                               // pip the output to a known file.
                               $"> '{logPath}' 2> '{logErrPath}'\" &";
                var command = Cli.Wrap("nohup")
                    .WithArguments(toolArgs)
                    .WithWorkingDirectory(action.projectPath)
                    .ExecuteAsync();
                var processId = command.ProcessId;
                Log.Information($"Started {action.toolName} as process-id=[{processId}]");
            }
            
            async Task StopTool(int pid)
            {
                Log.Information($"Stopping {action.toolName} at process-id=[{pid}]");

                try
                {
                    await Cli.Wrap("kill")
                        .WithArguments(pid.ToString())
                        .ExecuteAsync();
                }
                catch (Exception ex)
                {
                    Log.Warning($" received error while trying to stop process-id=[{pid}], message=[{ex.Message}]");
                }
            }

        }

        async Task StopInfra()
        {
            const string label = "compose down";
            Log.Information("Stopping infrastructure...");
            await Cli.Wrap("docker")
                .WithArguments("compose --profile core down")
                .WithWorkingDirectory(Path.Combine(args.BackendHome, "docker", "local"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{label}] " + line); }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{label}] " + line); }))
                .ExecuteAsync();
            Log.Information("Stopped infrastructure.");
        }

        async Task StartInfra()
        {
            const string label = "compose up";
            const string mongoClusterSetup = "mongo_cluster_setup";
            const string mongoMasterSetup = "mongo_master_setup";
            Log.Information("Starting infrastructure...");
            await Cli.Wrap("docker")
                .WithArguments("compose --profile core up -d --build")
                .WithWorkingDirectory(Path.Combine(args.BackendHome, "docker", "local"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{label}] " + line); }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{label}] " + line); }))
                .ExecuteAsync();
            Log.Information("Started infrastructure.");
            Log.Information("\nWaiting for mongo cluster...");
            
            await Cli.Wrap("docker")
                .WithArguments($"attach {mongoClusterSetup}")
                .WithWorkingDirectory(Path.Combine(args.BackendHome, "docker", "local"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{mongoClusterSetup}] " + line); }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{mongoClusterSetup}] " + line); }))
                .ExecuteAsync();
            
            Log.Information("\nWaiting for mongo master...");
            
            await Cli.Wrap("docker")
                .WithArguments($"attach {mongoMasterSetup}")
                .WithWorkingDirectory(Path.Combine(args.BackendHome, "docker", "local"))
                .WithStandardOutputPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{mongoMasterSetup}] " + line); }))
                .WithStandardErrorPipe(PipeTarget.ToDelegate(line => { Log.Information($" [{mongoMasterSetup}] " + line); }))
                .ExecuteAsync();
            
            Log.Information("Started infrastructure.");
        }
    }

    public static BackendRunPlan GetRunPlan(BackendRunToolCommandArgs args, BackendPsInfraStatus infraStatus, BackendPsToolStatus toolStatus, BackendToolList list)
    {
        var plan = new BackendRunPlan();

        if (args.runInfra)
        {
            if (infraStatus.AllRunning)
            {
                plan.startInfra = false;
                if (args.reset)
                {
                    plan.stopInfra = true;
                    plan.startInfra = true;
                }
            }
            else
            {
                plan.startInfra = true;
            }
        }
        
        if (args.runEssentialTools)
        {
            if (!infraStatus.AllRunning && !args.noDeps)
            {
                plan.startInfra = true;
            }
            
            var essentials = list.tools
                .Where(t => t.profiles.Contains("essential"))
                .Select(t => t.name)
                .ToArray();
            var phase = CreatePhase(essentials);
            plan.toolPhases.Add(phase);
        }
        
        if (args.tools.Length > 0)
        {
            var phase = CreatePhase(args.tools);
            plan.toolPhases.Add(phase);
        }

        BackendRunPlanToolCollection CreatePhase(string[] tools)
        {
            // we only need to consider the core if there are tools being started. 
            plan.compileCore = BackendCompileCommand.CheckSourceAndTargets(list.coreProjectPath, out _);
            
            var phase = new BackendRunPlanToolCollection();
            foreach (var toolName in tools)
            {
                var tool = list.tools.FirstOrDefault(t => toolName == t.name);
                if (tool == null)
                {
                    throw new CliException($"Cannot find tool=[{toolName}]");
                }

                var needsCompile = BackendCompileCommand.CheckSourceAndTargets(tool.projectPath, out var latestSourceTime);

                var classPathFile = BackendRunPlanTool.GetClassPathFilePath(tool.projectPath);
                var classPathFileMissing = !File.Exists(classPathFile);
                var needsClassPath = classPathFileMissing;
                if (!classPathFileMissing)
                {
                    var writeTime = new DateTimeOffset(File.GetLastWriteTime(classPathFile));
                    needsClassPath = latestSourceTime > writeTime;
                }
                
                
                var existingProcesses = toolStatus.tools.Where(t => t.isRunning && t.toolName == tool.name).ToList();
                var stopPids = args.noStop 
                    ? Array.Empty<int>() 
                    : existingProcesses.Select(x => x.processId).ToArray();
                if (existingProcesses.Count == 0 || (existingProcesses.Count > 0 && args.reset))
                {
                    phase.actions.Add(new BackendRunPlanTool
                    {
                        toolName = tool.name,
                        mainClassName = tool.mainClassName,
                        projectPath = tool.projectPath,
                        start = true,
                        generateClassPath = needsClassPath,
                        compile = needsCompile,
                        stopPids = stopPids
                    });
                }

            }
            return phase;
        }

        return plan;
    }

    public static void RunInfra()
    {
        
    }

    public static void RunScript()
    {
        
    }
    
}

public class BackendRunPlan
{
    public bool stopInfra;
    public bool startInfra;
    public bool compileCore;
    
    public List<BackendRunPlanToolCollection> toolPhases = new List<BackendRunPlanToolCollection>();
}

public class BackendRunPlanToolCollection
{
    public List<BackendRunPlanTool> actions = new List<BackendRunPlanTool>();
}

public class BackendRunPlanTool
{
    public string toolName;
    public string mainClassName;
    public string projectPath;
    public int[] stopPids;
    public bool compile;
    public bool generateClassPath;
    public bool start;

    public static string GetTargetsPath(string projectPath) => Path.Combine(projectPath, "target");
    public static string GetTempPath(string projectPath) => Path.Combine(GetTargetsPath(projectPath), "beamTemp");
    public static string GetClassPathFilePath(string projectPath) => Path.Combine(GetTempPath(projectPath), "classPath.txt");
    public static string GetLogPath(string projectPath) => Path.Combine(GetTempPath(projectPath), "logs");

    public string TargetsPath => GetTargetsPath(projectPath);
    public string TempPath => GetTempPath(projectPath);
    public string ClassPathFilePath => GetClassPathFilePath(projectPath);
    public string LogPath => GetLogPath(projectPath);
}