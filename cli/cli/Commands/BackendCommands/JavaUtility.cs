using System.CommandLine.Parsing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Beamable.Server;
using CliWrap;

namespace cli.BackendCommands;

public static class JavaUtility
{
    const string JAVA_HOME_ENV_VAR = "JAVA_HOME";

    public static async Task<string> GetJavaHome()
    {
        var existingConfig = Environment.GetEnvironmentVariable(JAVA_HOME_ENV_VAR);
        if (!string.IsNullOrEmpty(existingConfig))
        {
            return existingConfig;
        }

        if (CollectorManager.GetCurrentPlatform() != OSPlatform.OSX)
        {
            // TODO: make this work on windows too!
            throw new CliException("Resolving JAVA_HOME only works on OSX right now");
        }
        // the user does not have a JAVA_HOME set, so we need to find the one.

        await Cli.Wrap("/usr/libexec/java_home")
            .WithStandardOutputPipe(PipeTarget.ToDelegate(x => existingConfig = x))
            .ExecuteAsync();
        return existingConfig;
    }

    public static async Task<JpsResult> FindDebuggables()
    {
        var javaHome = await GetJavaHome();

        var args = 
            "-v " + // show the JVM arguments
            "-l " // show the full main class
            ;

        var buffer = new StringBuilder();
        
        // https://docs.oracle.com/en/java/javase/11/tools/jps.html
        Log.Debug($"Running `jps {args}`");
        var task = await Cli.Wrap("jps")
            .WithArguments(args)
            .WithEnvironmentVariables(new Dictionary<string, string>
            {
                [JAVA_HOME_ENV_VAR] = javaHome
            })
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(buffer))
            .ExecuteAsync();

        Log.Debug(" " + buffer);
        return ExtractJpsData(buffer.ToString());
    }

    public static JpsResult ExtractJpsData(string data)
    {
        var lines = data.Split(Environment.NewLine,
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        var results = new JpsResult();
        foreach (var line in lines)
        {
            var entry = new JpsEntry();
            var i = 0;
            var pidBreakIndex = 0;
            var mainClassBreakIndex = 0;
            
            // search for the pid
            for (;i < line.Length; i++)
            {
                if (!char.IsDigit(line, i))
                {
                    // found the pid!
                    pidBreakIndex = i;
                    entry.processId = int.Parse(line.AsSpan(0, pidBreakIndex));
                    break;
                }
            }
            
            i ++;
            
            // search for the main class
            for (; i < line.Length; i++)
            {
                if (i == line.Length - 1 || char.IsWhiteSpace(line, i))
                {
                    // found the main class
                    mainClassBreakIndex = i;
                    entry.mainClass = line.AsSpan(pidBreakIndex + 1, i - (pidBreakIndex + 1)).ToString();
                    break;
                }
            }

            i++;
            // the rest of the args
            var split = CommandLineStringSplitter.Instance.Split(line.Substring(i)).ToList();
            foreach (var option in split)
            {
                var parts = option.Split('=', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 1)
                {
                    entry.jvmArgs[parts[0]] = "";
                } else if (parts.Length == 2)
                {
                    entry.jvmArgs[parts[0]] = parts[1];
                }
            }
            
            results.entries.Add(entry);
        }

        return results;
    }

    public static async Task RunMaven(
        string args,
        string workingDir,
        string logPrefix = " ",
        bool waitForExit = true,
        Action<string> onStdOut=null)

    {
        if (onStdOut == null)
        {
            onStdOut = line =>
            {
                Log.Information(logPrefix + line);
            };
        }
        var javaHome = await GetJavaHome();

        Log.Information($"Running `mvn {args}`");
        var task = Cli.Wrap("mvn")
            .WithArguments(args)
            .WithWorkingDirectory(workingDir)
            .WithEnvironmentVariables(new Dictionary<string, string>
            {
                [JAVA_HOME_ENV_VAR] = javaHome
            })
            .WithStandardOutputPipe(PipeTarget.ToDelegate(onStdOut))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Error(logPrefix + line);
            }))
            .ExecuteAsync();
        if (waitForExit)
        {
            await task;
        }
    }
}

public class JpsResult
{
    public List<JpsEntry> entries = new List<JpsEntry>();
}

[DebuggerDisplay("[{processId}] {mainClass}")]
public class JpsEntry
{
    public int processId;
    public string mainClass;
    public Dictionary<string, string> jvmArgs = new Dictionary<string, string>();
}