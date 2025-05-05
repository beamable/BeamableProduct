using Beamable.Common.Dependencies;
using Beamable.Server;
using cli.OtelCommands.Grafana;
using cli.Utils;
using CliWrap;

namespace cli.OtelCommands;
using Otel = Beamable.Common.Constants.Features.Otel;

public class StartGrafanaCommandArgs : CommandArgs
{
    
}

public class StartGrafanaCommandResults
{
    
}

public class StartGrafanaCommand : AtomicCommand<StartGrafanaCommandArgs, StartGrafanaCommandResults>
{
    public StartGrafanaCommand() : base("open", "Opens a local Grafana installation to inspect telemetry data. ")
    {
    }

    public override void Configure()
    {
    }

    public override async Task<StartGrafanaCommandResults> GetResult(StartGrafanaCommandArgs args)
    {
        var env = new ClickhouseConnectionStrings
        {
            Host = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_CLICKHOUSE_HOST),
            UserName = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_CLICKHOUSE_USERNAME),
            Password = Environment.GetEnvironmentVariable(Otel.ENV_COLLECTOR_CLICKHOUSE_PASSWORD),
        };
        
        await StartGrafanaContainer(args.DependencyProvider, env);

        return new StartGrafanaCommandResults();
    }

    
    public static async Task StartGrafanaContainer(
        IDependencyProvider provider, 
        ClickhouseConnectionStrings connection)
    {
        var app = provider.GetService<IAppContext>();

        var wasRunning = await GrafanaCommand.IsGrafanaRunning(app);
        if (wasRunning)
        {
            MachineHelper.OpenBrowser(GrafanaCommand.GetGrafanaUrl(app));
            return;
        }
        var argString = $"run -d --rm " +
                        $"--name={GrafanaCommand.GetGrafanaContainerName(app)} " +
                        $"-p 127.0.0.1:{GrafanaCommand.GetGrafanaPort(app)}:3000 " +
                        $"-e GF_SECURITY_ADMIN_USER=beamable " +
                        $"-e GF_SECURITY_ADMIN_PASSWORD=beamable " +
                        $"-e BEAM_CLICKHOUSE_HOST={connection.Host} " +
                        $"-e BEAM_CLICKHOUSE_USERNAME={connection.UserName} " +
                        $"-e BEAM_CLICKHOUSE_PASSWORD={connection.Password} " +
                        $"-e GF_INSTALL_PLUGINS=grafana-clickhouse-datasource " +
                        $"-v {GrafanaCommand.GetAbsoluteFilePath_ProvisioningFolder()}:/etc/grafana/provisioning " + 
                        $"-v {GrafanaCommand.GetAbsoluteFilePath_GrafanaIni()}:/etc/grafana/grafana.ini " +
                        $"-v {GrafanaCommand.GetAbsoluteFilePath_DefaultDashboard()}:/etc/grafana/default-dashboard.json " +
                        $"-v {GrafanaCommand.GetGrafanaVolumeName(app)}:/var/lib/grafana " +
                        $"grafana/grafana:11.6.1";
        
        var command = Cli
            .Wrap(app.DockerPath)
            .WithArguments(argString)
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Debug($"Started Grafana container id=[{line}]");
            }))
            .WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
            {
                Log.Error(line);
            }));
		      
        var result = await command.ExecuteAsync();

        var isRunning = await GrafanaCommand.ReadGrafanaLogs(app);
        if (isRunning)
        {
            MachineHelper.OpenBrowser(GrafanaCommand.GetGrafanaUrl(app));
        }
        // get the logs and wait for the magic number... 
    }
}