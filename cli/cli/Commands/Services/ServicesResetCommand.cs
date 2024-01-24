using Beamable.Common;
using cli.Services;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli;

public class ServicesResetCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToReset;
	public string Target;
}

public class ServicesResetCommand : AtomicCommand<ServicesResetCommandArgs, ServicesResetCommand.ServicesResetResult>
{
	private BeamoLocalSystem _localBeamo;

	public ServicesResetCommand() :
		base("reset",
			"Resets services to default settings and cleans up docker images (if any exist)")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--ids", "The ids for the services you wish to reset") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToReset = i.Length == 0 ? null : i);

		AddArgument(new Argument<string>("target", $"Either image|container|protocols." +
												   $"'image' will cleanup all your locally built images for the selected Beamo Services.\n" +
												   $"'container' will stop all your locally running containers for the selected Beamo Services.\n" +
												   $"'protocols' will reset all the protocol data for the selected Beamo Services back to default parameters"), (args, i) => args.Target = i);
	}

	public override async Task<ServicesResetCommand.ServicesResetResult> GetResult(ServicesResetCommandArgs args)
	{
		_localBeamo = args.BeamoLocalSystem;

		await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
		await _localBeamo.StartListeningToDocker();

		if (args.BeamoIdsToReset == null)
		{
			var choices = _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId).Distinct().ToList();
			if (choices.Count > 1) choices.Insert(0, "_All_");

			if (choices.Count != 0)
			{
				args.BeamoIdsToReset = AnsiConsole.Prompt(new MultiSelectionPrompt<string>()
					.Title("Service Ids to Reset")
					.NotRequired()
					.InstructionsText("Select any number of Beam-O Service Ids to reset.")
					.AddChoices(choices)
					.AddBeamHightlight()
				).ToArray();
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]No Beam-O services are defined.[/]");
				args.BeamoIdsToReset = Array.Empty<string>();
			}
		}
		else if (args.ProjectService.ConfigFileExists is false)
		{
			BeamableLogger.Log("We couldn't find a service in the directory");
			string directory = AnsiConsole.Ask<string>("Enter the absolute or relative directory to use:");
			await new BeamCommandAssistantBuilder("services reset", args.AppContext)
				.AddArgument(args.Target)
				.WithOption(true, "--dir", directory)
				.WithOption(args.BeamoIdsToReset.Length > 0, "--ids", args.BeamoIdsToReset)
				.RunAsync();
			return null;
		}

		if (args.BeamoIdsToReset.Contains("_All_"))
			args.BeamoIdsToReset = _localBeamo.BeamoManifest.ServiceDefinitions.Select(sd => sd.BeamoId).ToArray();

		if (args.Target == "image")
		{
			await AnsiConsole
				.Progress()
				.StartAsync(async ctx =>
				{
					var progressTasks = args.BeamoIdsToReset.Select(id => ctx.AddTask($"Deleting Local Service Image - {id}")).ToList();
					var actualTasks = args.BeamoIdsToReset.Select(async id =>
					{
						await _localBeamo.CleanUpDocker(id);
						var progressTask = progressTasks.First(pt => pt.Description.Contains(id));
						progressTask.Increment(progressTask.MaxValue);
					});

					await Task.WhenAll(actualTasks);
				});

		}
		else if (args.Target == "container")
		{
			await AnsiConsole
				.Progress()
				.StartAsync(async ctx =>
				{
					var progressTasks = args.BeamoIdsToReset.Select(id => ctx.AddTask($"Deleting Local Service Image - {id}")).ToList();
					var actualTasks = args.BeamoIdsToReset.Select(async id =>
					{
						await _localBeamo.DeleteContainers(id);
						var progressTask = progressTasks.First(pt => pt.Description.Contains(id));
						progressTask.Increment(progressTask.MaxValue);
					});
					await Task.WhenAll(actualTasks);
				});
		}
		else if (args.Target == "protocols")
		{
			await AnsiConsole
				.Progress()
				.StartAsync(async ctx =>
				{
					var progressTasks = args.BeamoIdsToReset.Select(id => ctx.AddTask($"Reseting Local Service with {id}")).ToList();
					var actualTasks = args.BeamoIdsToReset.Select(async id =>
					{
						await _localBeamo.CleanUpDocker(id);

						var protocol = _localBeamo.BeamoManifest.ServiceDefinitions.First(sd => sd.BeamoId == id).Protocol;
						switch (protocol)
						{
							case BeamoProtocolType.HttpMicroservice:
								await _localBeamo.ResetToDefaultValues_HttpMicroservice(id);
								break;
							case BeamoProtocolType.EmbeddedMongoDb:
								await _localBeamo.ResetToDefaultValues_EmbeddedMongoDb(id);
								break;
							default:
								throw new ArgumentOutOfRangeException();
						}

						var progressTask = progressTasks.First(pt => pt.Description.Contains(id));
						progressTask.Increment(progressTask.MaxValue);
					});

					await Task.WhenAll(actualTasks);
				});
		}
		
		_localBeamo.SaveBeamoLocalManifest();
		_localBeamo.SaveBeamoLocalRuntime();
		await _localBeamo.StopListeningToDocker();
		return new ServicesResetResult { Target = args.Target, Ids = args.BeamoIdsToReset.ToList(), };
	}


	public class ServicesResetResult
	{
		public string Target;
		public List<string> Ids;
	}
}
