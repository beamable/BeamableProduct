using Beamable.Common;
using cli.Services;
using Newtonsoft.Json;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesDeployCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToDeploy;
	public bool Remote;
	public string FromJsonFile;
	public string RemoteComment;
	public string[] RemoteServiceComments;
}

public class ServicesDeployCommand : AppCommand<ServicesDeployCommandArgs>
{
	private readonly IAppContext _ctx;
	private readonly BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;

	public ServicesDeployCommand(IAppContext ctx, BeamoLocalSystem localBeamo, BeamoService remoteRemoteBeamo) :
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

		AddOption(new Option<string>("--from-file", () => null, $"If this option is set to a valid path to a ServiceManifest JSON, deploys that instead. Only works if --remote flag is set."),
			(args, i) => args.FromJsonFile = i);

		AddOption(new Option<string>("--comment", () => "", $"Requires --remote flag. Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal."),
			(args, i) => args.RemoteComment = i);

		AddOption(new Option<string[]>("--service-comments", Array.Empty<string>, $"Requires --remote flag. Any number of 'BeamoId::Comment' strings. " +
		                                                                          $"\nAssociates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal.")
			{
				AllowMultipleArgumentsPerToken = true
			},
			(args, i) => args.RemoteServiceComments = i);
	}

	public override async Task Handle(ServicesDeployCommandArgs args)
	{
		await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
		await _localBeamo.StartListeningToDocker();

		if (args.BeamoIdsToDeploy == null)
			args.BeamoIdsToDeploy = _localBeamo.BeamoManifest.ServiceDefinitions.Select(c => c.BeamoId).ToArray();


		if (args.Remote)
		{
			if (!string.IsNullOrEmpty(args.FromJsonFile))
			{
				ServiceManifest manifest;

				try
				{
					manifest = JsonConvert.DeserializeObject<ServiceManifest>(await File.ReadAllTextAsync(args.FromJsonFile));
				}
				catch (Exception e)
				{
					AnsiConsole.MarkupLine($"[red]A problem occurred while deserializing the given file. Please ensure the file exists and is a valid ServiceManifest.[/]");
					AnsiConsole.WriteException(e);
					return;
				}

				try
				{
					await _remoteBeamo.Deploy(manifest);
				}
				catch (Exception e)
				{
					AnsiConsole.MarkupLine($"[red]A problem occurred while deploying the given file.[/]");
					AnsiConsole.WriteException(e);
					return;
				}

				return;
			}

			// Parse and prepare per-service comments dictionary (BeamoId => CommentString) 
			var perServiceComments = new Dictionary<string, string>();
			for (var i = 0; i < args.RemoteServiceComments.Length; i++)
			{
				var commentArg = args.RemoteServiceComments[i];
				if (!commentArg.Contains("::"))
					throw new ArgumentOutOfRangeException($"{nameof(args.RemoteServiceComments)}[{i}]",
						$"Given service comment argument [{commentArg}] doesn't respect the 'BeamoId::Comment' format!");

				var splitComment = commentArg.Split("::");
				var id = splitComment[0];
				var comment = splitComment[1];

				if (_localBeamo.BeamoManifest.ServiceDefinitions.FindIndex(sd => sd.BeamoId == id) == -1)
					throw new ArgumentOutOfRangeException($"{nameof(args.RemoteServiceComments)}[{i}]",
						$"ID [{id}] in the given service comment argument [{commentArg}] " +
						$"doesn't match any of the registered services [{string.Join(",", _localBeamo.BeamoManifest.ServiceDefinitions.Select(sd => sd.BeamoId))}]!");

				perServiceComments.Add(id, comment);
			}

			// Get where we need to upload based on which platform env we are targeting
			var dockerRegistryUrl = _ctx.Host switch
			{
				Constants.PLATFORM_DEV => Constants.DOCKER_REGISTRY_DEV,
				Constants.PLATFORM_STAGING => Constants.DOCKER_REGISTRY_STAGING,
				Constants.PLATFORM_PRODUCTION => Constants.DOCKER_REGISTRY_PRODUCTION,
				_ => throw new ArgumentOutOfRangeException()
			};

			await AnsiConsole
				.Progress()
				.StartAsync(async ctx =>
				{
					// These are the services we care about tracking progress for... The storage dependencies get pulled into each of these but I don't think we need to show them for now...
					// Maybe that's not a good idea, but... we can easily change this if we want.
					var beamoServiceDefinitions = _localBeamo.BeamoManifest.ServiceDefinitions
						.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice)
						.Where(_localBeamo.VerifyCanBeBuiltLocally).ToList();

					// Prepare build and test with local deployment tasks
					var buildAndTestTasks = beamoServiceDefinitions
						.Select(sd => ctx.AddTask($"Test Local Deployment = {sd.BeamoId}"))
						.ToList();

					// Prepare uploading container tasks
					var uploadingContainerTasks = beamoServiceDefinitions
						.Select(sd => ctx.AddTask($"Uploading {sd.BeamoId}"))
						.ToList();

					// Upload Manifest Task
					var uploadManifestTask = ctx.AddTask("Publishing Manifest to Beam-O!");


					_ = await _localBeamo.DeployToRemote(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime, dockerRegistryUrl,
						args.RemoteComment ?? string.Empty,
						perServiceComments,
						(beamoId, progress) =>
						{
							var progressTask = buildAndTestTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
							progressTask?.Increment((progress * 99) - progressTask.Value);
						}, beamoId =>
						{
							var progressTask = buildAndTestTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
							progressTask?.Increment(progressTask.MaxValue - progressTask.Value);
						}, (beamoId, progress) =>
						{
							var progressTask = uploadingContainerTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
							progressTask?.Increment((progress * 99) - progressTask.Value);
						},
						(beamoId, successfull) =>
						{
							var progressTask = uploadingContainerTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
							if (progressTask != null)
							{
								progressTask.Increment(progressTask.MaxValue - progressTask.Value);
								progressTask.Description = successfull ? $"Success: {progressTask?.Description}" : $"Failure: {progressTask?.Description}";
							}
						});

					// Finish the upload manifest task
					uploadManifestTask.Increment(100);
				});
		}
		else
		{
			await AnsiConsole
				.Progress()
				.StartAsync(async ctx =>
				{
					var allProgressTasks = args.BeamoIdsToDeploy.Where(id => _localBeamo.VerifyCanBeBuiltLocally(id)).Select(id => ctx.AddTask($"Deploying Service {id}")).ToList();
					try
					{
						await _localBeamo.DeployToLocal(_localBeamo.BeamoManifest, args.BeamoIdsToDeploy,
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
