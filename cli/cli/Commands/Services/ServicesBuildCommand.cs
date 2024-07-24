using cli.Dotnet;
using cli.Services;
using CliWrap;
using CliWrap.Buffered;
using MongoDB.Driver;
using Newtonsoft.Json;
using Serilog;
using Spectre.Console;
using System.Text;
using System.Text.Json;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

namespace cli;

public class ServicesBuildCommandArgs : CommandArgs
{
	public List<string> services = new List<string>();
	public List<string> withServiceTags = new List<string>();
	public List<string> withoutServiceTags = new List<string>();
}

public class ServicesBuildCommandOutput
{
	public string id;
	public string message;
}

public class ServicesBuiltProgress
{
	public string id;
	public int totalSteps;
	public int completedSteps;

	public float Ratio => completedSteps / (float)totalSteps;
}

public class ServicesBuildCommand : StreamCommand<ServicesBuildCommandArgs, ServicesBuildCommandOutput>
{
	public ServicesBuildCommand() : base("build", "build a set of services into docker images")
	{
	}

	public override void Configure()
	{
		ProjectCommand.AddIdsOption(this, (args, i) => args.services = i);
		ProjectCommand.AddServiceTagsOption(this, 
			bindWithTags: (args, i) => args.withServiceTags = i,
			bindWithoutTags: (args, i) => args.withoutServiceTags = i);

	}

	public override async Task Handle(ServicesBuildCommandArgs args)
	{
		ProjectCommand.FinalizeServicesArg(args, 
			withTags: args.withServiceTags, 
			withoutTags: args.withoutServiceTags,
			includeStorage: false, 
			ref args.services);

		await AnsiConsole
			.Progress()
			.StartAsync(async ctx =>
			{
				var tasks = new List<Task>();
				foreach (var service in args.services)
				{
					var visual = ctx.AddTask(service);
					
					
					var buildTask = Build(args, service, msg =>
					{
						msg.id = service;
						Log.Information(msg.message);
						this.SendResults(msg);
					}, prog =>
					{
						prog.id = service;
						visual.Value = (prog.Ratio * visual.MaxValue);
					});
					tasks.Add(buildTask);
				}

				await Task.WhenAll(tasks);

			});
	}

	static async Task Build(CommandArgs args, 
		string id, 
		Action<ServicesBuildCommandOutput> logMessage=null, 
		Action<ServicesBuiltProgress> progressMessage=null,
		bool noCache=false)
	{
		// a fake number of "steps" that the tarball is allotted. 
		const int tarBallSteps = 2;
		const int estimatedSteps = 10;
		
		if (!args.BeamoLocalSystem.BeamoManifest.HttpMicroserviceLocalProtocols.TryGetValue(id, out var http))
		{
			logMessage?.Invoke(new ServicesBuildCommandOutput
			{
				message = "no service exists for the name"
			});
		}
		
		progressMessage?.Invoke(new ServicesBuiltProgress
		{
			completedSteps = 0,
			totalSteps = estimatedSteps // fake,
		});
		logMessage?.Invoke(new ServicesBuildCommandOutput
		{
			message = "building tarball..."
		});
		var tarStream = BeamoLocalSystem.GetTarfile(id, args);
		
		progressMessage?.Invoke(new ServicesBuiltProgress
		{
			completedSteps = tarBallSteps,
			totalSteps = estimatedSteps // fake,
		});
		
		logMessage?.Invoke(new ServicesBuildCommandOutput
		{
			message = "running docker command..."
		});
		// docker build - -f ./services/Toast/Dockerfile -t test --progress rawjson --platform linux/arm64
		var argString = $"buildx build - -f {http.RelativeDockerfilePath} -t {id.ToLowerInvariant()}:latest " +
		                $"--progress rawjson " +
		                $"--platform linux/amd64 " +
		                $"--no-cache " +
		                $"--label \"beamoId={id.ToLowerInvariant()}\"";

		var buffer = new StringBuilder();
		var shaToVertex = new Dictionary<string, BuildkitVertex>();

		bool TryParse(out BuildkitMessage msg)
		{
			string json;
			lock (buffer)
			{
				json = buffer.ToString();
			}
			try
			{
				lock (buffer)
				{
					msg = System.Text.Json.JsonSerializer.Deserialize<BuildkitMessage>(json,
						new JsonSerializerOptions { IncludeFields = true });
					Log.Warning(json);
					buffer.Clear();
					return true;
				}
			}
			catch
			{
				msg = null;
				return false;
			}
		}

		void PostMessage(BuildkitMessage msg)
		{
			if (msg.warnings.Count > 0)
			{
				
			}
			// post any new vertexes
			foreach (var vertex in msg.vertexes)
			{

				var wasStarted = false;
				if (shaToVertex.TryGetValue(vertex.digest, out var oldVertex))
				{
					wasStarted = oldVertex.IsStarted;
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
				
				// always overwrite the vertex. Docker will change the started and completed times
				shaToVertex[vertex.digest] = vertex;
			}
			
			// compute the total steps
			var totalSteps = tarBallSteps + shaToVertex.Count;
			var completedSteps = tarBallSteps + shaToVertex.Count(kvp => kvp.Value.IsCompleted);
			
			progressMessage?.Invoke(new ServicesBuiltProgress
			{
				completedSteps = completedSteps,
				totalSteps = totalSteps
			});
			
			// check for log updates on vertex data
			foreach (var log in msg.logs)
			{
				if (!shaToVertex.TryGetValue(log.vertex, out var vertex))
				{
					Log.Warning($"Received docker log for non-existent vertex=[{log.vertex}]");
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
				// don't know actually what to do with these...
				logMessage?.Invoke(new ServicesBuildCommandOutput
				{
					message = status.name
				});
			}
			
			
		}

		var command = CliWrap.Cli
			.Wrap("docker")
			.WithArguments(argString)
			.WithValidation(CommandResultValidation.None)
			.WithStandardInputPipe(PipeSource.FromStream(tarStream))
			.WithStandardOutputPipe(PipeTarget.ToDelegate(line =>
			{
				Log.Information("STDOUT: " + line);
			}))
			.WithStandardErrorPipe(PipeTarget.ToDelegate(line =>
			{
				buffer.Append(line);
				if (TryParse(out var msg))
				{
					PostMessage(msg);
				}
				
				// Log.Information("STDERR: " + line);
			}));
			
	
			

		var result = await command.ExecuteAsync();
		if (TryParse(out var msg))
		{
			PostMessage(msg);
		}
		
		progressMessage?.Invoke(new ServicesBuiltProgress
		{
			completedSteps = 1,
			totalSteps = 1
		});
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
		public string ID;
		public string name;
		public string vertex;
		public long current;
		public long total;
		public DateTimeOffset timestamp;
		public DateTimeOffset started;
		public DateTimeOffset completed;
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
// 	message VertexWarning {
// 	string vertex = 1 [(gogoproto.customtype) = "github.com/opencontainers/go-digest.Digest", (gogoproto.nullable) = false];
// 	int64 level = 2;
// 	bytes short = 3;
// 	repeated bytes detail = 4;
// 	string url = 5;
// 	pb.SourceInfo info = 6;
// 	repeated pb.Range ranges = 7;
// }
}
