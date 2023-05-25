using Beamable.Common;
using Beamable.Common.BeamCli;
using cli.Services;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesDeployCommandArgs : LoginCommandArgs
{
	public string[] BeamoIdsToEnable;
	public string[] BeamoIdsToDisable;

	public string FromJsonFile;

	public string RemoteComment;
	public string[] RemoteServiceComments;
}

public class ServicesDeployCommand : AppCommand<ServicesDeployCommandArgs>,
	IResultSteam<DefaultStreamResultChannel, ServiceDeployReportResult>,
	IResultSteam<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>

{
	private IAppContext _ctx;
	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;

	public ServicesDeployCommand() :
		base("deploy",
			"Deploys services remotely to the current realm")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string[]>("--enable", "These are the ids for services you wish to be enabled once Beam-O receives the updated manifest") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToEnable = i.Length == 0 ? null : i);

		AddOption(new Option<string[]>("--disable", "These are the ids for services you wish to be disabled once Beam-O receives the updated manifest") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.BeamoIdsToDisable = i.Length == 0 ? null : i);

		AddOption(new Option<string>("--from-file", () => null, $"If this option is set to a valid path to a ServiceManifest JSON, deploys that instead"),
			(args, i) => args.FromJsonFile = i);

		AddOption(new Option<string>("--comment", () => "", $"Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal"),
			(args, i) => args.RemoteComment = i);

		AddOption(new Option<string[]>("--service-comments", Array.Empty<string>, $"Any number of strings in the format BeamoId::Comment" +
		                                                                          $"\nAssociates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal") { AllowMultipleArgumentsPerToken = true },
			(args, i) => args.RemoteServiceComments = i);
	}

	public override async Task Handle(ServicesDeployCommandArgs args)
	{
		_ctx = args.AppContext;
		_localBeamo = args.BeamoLocalSystem;
		_remoteBeamo = args.BeamoService;
		try
		{
			await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest,
				_localBeamo.BeamoRuntime.ExistingLocalServiceInstances);
			await _localBeamo.StartListeningToDocker();
		}
		catch
		{
			return;
		}

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

		// Enable the list of BeamoIds that were given
		args.BeamoIdsToEnable ??= Array.Empty<string>();
		foreach (string beamoId in args.BeamoIdsToEnable)
		{
			var sd = _localBeamo.BeamoManifest.ServiceDefinitions.First(def => def.BeamoId == beamoId);
			sd.ShouldBeEnabledOnRemote = true;
		}

		// Disable the list of BeamoIds that were given
		args.BeamoIdsToDisable ??= Array.Empty<string>();
		foreach (string beamoId in args.BeamoIdsToDisable)
		{
			var sd = _localBeamo.BeamoManifest.ServiceDefinitions.First(def => def.BeamoId == beamoId);
			sd.ShouldBeEnabledOnRemote = false;
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


				var atLeastOneFailed = false;

				_ = await _localBeamo.DeployToRemote(_localBeamo.BeamoManifest, _localBeamo.BeamoRuntime, dockerRegistryUrl,
					args.RemoteComment ?? string.Empty,
					perServiceComments,
					(beamoId, progress) =>
					{
						var progressTask = buildAndTestTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
						progressTask?.Increment((progress * 99) - progressTask.Value);
						this.SendResults<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>(
							new ServiceRemoteDeployProgressResult() { BeamoId = beamoId, BuildAndTestProgress = progressTask?.Value ?? 0.0f, ContainerUploadProgress = 0.0f, }
						);
					}, beamoId =>
					{
						var progressTask = buildAndTestTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
						progressTask?.Increment(progressTask.MaxValue - progressTask.Value);
						this.SendResults<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>(
							new ServiceRemoteDeployProgressResult() { BeamoId = beamoId, BuildAndTestProgress = progressTask?.Value ?? 0.0f, ContainerUploadProgress = 0.0f, }
						);
					}, (beamoId, progress) =>
					{
						var buildAndTestTask = buildAndTestTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
						var progressTask = uploadingContainerTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
						progressTask?.Increment((progress * 99) - progressTask.Value);
						this.SendResults<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>(
							new ServiceRemoteDeployProgressResult() { BeamoId = beamoId, BuildAndTestProgress = buildAndTestTask?.Value ?? 0.0f, ContainerUploadProgress = progressTask?.Value ?? 0.0f, }
						);
					},
					(beamoId, successful) =>
					{
						var progressTask = uploadingContainerTasks.FirstOrDefault(pt => pt.Description.Contains(beamoId));
						if (progressTask != null)
						{
							progressTask.Increment(progressTask.MaxValue - progressTask.Value);
							progressTask.Description = successful ? $"Success: {progressTask?.Description}" : $"Failure: {progressTask?.Description}";
							atLeastOneFailed |= !successful;
						}
					});

				// Finish the upload manifest task
				uploadManifestTask.Increment(100);

				this.SendResults<DefaultStreamResultChannel, ServiceDeployReportResult>(new ServiceDeployReportResult() { Success = !atLeastOneFailed, FailureReason = "" });

				// After deploying to remote, we need to stop the services we deployed locally.
				await _localBeamo.StopExistingLocalServiceInstances();

				_localBeamo.SaveBeamoLocalManifest();
				_localBeamo.SaveBeamoLocalRuntime();
			});

		await _localBeamo.StopListeningToDocker();
	}
}

public class ServiceRemoteDeployProgressResult : IResultChannel
{
	public string ChannelName => "remote_progress";

	public string BeamoId;
	public double BuildAndTestProgress;
	public double ContainerUploadProgress;
}

public class ServiceDeployReportResult
{
	public bool Success;

	public string FailureReason;
}
