using Beamable.Server;
using CliWrap;

namespace cli.OtelCommands.Grafana;

public class GrafanaCommand : CommandGroup
{
    public override bool IsForInternalUse => true;

    public GrafanaCommand() : base("grafana", "Allows access to a local Grafana instance")
    {
    }
    
    
    public static string GetGrafanaContainerName(IAppContext app)
    {
        return $"beam-grafana-{app.Cid}-{app.ProjectName}";
    }

    public static string GetGrafanaVolumeName(IAppContext app)
    {
        return $"beam-grafana-volume-{app.Cid}-{app.ProjectName}";
    }

    public static string GetAbsoluteFilePath_DefaultDashboard()
    {
        return Path.Combine(Path.GetFullPath(AppContext.BaseDirectory), "Resources", "Grafana",
            "default-dashboard.json");
    }
    
    public static string GetAbsoluteFilePath_GrafanaIni()
    {
        return Path.Combine(Path.GetFullPath(AppContext.BaseDirectory), "Resources", "Grafana",
            "grafana.ini");
    }
    
    public static string GetAbsoluteFilePath_ProvisioningFolder()
    {
        return Path.Combine(Path.GetFullPath(AppContext.BaseDirectory), "Resources", "Grafana",
            "provisioning");
    }
    
    public static int GetGrafanaPort(IAppContext app)
    {
        return 10101;
    }

    public static string GetGrafanaUrl(IAppContext app)
    {
        return $"http://localhost:{GetGrafanaPort(app)}";
    }

    public static async Task<bool> ReadGrafanaLogs(IAppContext app)
    {
        var containerName = GetGrafanaContainerName(app);

        var argString = $"logs {containerName} -f";
        var tcs = new TaskCompletionSource<bool>();
        var command = Cli
            .Wrap(app.DockerPath)
            .WithArguments(argString)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                if (line.Contains("app registry initialized", StringComparison.CurrentCultureIgnoreCase))
                {
                    // ah, its ready to rock!
                    tcs.SetResult(true);
                }
                Log.Information($"found log {line}");
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Information($"error log {line}");
                tcs.SetResult(false);
            }));
        var logExec = command.ExecuteAsync();

        var isRunning = await tcs.Task;
        return isRunning;
    }
    
    
    public static async Task<bool> StopGrafana(IAppContext app)
    {
        var containerName = GetGrafanaContainerName(app);

        var argString = $"rm -f {containerName}";
        var failed = false;
        var command = Cli
            .Wrap(app.DockerPath)
            .WithArguments(argString)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            { 
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                failed = true;
            }));
        var logExec = command.ExecuteAsync();
        
        await logExec;
        
        return !failed;
    }

    public static async Task<bool> IsGrafanaRunning(IAppContext app)
    {
        var containerName = GetGrafanaContainerName(app);

        var argString = $"inspect {containerName}";
        var failed = false;
        var command = Cli
            .Wrap(app.DockerPath)
            .WithArguments(argString)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            { 
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                failed = true;
            }));
        var logExec = command.ExecuteAsync();
        
        await logExec;
        
        return !failed;
    }

}
