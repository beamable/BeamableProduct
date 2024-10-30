using CliWrap;
using Serilog;
using System.CommandLine;

namespace cli.DockerCommands;

public class DockerStatusCommandArgs : CommandArgs
{
	public bool watch;
}

public class DockerStatusCommandOutput
{
	public bool isDaemonRunning;
	public bool isCliAccessible;

	public string cliLocation;
}


public class DockerStatusCommand : StreamCommand<DockerStatusCommandArgs, DockerStatusCommandOutput>
{
	public override bool AutoLogOutput => true;

	public DockerStatusCommand() : base("status", "Check if docker is running and available to the CLI")
	{
		AddAlias("ps");
	}

	public override void Configure()
	{
		AddOption(new Option<bool>(new string[] { "--watch", "-w" }, "Emit a stream of updates as docker changes"),
			(args, i) =>
			{
				args.watch = i;
			});
	}

	public override async Task Handle(DockerStatusCommandArgs args)
	{

		if (args.watch)
		{
			DockerStatusCommandOutput lastStatus = null;
			while (!args.Lifecycle.IsCancelled)
			{
				
				var status = await CheckDocker(args);

				var changes = false;
				if (lastStatus == null)
				{
					changes = true;
				}
				else
				{
					changes |= lastStatus.isCliAccessible != status.isCliAccessible;
					changes |= lastStatus.isDaemonRunning != status.isDaemonRunning;
				}

				if (changes)
				{
					SendResults(status);
				}
				
				
				lastStatus = status;
				await Task.Delay(500);

			}
		}
		else
		{
			this.SendResults(await CheckDocker(args));
		}
		
	}

	public static async Task<DockerStatusCommandOutput> CheckDocker(CommandArgs args)
	{
		var daemonTask = IsDaemonAvailable(args);
		var cliTask = IsCliAvailable(args);
		
		var output = new DockerStatusCommandOutput
		{
			isDaemonRunning = await daemonTask,
			isCliAccessible = await cliTask,
			cliLocation = args.AppContext.DockerPath
		};
		return output;
	}

	static async Task<bool> IsCliAvailable(CommandArgs args)
	{

		var command = CliWrap.Cli
			.Wrap(args.AppContext.DockerPath)
			.WithStandardOutputPipe(PipeTarget.ToDelegate(x =>
			{
			}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(x =>
			{
			}))
			.WithArguments("--version")
			.WithValidation(CommandResultValidation.None);

		try
		{
			var res = command.ExecuteAsync();
			await res;
			var success = res.Task.Result.ExitCode == 0;
			if (!success)
			{
				return false;
			}

			return true;
		}
		catch (Exception)
		{
			return false;
		}

	}

	static async Task<bool> IsDaemonAvailable(CommandArgs args)
	{
		try
		{
			await args.BeamoLocalSystem.Client.System.PingAsync();
			return true;
		}
		catch
		{
			return false;
		}
	}
}
