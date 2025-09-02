using System.CommandLine;
using System.Diagnostics;
using System.Net;
using System.Text;
using Beamable.Server;
using Spectre.Console;

namespace cli.BackendCommands;

public class BackendLogCommandArgs : CommandArgs, IBackendCommandArgs
{
    public string toolName;
    public bool watch;
    public int linesBack;
    public string backendHome;
    public string BackendHome => backendHome;
}

public class BackendLogCommand 
    : AppCommand<BackendLogCommandArgs>
    , ISkipManifest
    , IStandaloneCommand
{
    public BackendLogCommand() : base("logs", "read logs for a tool started with the cli")
    {
    }

    public override void Configure()
    {
        AddOption(new Option<string>(new string[] { "--tool", "-t" }, "the name of the tool to read logs for"),
            (args, i) => args.toolName = i);
        AddOption(new Option<bool>(new string[] { "--watch", "-w" }, "should the logs watch the file"),
            (args, i) => args.watch = i);
        AddOption(new Option<int>(new string[] { "--lines", "-l" }, () => 20, "the number of lines to read backwards in the log file"),
            (args, i) => args.linesBack = i);
        
        BackendCommandGroup.AddBackendHomeOption(this, (args, i) => args.backendHome = i);
    }

    public override async Task Handle(BackendLogCommandArgs args)
    {
        BackendCommandGroup.ValidateBackendHomeDirectoryExists(args.backendHome);
        
        var list = BackendListToolsCommand.GatherToolList(args.backendHome);

        
        var tool = list.tools.FirstOrDefault(t => t.name == args.toolName);
        if (tool == null) throw new CliException($"Unknown tool=[{args.toolName}]");

        var toolStatus = await BackendPsCommand.CheckToolStatus(list, args);
        
        var matchingTools = toolStatus.tools
            .Where(t => t.toolName == tool.name)
            .DistinctBy(t => t.processId)
            .ToList();

        if (matchingTools.Count > 1)
        {
            throw new NotImplementedException("there are too many instances running, not sure which one to pick");
        }

        var matchingTool = matchingTools[0];
        if (string.IsNullOrEmpty(matchingTool.stdOutPath))
        {
            Log.Warning("The instance does not have a configured std-out buffer location. Maybe it was run from the IDE?");
            return;
        }
        
        var stdOut = TailLogs(tool, matchingTool.stdOutPath, args.linesBack, args.watch, args, Log.Information);
        var stdErr = TailLogs(tool, matchingTool.stdErrPath, args.linesBack, args.watch, args, Log.Error);

        await stdOut;
    }

    // chat-gpt wrote this for me.
    static void SeekToLastNLines(FileStream fs, int n, int bufferSize = 4096)
    {
        byte[] newlineBytes = Encoding.UTF8.GetBytes(Environment.NewLine);
        int nlLen = newlineBytes.Length;

        byte[] buffer = new byte[bufferSize + nlLen]; // pad for overlap check
        long filePos = fs.Length;
        int newLinesFound = 0;

        while (filePos > 0 && newLinesFound <= n)
        {
            int toRead = (int)Math.Min(bufferSize, filePos);
            filePos -= toRead;
            fs.Seek(filePos, SeekOrigin.Begin);

            // read a block + overlap to handle newline sequences crossing boundary
            int read = fs.Read(buffer, 0, toRead + (nlLen - 1));

            for (int i = read - nlLen; i >= 0; i--)
            {
                bool isNewline = true;
                for (int j = 0; j < nlLen; j++)
                {
                    if (buffer[i + j] != newlineBytes[j])
                    {
                        isNewline = false;
                        break;
                    }
                }

                if (isNewline)
                {
                    newLinesFound++;
                    if (newLinesFound > n)
                    {
                        fs.Seek(filePos + i + nlLen, SeekOrigin.Begin);
                        return;
                    }
                }
            }
        }

        // If file has fewer than n lines, rewind to start
        fs.Seek(0, SeekOrigin.Begin);
    }
    
    public static async Task TailLogs(BackendToolInfo tool, string logPath, int lines, bool watch, CommandArgs args, Action<string> onLine)
    {
        while (watch && !File.Exists(logPath) && !args.Lifecycle.IsCancelled)
        {
            // wait for the file to exist.
            await Task.Delay(TimeSpan.FromMilliseconds(250));
        }

        if (!File.Exists(logPath))
        {
            return;
        }

        using var stream = File.OpenRead(logPath);
        SeekToLastNLines(stream, lines);
        using var reader = new StreamReader(stream);

        while (!args.Lifecycle.IsCancelled)
        {
            if (reader.EndOfStream)
            {
                if (!watch)
                {
                    return;
                }
                await Task.Delay(TimeSpan.FromMilliseconds(250));
                continue;
            }
            var line = await reader.ReadLineAsync();
            onLine?.Invoke(line);
        }
    }
}