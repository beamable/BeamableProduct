using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesDeployCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToDeploy;
	public bool Remote;
}

public class ServicesDeployCommand : AppCommand<ServicesDeployCommandArgs>
{
	private readonly IAppContext _ctx;
	private readonly BeamoLocalService _localBeamo;
	private BeamoService _remoteBeamo;

	public ServicesDeployCommand(IAppContext ctx, BeamoLocalService localBeamo, BeamoService remoteRemoteBeamo) :
		base("deploy",
			"Deploys services, either locally or remotely (to the current realm).")
	{
		_ctx = ctx;
		_localBeamo = localBeamo;
		_remoteBeamo = remoteRemoteBeamo;
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--ids", "The ids for the services you wish to deploy. Ignoring this option deploys all services." +
		                                        "If '--remote' option is set, these are the ids that'll become enabled by Beam-O once it receives the updated manifest.")
			{
				AllowMultipleArgumentsPerToken = true
			},
			(args, i) => args.BeamoIdsToDeploy = i.Length == 0 ? null : i);

		AddOption(new Option<bool>("--remote", () => false, $"If this option is set, we publish the manifest instead."),
			(args, i) => args.Remote = i);
	}

	public override async Task Handle(ServicesDeployCommandArgs args)
	{
		await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
		await _localBeamo.StartListeningToDocker();

		if (args.BeamoIdsToDeploy == null)
			args.BeamoIdsToDeploy = _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId).ToArray();


		if (args.Remote)
		{
			throw new NotImplementedException();
		}
		else
		{
			await AnsiConsole
				.Progress()
				.StartAsync(async ctx =>
				{
					var allProgressTasks = args.BeamoIdsToDeploy.Select(id => ctx.AddTask($"Deploying Service {id}")).ToList();
					try
					{
						await _localBeamo.DeployToLocalClient(_localBeamo.BeamoManifest, args.BeamoIdsToDeploy,
							(beamoId, progress) =>
							{
								var progressTask = allProgressTasks.First(pt => pt.Description.Contains(beamoId));
								progressTask.Increment((progress * 80) - progressTask.Value);
							}, beamoId =>
							{
								var progressTask = allProgressTasks.First(pt => pt.Description.Contains(beamoId));
								progressTask.Increment(20);
							});
					}
					catch (CliException e)
					{
						if (e.Message.Contains("cyclical", StringComparison.InvariantCultureIgnoreCase))
							AnsiConsole.MarkupLine($"[red]{e.Message}[/]");
						else
							throw;
					}
				});

			_localBeamo.SaveBeamoLocalManifest();
			_localBeamo.SaveBeamoLocalRuntime();
		}

		await _localBeamo.StopListeningToDocker();
	}
}
