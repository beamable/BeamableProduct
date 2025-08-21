using System.CommandLine;
using System.Text;
using Beamable.Server;
using JetBrains.Annotations;

namespace cli.BackendCommands;

public class BackendCommandGroup : CommandGroup
{
    public override bool IsForInternalUse => true;

    public BackendCommandGroup() : base("local", "commands for managing local backend development")
    {
    }

    public static void AddBackendRepoOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, string> binder)
        where TArgs : CommandArgs
    {
        command.AddOption(BackendRepoOption.Instance, binder);
    }
    public static void AddBackendHomeOption<TArgs>(AppCommand<TArgs> command, Action<TArgs, string> binder)
        where TArgs : CommandArgs
    {
        command.AddOption(BackendHomeOption.Instance, (args, i) =>
        {
            var absPath = Path.GetFullPath(i);
            binder(args, absPath);
        });
    }

    public static void ValidateBackendHomeDirectoryExists(string backendHomeDir)
    {
        if (!Directory.Exists(backendHomeDir))
        {
            throw new CliException($"The backend home directory does not exist. path=[{backendHomeDir}]");
        }

        var dirs = Directory.GetDirectories(backendHomeDir).Select(Path.GetFileName).ToList();
        var expectedDirs = new string[]
        {
            "core",
            "bin",
            "tools",
        };

        var files = Directory.GetFiles(backendHomeDir).Select(Path.GetFileName).ToList();
        var expectedFiles = new string[]
        {
            "pom.xml",
            "build.xml"
        };

        var missingDirs = expectedDirs.Except(dirs).ToList();
        var missingFiles = expectedFiles.Except(files).ToList();

        if (missingDirs.Count > 0 || missingFiles.Count > 0)
        {
            throw new CliException(
                $"The backend home directory does not look like a valid Beamable backend scala folder. path=[{backendHomeDir}]");
        }
    }

    
}



public class BackendHomeOption : Option<string>
{
    // TODO: The scala backend itself uses an ENV var called "PLATFORM" to do essentially the same thing.
    //  but "PLATFORM" is also used by MSBuild, so it feels like we should STOP using "PLATFORM" and
    //  converge to something with a BEAM prefix. 
    public const string ENV_VAR = "BEAM_BACKEND_HOME";

    public static BackendHomeOption Instance { get; } = new BackendHomeOption();

    private BackendHomeOption() : base(
        aliases: new string[]{"--backend-home"}, 
        description: $"The path to the local checkout of the Beamable Backend codebase. Control the default value with the {ENV_VAR} environment variable. ",
        getDefaultValue: GetDefaultValue)
    {
        
    }

    public static string GetDefaultValue()
    {
        var env = Environment.GetEnvironmentVariable(ENV_VAR);
        if (!string.IsNullOrEmpty(env))
        {
            return env;
        }
        return ".";
    }
}


public class BackendRepoOption : Option<string>
{
    public const string ENV_VAR = "BEAM_BACKEND_REPO";

    public static BackendRepoOption Instance { get; } = new BackendRepoOption();

    private BackendRepoOption() : base(
        aliases: new string[]{"--backend-repo"}, 
        description: $"The github repository for your Beamable backend. Control the default value with the {ENV_VAR} environment variable.",
        getDefaultValue: GetDefaultValue)
    {
        
    }

    public static string GetDefaultValue()
    {
        var env = Environment.GetEnvironmentVariable(ENV_VAR);
        if (!string.IsNullOrEmpty(env))
        {
            return env;
        }
        return "beamable/BeamableBackend";
    }
}