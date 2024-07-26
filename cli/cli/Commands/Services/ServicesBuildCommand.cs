using Beamable.Common.BeamCli;
using Beamable.Common.Dependencies;
using cli.Dotnet;
using cli.Services;
using CliWrap;
using CliWrap.Buffered;
using MongoDB.Driver;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.CommandLine;
using System.Text;
using System.Text.Json;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace cli;

public class ServicesBuildCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();

	public bool simultaneous;
	public bool noCache;
	public bool pull;
	public bool forceCpu;
	public string[] tags;
}

public class ServicesBuildCommandOutput
{
	public string id;
	public string message;
	public bool isFailure;
}

public class ServicesBuiltProgress
{
	public string id;
	public int totalSteps;
	public int completedSteps;

	public float Ratio => completedSteps / (float)totalSteps;
}

public class ProgressStream : IResultChannel
{
	public string ChannelName => "progress";
}

public class ServicesBuildCommand : AppCommand<ServicesBuildCommandArgs>
	, IResultSteam<DefaultStreamResultChannel, ServicesBuildCommandOutput>
	, IResultSteam<ProgressStream, ServicesBuiltProgress>
{
	public ServicesBuildCommand() : base("build", "Build a set of services into docker images")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		ProjectCommand.AddServiceTagsOption(this, 
			bindWithTags: (args, i) => args.withServiceTags = i,
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);

		var serialOption = new Option<bool>("--simultaneous",
			"When true, all build images will run in parallel");
		serialOption.AddAlias("-s");
		serialOption.SetDefaultValue(false);
		
		var forceCpuOption = new Option<bool>("--force-cpu-arch",
			"When true, build an image for the Beamable Cloud architecture, amd64");
		forceCpuOption.AddAlias("-fcpu");
		forceCpuOption.SetDefaultValue(false);
		
		var pullOption = new Option<bool>("--pull",
			"When true, force the docker build to pull all base images");
		pullOption.AddAlias("-p");
		pullOption.SetDefaultValue(false);
		
		var noCacheOption = new Option<bool>("--no-cache",
			"When true, force the docker build to ignore all caches");
		noCacheOption.SetDefaultValue(false);
		
		var tagsOption = new Option<string[]>("--tags",
			"Provider custom tags for the resulting docker images");
		tagsOption.SetDefaultValue(new string[]{"latest"});
		tagsOption.AllowMultipleArgumentsPerToken = true;
		tagsOption.Arity = ArgumentArity.ZeroOrMore;

		AddOption(forceCpuOption, (args, i) => args.forceCpu = i);
		AddOption(pullOption, (args, i) => args.pull = i);
		AddOption(noCacheOption, (args, i) => args.noCache = i);
		AddOption(tagsOption, (args, i) => args.tags = i);
		AddOption(serialOption, (args, i) => args.simultaneous = i);

	}

	public override async Task Handle(ServicesBuildCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, 
			withTags: args.withServiceTags, 
			withoutTags: args.withoutServiceTags,
			includeStorage: false, 
			ref args.services);

		var failed = false;
		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var tasks = new List<Task<BuildImageOutput>>();
				var visuals = new ProgressTask[args.services.Count];
				for (var i = 0; i < visuals.Length; i++)
				{
					visuals[i] = ctx.AddTask(args.services[i]);
				}
				for (var i = 0 ; i < args.services.Count; i ++)
				{
					var index = i;
					var service = args.services[i];
					var buildTask = Build(
						args.DependencyProvider, 
						service,
						noCache: args.noCache,
						pull: args.pull,
						forceCpu: args.forceCpu,
						tags: args.tags,
						logMessage: msg =>
					{
						msg.id = service;
						if (string.IsNullOrEmpty(msg.message)) return;
						
						if (msg.isFailure)
						{
							Log.Error(msg.message);
							visuals[index].StopTask();
						}
						else
						{						
							Log.Information($"({service}): {msg.message}");
						}
						
						this.SendResults<DefaultStreamResultChannel, ServicesBuildCommandOutput>(msg);
					}, progressMessage: prog =>
					{
						prog.id = service;
						visuals[index].Value = (prog.Ratio * visuals[index].MaxValue);
						this.SendResults<ProgressStream, ServicesBuiltProgress>(prog);
					});
					tasks.Add(buildTask);

					if (args.simultaneous)
					{
						await buildTask;
					}
				}

				var results = await Task.WhenAll(tasks);
				failed = results.Any(x => !x.success);

				foreach (var result in results)
				{
					Log.Information(result.Display);
				}

			});

		if (failed)
		{
			throw new CliException("failed to build. see logs for details. ");
		}
	}

	/// <summary>
	/// Use docker buildkit to build a beamo service id.
	/// </summary>
	/// <param name="provider"></param>
	/// <param name="id">a beamoId of a local http microservice. </param>
	/// <param name="logMessage">a callback to use for log outputs.</param>
	/// <param name="progressMessage">a callback to use for progress outputs. The progress total step count may change.</param>
	/// <param name="noCache">mirrors dockers --no-cache</param>
	/// <param name="forceCpu">when true, appends --platform linux/amd64 </param>
	/// <param name="pull">mirrors dockers --pull </param>
	/// <param name="tags">defaults to "latest", but allows for multiple tags</param>
	/// <returns>a <see cref="BuildImageOutput"/> including success and image-id </returns>
	public static async Task<BuildImageOutput> Build(
		IDependencyProvider provider, 
		string id, 
		Action<ServicesBuildCommandOutput> logMessage=null, 
		Action<ServicesBuiltProgress> progressMessage=null,
		bool noCache=false,
		bool forceCpu=false,
		bool pull=false,
		string[] tags=null)
	{
		// a fake number of "steps" that the tarball is allotted. 
		const int tarBallSteps = 2;
		const int estimatedSteps = 10;
		const int stepPadding = 2;

		var beamoLocal = provider.GetService<BeamoLocalSystem>();
		var app = provider.GetService<IAppContext>();

		var dockerPath = app.DockerPath;
		if (!DockerPathOption.TryValidateDockerExec(dockerPath, out var dockerPathError))
		{
			throw new CliException(dockerPathError);
		}
		
		if (tags == null)
		{
			tags = new string[] { "latest" };
		}
		
		if (!beamoLocal.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(id, out var http))
		{
			logMessage?.Invoke(new ServicesBuildCommandOutput
			{
				message = "no service exists for the name",
				isFailure = true
			});
			return new BuildImageOutput
			{
				service = id
			};
		}

		if (!beamoLocal.BeamoManifest.TryGetDefinition(id, out var definition))
		{
			logMessage?.Invoke(new ServicesBuildCommandOutput
			{
				message = "no definition exists for the name",
				isFailure = true
			});
			return new BuildImageOutput
			{
				service = id
			};
		}
		
		progressMessage?.Invoke(new ServicesBuiltProgress
		{
			completedSteps = 0,
			totalSteps = estimatedSteps
		});
		
		logMessage?.Invoke(new ServicesBuildCommandOutput
		{
			message = "building tarball..."
		});
		var tarStream = await BeamoLocalSystem.GetTarfile(id, provider);
		
		progressMessage?.Invoke(new ServicesBuiltProgress
		{
			completedSteps = tarBallSteps,
			totalSteps = estimatedSteps
		});
		
		logMessage?.Invoke(new ServicesBuildCommandOutput
		{
			message = "starting docker build..."
		});

		var tagString = string.Join(" ", tags.Select(tag => $"-t {id.ToLowerInvariant()}:{tag}"));
		var argString = $"buildx build - -f {http.RelativeDockerfilePath} " +
		                $"{tagString} " +
		                $"--progress rawjson " +
		                $"{(forceCpu ? "--platform linux/amd64 " : "")} " +
		                $"{(noCache ? "--no-cache " : "")}" +
		                $"{(pull ? "--pull " : "")}" +
		                $"--label \"beamoId={id.ToLowerInvariant()}\"";

		Log.Verbose($"running docker command with args=[{argString}]");
		var buffer = new StringBuilder();
		var digestToVertex = new Dictionary<string, BuildkitVertex>();
		var idToStatus = new Dictionary<string, BuildkitStatus>();
		string imageId = null; 
		bool TryParse(out BuildkitMessage msg)
		{
			string json = buffer.ToString();
			try
			{
				msg = System.Text.Json.JsonSerializer.Deserialize<BuildkitMessage>(json,
					new JsonSerializerOptions { IncludeFields = true });
				buffer.Clear();
				return true;
			}
			catch
			{
				msg = null;
				return false;
			}
		}

		void PostMessage(BuildkitMessage msg)
		{
			// post any new vertexes
			foreach (var vertex in msg.vertexes)
			{
				var wasStarted = false;
				var wasFailed = false;
				if (digestToVertex.TryGetValue(vertex.digest, out var oldVertex))
				{
					wasStarted = oldVertex.IsStarted;
					wasFailed = oldVertex.IsFailed;
				}
				
				if (!wasStarted && vertex.IsStarted)
				{
					// this is the first time we're seeing it, 
					//  it is docker-idiomatic to log the step
					logMessage?.Invoke(new ServicesBuildCommandOutput
					{
						message = vertex.Display
					});
				}

				if (!wasFailed && vertex.IsFailed)
				{
					logMessage?.Invoke(new ServicesBuildCommandOutput
					{
						message = $"[failed] {vertex.error}",
						isFailure = true
					});
				}
				
				// always overwrite the vertex. Docker will change the started and completed times
				digestToVertex[vertex.digest] = vertex;
			}
			
			// compute the total steps
			var totalSteps = tarBallSteps + digestToVertex.Count;
			var completedSteps = tarBallSteps + digestToVertex.Count(kvp => kvp.Value.IsCompleted);
			
			progressMessage?.Invoke(new ServicesBuiltProgress
			{
				completedSteps = completedSteps,
				totalSteps = totalSteps + stepPadding // add some padding to help mitigate when docker emits vertexes slowly
			});
			
			// check for log updates on vertex data
			foreach (var log in msg.logs)
			{
				if (!digestToVertex.ContainsKey(log.vertex))
				{
					Log.Warning($"Received docker log for non-existent vertex=[{log.vertex}] message=[{log.DecodedMessage}]");
					continue;
				}

				logMessage?.Invoke(new ServicesBuildCommandOutput
				{
					message = log.DecodedMessage
				});
			}
			
			// check for status updates on vertex data
			foreach (var status in msg.statuses)
			{
				// there is a status with the follow format, 
				//  writing image sha256:d3150be3b218a7f0327310ad97e06f7a1cd708548d01707371676262781b48f3
				// we can use this to extract the image id without a second docker call!
				const string prefix = "writing image ";
				if (status.id?.StartsWith(prefix) ?? false)
				{
					imageId = status.id.Substring(prefix.Length + 1);
					Log.Verbose($"identified image id for service=[{id}] from line=[{status.id}] image=[{imageId}]");
				}

				var wasStarted = false;
				if (idToStatus.TryGetValue(status.id, out var oldStatus))
				{
					wasStarted = oldStatus.IsStarted;
				}

				idToStatus[status.id] = status;
				if (!wasStarted && status.IsStarted)
				{
					logMessage?.Invoke(new ServicesBuildCommandOutput { message = status.id });
				}
			}
			
		}

		var command = Cli
			.Wrap(dockerPath)
			.WithArguments(argString)
			.WithValidation(CommandResultValidation.None)
			.WithStandardInputPipe(PipeSource.FromStream(tarStream))
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				Log.Warning("Unexpected Docker output: " + line);
			}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
			{
				lock (buffer)
				{
					buffer.Append(line);
					if (TryParse(out var msg))
					{
						PostMessage(msg);
					}
				}
			}));
			
		
		var result = await command.ExecuteAsync();
		var isSuccess = result.ExitCode == 0;
		if (isSuccess)
		{
			progressMessage?.Invoke(new ServicesBuiltProgress
			{
				completedSteps = 1,
				totalSteps = 1
			});
		}
		
		
		return new BuildImageOutput
		{
			success = isSuccess,
			service = id,
			fullImageId = imageId
		};
	}

	/// <summary>
	/// From observation, the structure of the rawjson mostly follows the proto definition,
	/// https://github.com/moby/buildkit/blob/master/api/services/control/control.proto#L115
	/// of the StatusResponse
	/// </summary>
	class BuildkitMessage
	{
		public List<BuildkitVertex> vertexes = new List<BuildkitVertex>();
		public List<BuildkitStatus> statuses = new List<BuildkitStatus>();
		public List<BuildkitLogs> logs = new List<BuildkitLogs>();
		public List<VertexWarning> warnings = new List<VertexWarning>();
	}

	/// <summary>
	/// a vertex represents a build step in the docker-file.
	/// buildkit will publish all the vertexes and then publish status and log updates for each vertex.
	/// </summary>
	class BuildkitVertex
	{
		public string digest;
		public string name;
		public string error;
		public bool? cached;
		public DateTime? started;
		public DateTime? completed;

		public bool IsStarted => started.HasValue;
		public bool IsCompleted => completed.HasValue;
		public bool IsCached => cached.HasValue && cached.Value;
		public bool IsFailed => !string.IsNullOrEmpty(error);

		public string Display => $"{(IsCached ? "[cached] " : "")}{name}";
		
		// this will show graph dependencies between the vertex info, but I don't think we actually need it.
		public List<string> inputs = new List<string>(); 
	}

	class BuildkitLogs
	{
		public string vertex;
		public DateTimeOffset timestamp;
		public int stream;
		
		/// <summary>
		/// the proto definition shows a byte[] msg stream,
		/// but from observation, this is a base64 encoded string.
		/// </summary>
		public string data;

		public string DecodedMessage => Encoding.UTF8.GetString(Convert.FromBase64String(data));
	}

	class BuildkitStatus
	{
		public string id;
		public string vertex;
		public long current;
		public long total;
		public DateTime? timestamp;
		public DateTime? started;
		public DateTime? completed;
		public bool IsStarted => started.HasValue;
	}

	class VertexWarning
	{
		public string vertex;
		public long level;
		[JsonProperty("short")]
		public string shortMessage;

		public List<string> detail = new List<string>();
		public string url;
	}
}

public struct BuildImageOutput
{
	public bool success;
	public string service;
	public string fullImageId; // in the form of sha256:203948q30497q235498q734056982304598135

	public string LongImageId // in the form of 203948q30497q235498q734056982304598135
	{
		get
		{
			var schemeIndex = fullImageId.IndexOf(':');
			if (schemeIndex > 0)
			{
				return fullImageId.Substring(schemeIndex + 1);
			}
			else
			{
				return fullImageId;
			}
		}
	}

	public string ShortImageId => LongImageId.Substring(0, Math.Min(12, LongImageId.Length)); // 203948q30497q2
	
	public string Display => $"{service}: {(success ? ShortImageId : "(failed)")}";
}
