using Beamable.Common;
using Beamable.Common.Api;
using Beamable.Common.Api.Realms;
using Beamable.Common.BeamCli;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.CommandLine;

namespace cli;

public class ServicesDeployCommandArgs : LoginCommandArgs
{
	public string FromJsonFile;

	public string RemoteComment;
	public string[] RemoteServiceComments;
	public string dockerRegistryUrl;
}

public class ServicesDeployCommand : AppCommand<ServicesDeployCommandArgs>,
	IResultSteam<DefaultStreamResultChannel, ServiceDeployReportResult>,
	IResultSteam<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>
{
	private IAppContext _ctx;
	private BeamoLocalSystem _localBeamo;
	private BeamoService _remoteBeamo;
	private ServicesListCommand _servicesListCommand;
	private IAliasService _aliasService;
	private AppLifecycle _lifeCycle;

	public ServicesDeployCommand(ServicesListCommand servicesListCommand) :
		base("deploy",
			"Deploys services remotely to the current realm")
	{
		_servicesListCommand = servicesListCommand;
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--from-file", () => null, $"If this option is set to a valid path to a ServiceManifest JSON, deploys that instead"),
			(args, i) => args.FromJsonFile = i);

		AddOption(new Option<string>("--comment", () => "", $"Associates this comment along with the published Manifest. You'll be able to read it via the Beamable Portal"),
			(args, i) => args.RemoteComment = i);

		AddOption(new Option<string[]>("--service-comments", Array.Empty<string>, $"Any number of strings in the format BeamoId::Comment" +
																				  $"\nAssociates each comment to the given Beamo Id if it's among the published services. You'll be able to read it via the Beamable Portal")
		{
			AllowMultipleArgumentsPerToken = true
		},
			(args, i) => args.RemoteServiceComments = i);

		AddOption(new Option<string>("--docker-registry-url", "A custom docker registry url to use when uploading. By default, the result from the beamo/registry network call will be used, " +
															  "with minor string manipulation to add https scheme, remove port specificatino, and add /v2 "), (args, i) => args.dockerRegistryUrl = i);
	}

	public override async Task Handle(ServicesDeployCommandArgs args)
	{
		_ctx = args.AppContext;
		_localBeamo = args.BeamoLocalSystem;
		_remoteBeamo = args.BeamoService;
		_aliasService = args.AliasService;
		_lifeCycle = args.Lifecycle;

		var isDockerRunning = await _localBeamo.CheckIsRunning();
		if (!isDockerRunning)
		{
			throw CliExceptions.DOCKER_NOT_RUNNING;
		}
		_lifeCycle.CancellationToken.ThrowIfCancellationRequested();

		var cid = _ctx.Cid;
		if (!AliasHelper.IsCid(cid))
		{
			try
			{
				Log.Verbose("Resolving alias...");
				var aliasResolve = await _aliasService.Resolve(cid).ShowLoading("Resolving alias...");
				cid = aliasResolve.Cid.GetOrElse(() => throw new CliException("Invalid alias"));
				_ctx.Set(cid, _ctx.Pid, _ctx.Host);
			}
			catch (Exception)
			{
				var msg = $"Unable to resolve alias for '{cid}'";
				AnsiConsole.WriteLine(msg);
				Log.Error(msg);
				return;
			}
		}

		List<Promise<Unit>> promises = new List<Promise<Unit>>();
		foreach (var definition in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions)
		{
			promises.Add(args.BeamoLocalSystem.UpdateDockerFile(definition));
		}

		var sequence = Promise.Sequence(promises);
		await sequence;

		try
		{
			await _localBeamo.SynchronizeInstanceStatusWithDocker(_localBeamo.BeamoManifest,
				_localBeamo.BeamoRuntime.ExistingLocalServiceInstances, _lifeCycle.CancellationToken);
			await _localBeamo.StartListeningToDocker();
		}
		catch (Exception e)
		{
			Log.Error(e.Message);
			AnsiConsole.WriteLine(e.Message);
			return;
		}

		_lifeCycle.CancellationToken.ThrowIfCancellationRequested();
		if (!string.IsNullOrEmpty(args.FromJsonFile))
		{
			ServiceManifest manifest;

			try
			{
				manifest = JsonConvert.DeserializeObject<ServiceManifest>(await File.ReadAllTextAsync(args.FromJsonFile));
			}
			catch (Exception e)
			{
				var errMsg =
					"A problem occurred while deserializing the given file. Please ensure the file exists and is a valid ServiceManifest.";
				Log.Error(errMsg);
				AnsiConsole.MarkupLine($"[red]{errMsg}[/]");
				AnsiConsole.WriteException(e);
				return;
			}

			try
			{
				await _remoteBeamo.Deploy(manifest);
			}
			catch (Exception e)
			{
				var errMsg = "A problem occurred while deploying the given file.";
				Log.Error(errMsg);
				AnsiConsole.MarkupLine($"[red]{errMsg}[/]");
				AnsiConsole.WriteException(e);
				return;
			}

			return;
		}

		// Parse and prepare per-service comments dictionary (BeamoId => CommentString) 
		Log.Verbose("Parse and prepare per-service comments dictionary");
		var perServiceComments = new Dictionary<string, string>();
		for (var i = 0; i < args.RemoteServiceComments.Length; i++)
		{
			_lifeCycle.CancellationToken.ThrowIfCancellationRequested();
			var commentArg = args.RemoteServiceComments[i];
			if (!commentArg.Contains("::"))
			{
				var errMsg =
					$"Given service comment argument [{commentArg}] doesn't respect the 'BeamoId::Comment' format!";
				Log.Error(errMsg, "ERROR");
				throw new ArgumentOutOfRangeException($"{nameof(args.RemoteServiceComments)}[{i}]", errMsg);
			}

			var splitComment = commentArg.Split("::");
			var id = splitComment[0];
			var comment = splitComment[1];

			if (_localBeamo.BeamoManifest.ServiceDefinitions.FindIndex(sd => sd.BeamoId == id) == -1)
			{
				var errMsg = $"ID [{id}] in the given service comment argument [{commentArg}] " +
							 $"doesn't match any of the registered services [{string.Join(",", _localBeamo.BeamoManifest.ServiceDefinitions.Select(sd => sd.BeamoId))}]!";
				Log.Error(errMsg);
				throw new ArgumentOutOfRangeException($"{nameof(args.RemoteServiceComments)}[{i}]", errMsg);
			}

			perServiceComments.Add(id, comment);
		}

		// Get where we need to upload based on which platform env we are targeting
		Log.Verbose("Getting docker registry url...");
		var dockerRegistryUrl = args.dockerRegistryUrl;
		if (string.IsNullOrEmpty(dockerRegistryUrl))
		{
			dockerRegistryUrl = await _remoteBeamo.GetDockerImageRegistryUri();
		}

		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				_lifeCycle.CancellationToken.ThrowIfCancellationRequested();
				// These are the services we care about tracking progress for... The storage dependencies get pulled into each of these but I don't think we need to show them for now...
				// Maybe that's not a good idea, but... we can easily change this if we want.
				var beamoServiceDefinitions = _localBeamo.BeamoManifest.ServiceDefinitions
					.Where(sd => sd.Protocol == BeamoProtocolType.HttpMicroservice)
					.Where(_localBeamo.VerifyCanBeBuiltLocally).ToList();

				_lifeCycle.CancellationToken.ThrowIfCancellationRequested();
				// Prepare build and test with local deployment tasks
				var buildAndTestTasks = beamoServiceDefinitions
					.Select(sd => ctx.AddTask($"Test Local Deployment = {sd.BeamoId}"))
					.ToList();

				_lifeCycle.CancellationToken.ThrowIfCancellationRequested();
				// Prepare uploading container tasks
				var uploadingContainerTasks = beamoServiceDefinitions
					.Select(sd => ctx.AddTask($"Uploading {sd.BeamoId}"))
					.ToList();

				// Upload Manifest Task
				var uploadManifestTask = ctx.AddTask("Publishing Manifest to Beam-O!");


				var atLeastOneFailed = false;

				Log.Verbose("Starting deployment...");
				_ = await _localBeamo.DeployToRemote(_localBeamo, _remoteBeamo, dockerRegistryUrl,
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
							this.SendResults<ServiceRemoteDeployProgressResult, ServiceRemoteDeployProgressResult>(
								new ServiceRemoteDeployProgressResult() { BeamoId = beamoId, BuildAndTestProgress = progressTask.MaxValue, ContainerUploadProgress = progressTask.Value }
							);
							progressTask.Description = successful ? $"Success: {progressTask?.Description}" : $"Failure: {progressTask?.Description}";
							atLeastOneFailed |= !successful;
						}
					}, _lifeCycle.CancellationToken);

				// Finish the upload manifest task
				uploadManifestTask.Increment(100);

				this.SendResults<DefaultStreamResultChannel, ServiceDeployReportResult>(new ServiceDeployReportResult() { Success = !atLeastOneFailed, FailureReason = "" });

				// After deploying to remote, we need to stop the services we deployed locally.
				await _localBeamo.StopExistingLocalServiceInstances();

				Log.Verbose("Finished deployment");

				Log.Verbose("Saving beamo local runtime");
				_localBeamo.SaveBeamoLocalRuntime();
			});

		await _localBeamo.StopListeningToDocker();
		Log.Verbose("Process completed");
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
