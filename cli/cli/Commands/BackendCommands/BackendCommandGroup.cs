using System.CommandLine;
using System.Text;
using Beamable.Server;
using JetBrains.Annotations;

namespace cli.BackendCommands;

public interface IBackendCommandArgs
{
    public string BackendHome { get; }
}

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
        if (!TryValidateBackendHomeDirectoryExists(backendHomeDir, out var message))
        {
            throw new CliException($"{message}\n" +
                                   $"Pass the --backend-home option, or navigate to the backend directory.\n" +
                                   $"path=[{backendHomeDir}]");
        }
    }
    public static bool TryValidateBackendHomeDirectoryExists(string backendHomeDir, out string message)
    {
        message = null;
        if (!Directory.Exists(backendHomeDir))
        {
            message = "The backend home directory does not exist. ";
            return false;
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
            message = "The backend home directory does not look like a valid Beamable backend scala folder. ";
            return false;
        }

        return true;
    }

    
}



public class BackendHomeOption : Option<string>
{
    public static BackendHomeOption Instance { get; } = new BackendHomeOption();

    private BackendHomeOption() : base(
        aliases: new string[]{"--backend-home"}, 
        description: $"The path to the local checkout of the Beamable Backend codebase. ",
        getDefaultValue: GetDefaultValue)
    {
        
    }

    public static string GetDefaultValue()
    {
        var thisFolder = Path.GetFullPath(".");
        var current = thisFolder;

        while (!BackendCommandGroup.TryValidateBackendHomeDirectoryExists(current, out _))
        {
            current = Path.GetDirectoryName(current);
            if (current == null)
            {
                return thisFolder;
            }
        }
        
        return Path.GetFullPath(current);
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
