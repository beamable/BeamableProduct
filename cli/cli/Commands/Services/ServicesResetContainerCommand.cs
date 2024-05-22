using cli.Dotnet;
using cli.Services;
using Spectre.Console;

namespace cli;

public class ServicesResetContainerCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();

}

public class ServicesResetContainerCommandOutput
{
	public string id;
	public string message;
}
public class ServicesResetContainerCommand : StreamCommand<ServicesResetContainerCommandArgs, ServicesResetContainerCommandOutput>
{
	public ServicesResetContainerCommand() : base("container", "Delete any containers associated with the given Beamable services")
	{
	}
	

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);

	}

	public override async Task Handle(ServicesResetContainerCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);
		
		await args.BeamoLocalSystem.SynchronizeInstanceStatusWithDocker(args.BeamoLocalSystem.BeamoManifest, args.BeamoLocalSystem.BeamoRuntime.ExistingLocalServiceInstances);
		await args.BeamoLocalSystem.StartListeningToDocker();

		await TurnOffContainers(args.BeamoLocalSystem, args.services.ToArray(), SendResults);
	}

	public static async Task TurnOffContainers(BeamoLocalSystem beamo, string[] services, Action<ServicesResetContainerCommandOutput> onStop)
	{

		// yield return new ServicesResetContainerCommandOutput();
		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var progressTasks = services.Select(id => ctx.AddTask($"Deleting Local Service Image - {id}")).ToList();
				var actualTasks = services.Select(async id =>
				{
					await beamo.DeleteContainers(id);
					var progressTask = progressTasks.First(pt => pt.Description.Contains(id));
					progressTask.Increment(progressTask.MaxValue);
					onStop(new ServicesResetContainerCommandOutput
					{
						id = id,
						message = "stopped"
					});
				});
				await Task.WhenAll(actualTasks);
			});
	}
}
