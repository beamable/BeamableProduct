using System.CommandLine;

namespace cli.BackendCommands;

public class BackendRunToolCommandArgs : CommandArgs
{
    public string backendHome;
    public string[] tools;
}

public class BackendRunToolCommand : AppCommand<BackendRunToolCommandArgs>
{
    public BackendRunToolCommand() : base("tool", "Run a named tool")
    {
    }

    public override void Configure()
    {
        var toolArg = new Argument<string[]>("tool", "The name of the tool to run");
        toolArg.Arity = ArgumentArity.OneOrMore;
        
        AddArgument(toolArg, (args, i) =>
        {
            var allTools = i.SelectMany(tools => tools.Split(new char[] { ',', ';' },
                StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)).ToArray();
            args.tools = allTools;
        });
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override Task Handle(BackendRunToolCommandArgs args)
    {
        // var toolPath = Path.Combine(args.backendHome, args.tool);
        // var needsRecompile = BackendCompileCommand.CheckSourceAndTargets(toolPath);

        // TODO: check if core needs to be re-built.
        
        var toolInfo = BackendListToolsCommand.GatherToolList(args.backendHome);
        // TODO: check if the tool path needs to be rebuilt.
        // TODO: then run these tools.
        //  mvn compile exec:java -Dexec.mainClass=com.disruptorbeam.dbflake.DBFlakeApp
        
        throw new NotImplementedException();
    }

    public static async Task<string> GetClassPathForProject()
    {
        // TODO: must make sure to manually pass JAVA_HOME to the 1.8 address, otherwise a local system or brew may override it in mvn
        //mvn dependency:build-classpath -DincludeScope=runtime
        
        //  mvn compile exec:java -Dexec.mainClass=com.disruptorbeam.dbflake.DBFlakeApp
        throw new NotImplementedException();
    }
}