using cli.Dotnet;
using Spectre.Console;

namespace cli;

public class ServicesResetImageCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();

}

public class ServicesResetImageCommandOutput
{
	public string id;
	public string message;
}

public class ServicesResetImageCommand : StreamCommand<ServicesResetImageCommandArgs, ServicesResetImageCommandOutput>
{
	public ServicesResetImageCommand() : base("image", "Delete any images associated with the given Beamable services")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
	}

	public override async Task Handle(ServicesResetImageCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, ref args.services);

		await args.BeamoLocalSystem.SynchronizeInstanceStatusWithDocker(args.BeamoLocalSystem.BeamoManifest, args.BeamoLocalSystem.BeamoRuntime.ExistingLocalServiceInstances);
		await args.BeamoLocalSystem.StartListeningToDocker();

		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var progressTasks = args.services.Select(id => ctx.AddTask($"Deleting Local Service Image - {id}")).ToList();
				var actualTasks = args.services.Select(async id =>
				{
					await args.BeamoLocalSystem.CleanUpDocker(id);
					var progressTask = progressTasks.First(pt => pt.Description.Contains(id));
					progressTask.Increment(progressTask.MaxValue);
					this.SendResults(new ServicesResetImageCommandOutput
					{
						message = "removed",
						id = id
					});
				});

				await Task.WhenAll(actualTasks);
			});
	}
}
