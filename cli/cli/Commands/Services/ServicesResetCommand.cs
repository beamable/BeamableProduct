using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;
using System.CommandLine.Invocation;

namespace cli;

public class ServicesResetCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToReset;
}

public class ServicesResetCommand : AppCommand<ServicesResetCommandArgs>
{
	private readonly BeamoLocalSystem _localBeamo;

	public ServicesResetCommand(BeamoLocalSystem localBeamo) :
		base("reset",
			"Resets services to default settings and cleans up docker images (if any exist).")
	{
		_localBeamo = localBeamo;
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--ids", "The ids for the services you wish to reset (cleanup docker and revert to default protocol settings).") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToReset = i.Length == 0 ? null : i);
	}

	public override async Task Handle(ServicesResetCommandArgs args)
	{
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
				).ToArray();
			}
			else
			{
				AnsiConsole.MarkupLine("[yellow]No Beam-O services are defined.[/]");
				args.BeamoIdsToReset = Array.Empty<string>();
			}
		}

		if (args.BeamoIdsToReset.Contains("_All_"))
			args.BeamoIdsToReset = _localBeamo.BeamoManifest.ServiceDefinitions.Select(sd => sd.BeamoId).ToArray();


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

		_localBeamo.SaveBeamoLocalManifest();
		_localBeamo.SaveBeamoLocalRuntime();
		await _localBeamo.StopListeningToDocker();
	}
}
