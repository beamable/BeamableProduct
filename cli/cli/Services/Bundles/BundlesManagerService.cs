using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.DeploymentCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace cli.Services.Bundles;

/// <summary>A request the bundles management API refuses; <see cref="Status"/> is the HTTP status to answer with.</summary>
public class BundleRequestException : Exception
{
	public int Status { get; }

	public BundleRequestException(int status, string message) : base(message)
	{
		Status = status;
	}
}

/// <summary>
/// The localhost API `beam bundles management` serves for the portal's Bundles page, like `beam server serve`
/// does for Unity. The page calls it straight from the browser; every operation runs one of the existing
/// `bundles` / `deploy` commands against this workspace, see <see cref="BundleJobRunner"/>.
/// <para>
/// It only listens on 127.0.0.1, and every request must carry this run's session token, come from the portal's
/// origin and address 127.0.0.1 by IP (a DNS-rebinding guard), because the commands run with this CLI's
/// credentials.
/// </para>
/// </summary>
public class BundlesManagementServer
{
	/// <summary>First port tried. Clear of `beam server serve`'s range (8342+), which `server ps` probes.</summary>
	public const int DEFAULT_PORT = 8360;
	const int MAX_PORT = DEFAULT_PORT + 100;

	readonly BundlesManagementContext _ctx;
	readonly string _allowedOrigin;
	readonly byte[] _token;
	HttpListener _listener;

	public int Port { get; private set; }

	/// <summary>The session secret the page must send as <c>Authorization: Bearer</c>. Fresh for every run.</summary>
	public string Token { get; }

	/// <param name="portalUrl">The portal the page runs in; only its origin may call the API.</param>
	public BundlesManagementServer(BundlesManagementContext ctx, string portalUrl, string token = null)
	{
		_ctx = ctx;
		_allowedOrigin = OriginOf(portalUrl);
		Token = token ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
			.TrimEnd('=').Replace('+', '-').Replace('/', '_');
		_token = Encoding.UTF8.GetBytes(Token);
	}

	/// <summary>Scheme, host and port of <paramref name="url"/>: what a browser sends as its <c>Origin</c>.</summary>
	public static string OriginOf(string url) => new Uri(url).GetLeftPart(UriPartial.Authority);

	/// <summary>Binds the first free port from <see cref="DEFAULT_PORT"/>.</summary>
	public void Start()
	{
		for (var port = DEFAULT_PORT; port <= MAX_PORT; port++)
		{
			var listener = new HttpListener();
			// 127.0.0.1 only: nothing on the network may reach an API that runs commands as this CLI.
			listener.Prefixes.Add($"http://127.0.0.1:{port}/");
			try
			{
				listener.Start();
				_listener = listener;
				Port = port;
				return;
			}
			catch (HttpListenerException)
			{
				listener.Close();
			}
		}

		throw new CliException($"No free port between {DEFAULT_PORT} and {MAX_PORT} for the bundles management API.");
	}

	/// <summary>Answers requests until <paramref name="token"/> is cancelled.</summary>
	public async Task Serve(CancellationToken token)
	{
		await using var stop = token.Register(() => _listener.Stop());
		while (!token.IsCancellationRequested)
		{
			HttpListenerContext context;
			try
			{
				context = await _listener.GetContextAsync();
			}
			catch (Exception) when (token.IsCancellationRequested)
			{
				break;
			}

			_ = Task.Run(() => Handle(context));
		}
	}

	async Task Handle(HttpListenerContext context)
	{
		var req = context.Request;
		var resp = context.Response;
		var origin = req.Headers["Origin"];
		int status;
		object body;
		try
		{
			(status, body) = Authorize(req.HttpMethod, req.Headers["Host"], origin, req.Headers["Authorization"]);
			if (status == 200)
			{
				if (req.HttpMethod == "OPTIONS")
				{
					status = 204;
					resp.Headers["Access-Control-Allow-Methods"] = "GET, POST";
					resp.Headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type";
					// Chrome's Private Network Access: lets an https portal call 127.0.0.1.
					resp.Headers["Access-Control-Allow-Private-Network"] = "true";
					resp.Headers["Access-Control-Max-Age"] = "600";
				}
				else
				{
					string text;
					using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
					{
						text = await reader.ReadToEndAsync();
					}

					body = Dispatch(req.HttpMethod, req.Url!.AbsolutePath, req.QueryString["fromLog"], text);
				}
			}
		}
		catch (BundleRequestException ex)
		{
			(status, body) = (ex.Status, new { message = ex.Message });
		}
		catch (Exception ex)
		{
			(status, body) = (500, new { message = ex.Message });
		}

		try
		{
			// The portal's origin is only named back to the portal itself.
			if (origin == _allowedOrigin)
			{
				resp.Headers["Access-Control-Allow-Origin"] = _allowedOrigin;
				resp.Headers["Vary"] = "Origin";
			}

			resp.Headers["Cache-Control"] = "no-store";
			resp.StatusCode = status;
			if (body != null)
			{
				var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
				resp.ContentType = "application/json; charset=utf-8";
				await resp.OutputStream.WriteAsync(bytes);
			}
		}
		finally
		{
			resp.Close();
		}
	}

	/// <summary>
	/// Whether a request may reach the API: status 200, or the status and body to refuse it with. A preflight
	/// (<c>OPTIONS</c>) carries no credentials, so it only needs the right host and origin.
	/// </summary>
	public (int status, object body) Authorize(string method, string host, string origin, string authorization)
	{
		if (host != $"127.0.0.1:{Port}")
			return (403, new { message = "Address the bundles management API by 127.0.0.1." });
		if (origin != _allowedOrigin)
			return (403, new { message = $"Only {_allowedOrigin} may call the bundles management API." });
		if (method == "OPTIONS")
			return (200, null);

		const string prefix = "Bearer ";
		var presented = authorization != null && authorization.StartsWith(prefix, StringComparison.Ordinal)
			? Encoding.UTF8.GetBytes(authorization.Substring(prefix.Length))
			: Array.Empty<byte>();
		return CryptographicOperations.FixedTimeEquals(presented, _token)
			? (200, null)
			: (401, new { message = "Open the Bundles page from the link `beam bundles management` printed; it carries the session token." });
	}

	/// <summary>Routes an authorized request. Throws <see cref="BundleRequestException"/> for a bad one.</summary>
	public object Dispatch(string method, string path, string fromLog, string body)
	{
		var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
		switch (method, parts.Length > 0 ? parts[0] : "", parts.Length)
		{
			case ("GET", "workspace", 1):
				return Workspace();
			case ("GET", "jobs", 1):
				return _ctx.Jobs.All().Select(j => j.ToView(int.MaxValue)).ToArray();
			case ("POST", "jobs", 1):
			{
				JObject request;
				try
				{
					request = string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
				}
				catch (JsonException)
				{
					throw new BundleRequestException(400, "The request body is not a JSON object.");
				}

				string Field(string name) => request[name]?.Type == JTokenType.String ? request[name]!.Value<string>() : "";
				var op = Field("op");
				var commandArgs = BundleJobRunner.BuildArgs(op, Field("bundle"), Field("tag"), Field("checksum"),
					Field("comment"), Field("acl"), Field("scope"), request["flag"]?.Type == JTokenType.Boolean && request["flag"]!.Value<bool>());
				return _ctx.Jobs.Start(op, commandArgs).ToView(0);
			}
			case ("GET", "jobs", 2):
				return _ctx.Jobs.Get(parts[1]).ToView(int.TryParse(fromLog, out var from) ? from : 0);
			case ("POST", "jobs", 3) when parts[2] == "cancel":
				_ctx.Jobs.Cancel(parts[1]);
				return _ctx.Jobs.Get(parts[1]).ToView(int.MaxValue);
			default:
				throw new BundleRequestException(404, $"No route {method} {path}.");
		}
	}

	BundlesWorkspaceView Workspace()
	{
		var pins = _ctx.ConfigService.LoadManifestReferences() ?? new ManifestReferences();
		return new BundlesWorkspaceView
		{
			cid = _ctx.Cid,
			pid = _ctx.Pid,
			zid = _ctx.Zid,
			host = _ctx.Host,
			ownerAccountId = _ctx.OwnerAccountId.ToString(),
			ownerEmail = _ctx.OwnerEmail,
			workingDirectory = _ctx.ConfigService.BeamableWorkspace,
			realmPins = pins.realm.Select(kv => new BundlePinView { name = kv.Key, checksum = kv.Value }).ToArray(),
			zonePins = pins.zone.Select(kv => new BundlePinView { name = kv.Key, checksum = kv.Value }).ToArray(),
		};
	}
}

/// <summary>The CLI state <see cref="BundlesManagementServer"/> works with.</summary>
public class BundlesManagementContext
{
	public string Cid;
	public string Pid;
	public string Zid;
	public string Host;
	public long OwnerAccountId;
	public string OwnerEmail;
	public ConfigService ConfigService;
	public BundleJobRunner Jobs;
}

/// <summary>
/// Runs bundle operations as background jobs. Each job invokes the matching CLI command in a fresh in-process
/// <see cref="App"/> (as `beam server` and `beam mcp` do) and records what it reports. Jobs run one at a time:
/// the console is redirected for the duration and commands share the workspace files.
/// </summary>
public class BundleJobRunner
{
	const int MAX_JOBS = 50;

	// Values must start alphanumeric so nothing the portal sends can parse as an option.
	static readonly Regex BundleRefPattern = new(@"^@?[A-Za-z0-9][A-Za-z0-9._-]*(/[A-Za-z0-9][A-Za-z0-9._-]*)?(@[A-Za-z0-9][A-Za-z0-9._:-]*)?$", RegexOptions.Compiled);
	static readonly Regex TagPattern = new(@"^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$", RegexOptions.Compiled);
	static readonly Regex ChecksumPattern = new(@"^sha256:[0-9a-fA-F]{8,128}$", RegexOptions.Compiled);
	static readonly string[] AclValues = { "private", "org", "public" };
	static readonly string[] ScopeValues = { "realm", "zone" };

	readonly SemaphoreSlim _gate = new(1, 1);
	readonly ConcurrentDictionary<string, BundleJob> _jobs = new();
	readonly string[] _globalArgs;

	/// <param name="globalArgs">Options every job runs with, so it targets the same cid/pid/host as this CLI.</param>
	public BundleJobRunner(string[] globalArgs)
	{
		_globalArgs = globalArgs;
	}

	/// <summary>Maps a whitelisted operation to the CLI arguments that perform it.</summary>
	public static string[] BuildArgs(string op, string bundle, string tag, string checksum, string comment,
		string acl, string scope, bool flag)
	{
		string Bundle()
		{
			if (string.IsNullOrEmpty(bundle) || !BundleRefPattern.IsMatch(bundle))
				throw new BundleRequestException(400, $"Invalid bundle reference [{bundle}].");
			return bundle;
		}

		string Tag()
		{
			if (!TagPattern.IsMatch(tag ?? ""))
				throw new BundleRequestException(400, $"Invalid tag [{tag}].");
			return tag;
		}

		string OneOf(string value, string[] allowed, string what)
		{
			if (!allowed.Contains(value))
				throw new BundleRequestException(400, $"Invalid {what} [{value}]. Expected one of: {string.Join(", ", allowed)}.");
			return value;
		}

		var args = new List<string>();
		switch (op)
		{
			case "list":
				args.AddRange(new[] { "bundles", "list" });
				break;
			case "get":
				args.AddRange(new[] { "bundles", "get", Bundle() });
				break;
			case "releases":
				args.AddRange(new[] { "bundles", "releases", Bundle() });
				break;
			case "install":
				args.AddRange(new[] { "bundles", "install", Bundle() });
				if (!string.IsNullOrEmpty(checksum))
				{
					if (!ChecksumPattern.IsMatch(checksum))
						throw new BundleRequestException(400, $"Invalid checksum [{checksum}].");
					args.AddRange(new[] { "--checksum", checksum });
				}
				else if (!string.IsNullOrEmpty(tag))
				{
					args.AddRange(new[] { "--tag", Tag() });
				}
				break;
			case "uninstall":
				args.AddRange(new[] { "bundles", "uninstall", Bundle() });
				break;
			case "prune-yanked":
				args.AddRange(new[] { "bundles", "prune-yanked" });
				if (flag) args.Add("--remove");
				break;
			case "plan":
				args.AddRange(new[] { "bundles", "plan", Bundle() });
				break;
			case "publish":
				args.AddRange(new[] { "bundles", "publish", Bundle() });
				// flag = publish exactly what the last `plan` job showed.
				if (flag) args.Add("--from-latest-plan");
				if (!string.IsNullOrEmpty(tag)) args.AddRange(new[] { "--tag", Tag() });
				if (!string.IsNullOrEmpty(acl)) args.AddRange(new[] { "--acl", OneOf(acl, AclValues, "acl") });
				if (!string.IsNullOrWhiteSpace(comment)) args.Add(CommentOption(comment));
				break;
			case "tag":
				args.AddRange(new[] { "bundles", "tag", Bundle(), Tag() });
				break;
			case "yank":
				args.AddRange(new[] { "bundles", "yank", Bundle() });
				break;
			case "acl":
				args.AddRange(new[] { "bundles", "acl", Bundle(), "--scope", OneOf(acl, AclValues, "acl") });
				break;
			case "deploy-plan":
				args.AddRange(new[] { "deploy", "plan", "--scope", OneOf(scope, ScopeValues, "scope") });
				if (!string.IsNullOrWhiteSpace(comment)) args.Add(CommentOption(comment));
				break;
			case "deploy-release":
				args.AddRange(new[] { "deploy", "release", "--from-latest-plan", "--scope", OneOf(scope, ScopeValues, "scope") });
				// Release clears the plan's comments and applies its own, so the comment goes here too.
				if (!string.IsNullOrWhiteSpace(comment)) args.Add(CommentOption(comment));
				break;
			default:
				throw new BundleRequestException(400, $"Unknown operation [{op}].");
		}

		return args.ToArray();
	}

	/// <summary>One token, so a comment that starts with a dash is not parsed as an option.</summary>
	static string CommentOption(string comment) => "--comment=" + comment.Trim();

	public BundleJob Start(string op, string[] commandArgs)
	{
		var job = new BundleJob
		{
			id = Guid.NewGuid().ToString("N").Substring(0, 12),
			op = op,
			commandLine = "beam " + string.Join(" ", commandArgs.Select(a => a.Contains(' ') ? $"\"{a}\"" : a)),
			createdAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
		};
		_jobs[job.id] = job;
		foreach (var old in _jobs.Values.OrderByDescending(j => j.createdAt).Skip(MAX_JOBS).ToList())
		{
			_jobs.TryRemove(old.id, out _);
		}

		_ = Task.Run(() => Run(job, commandArgs));
		return job;
	}

	public BundleJob Get(string jobId)
	{
		if (string.IsNullOrEmpty(jobId) || !_jobs.TryGetValue(jobId, out var job))
			throw new BundleRequestException(404, $"No job [{jobId}].");
		return job;
	}

	public IEnumerable<BundleJob> All() => _jobs.Values.OrderByDescending(j => j.createdAt);

	public void Cancel(string jobId)
	{
		var job = Get(jobId);
		job.cancelRequested = true;
		job.lifecycle?.Cancel();
	}

	async Task Run(BundleJob job, string[] commandArgs)
	{
		await _gate.WaitAsync();
		var previousOut = Console.Out;
		var previousErr = Console.Error;
		var errors = new StringWriter();
		try
		{
			if (job.cancelRequested)
			{
				job.status = BundleJob.STATUS_CANCELLED;
				return;
			}

			job.status = BundleJob.STATUS_RUNNING;
			job.startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			Console.SetOut(TextWriter.Null);
			Console.SetError(errors);

			var app = new App();
			app.Configure(builder =>
			{
				builder.Remove<IDataReporterService>();
				builder.AddSingleton<IDataReporterService, BundleJobReporter>(provider =>
				{
					job.lifecycle = provider.GetService<AppLifecycle>();
					return new BundleJobReporter(job);
				});
			}, overwriteLogger: false);
			app.Build();

			// --quiet: publish and release otherwise block on a "type 'yes'" prompt nobody can answer.
			// --raw: the CLI only reports an exception to the reporter when output is piped or raw, and a failed
			// request still exits 0, so without it an API error from a CLI started in a terminal is lost.
			// --logs i: the default (warnings) leaves a healthy job silent; info says what each stage is doing.
			var fullArgs = commandArgs.Concat(_globalArgs)
				.Concat(new[] { "--quiet", "--raw", "--emit-log-streams", "--logs", "i" }).ToArray();
			var exitCode = await Task.Run(() => app.RunAsync(fullArgs)).ConfigureAwait(false);

			// The CLI writes its human-readable output (tables, prompts) to stderr too, so stderr is only the
			// error when the command failed without reporting one on an error channel.
			var stderr = errors.ToString().Trim();
			if (exitCode != 0 && job.errors.Count == 0 && !string.IsNullOrEmpty(stderr))
			{
				job.AddError(stderr);
			}

			job.status = job.cancelRequested
				? BundleJob.STATUS_CANCELLED
				: exitCode == 0 && job.errors.Count == 0
					? BundleJob.STATUS_SUCCEEDED
					: BundleJob.STATUS_FAILED;
		}
		catch (Exception ex)
		{
			job.AddError(ex.Message);
			job.status = BundleJob.STATUS_FAILED;
		}
		finally
		{
			Console.SetOut(previousOut);
			Console.SetError(previousErr);
			job.finishedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
			job.lifecycle = null;
			_gate.Release();
		}
	}
}

/// <summary>Collects what a job's command reports: logs, errors, and the latest payload of every other channel.</summary>
public class BundleJobReporter : IDataReporterService
{
	readonly BundleJob _job;

	public BundleJobReporter(BundleJob job)
	{
		_job = job;
	}

	public void Report<T>(string type, T data)
	{
		var json = JsonConvert.SerializeObject(data, UnitySerializationSettings.Instance);
		if (type == "logs" && data is Beamable.Common.BeamCli.Contracts.CliLogMessage log)
		{
			_job.AddLog(log.logLevel, log.message, log.timestamp);
			return;
		}

		// plan, publish and deploy report each of their steps (fetching, every service build, uploads) on this channel.
		if (data is PlanReleaseProgress progress)
		{
			_job.SetProgress(progress.name, progress.ratio, progress.isKnownLength, progress.serviceName);
			return;
		}

		if (type.StartsWith("error", StringComparison.OrdinalIgnoreCase))
		{
			var message = JObject.Parse(json)["message"]?.ToString();
			_job.AddError(string.IsNullOrEmpty(message) ? json : message);
			return;
		}

		_job.SetChannel(type, json);
	}
}

public class BundleJob
{
	public const string STATUS_QUEUED = "queued";
	public const string STATUS_RUNNING = "running";
	public const string STATUS_SUCCEEDED = "succeeded";
	public const string STATUS_FAILED = "failed";
	public const string STATUS_CANCELLED = "cancelled";

	public string id;
	public string op;
	public string commandLine;
	public volatile string status = STATUS_QUEUED;
	public long createdAt;
	public long startedAt;
	public long finishedAt;
	public volatile bool cancelRequested;
	public AppLifecycle lifecycle;

	readonly object _lock = new();
	readonly List<BundleJobLogView> _logs = new();
	public readonly List<string> errors = new();
	readonly Dictionary<string, string> _channels = new();
	readonly List<BundleJobProgressView> _progress = new();

	public void AddLog(string level, string message, long ts)
	{
		lock (_lock) _logs.Add(new BundleJobLogView { level = level, message = message, ts = ts });
	}

	public void AddError(string message)
	{
		lock (_lock)
		{
			errors.Add(message);
			_logs.Add(new BundleJobLogView { level = "Error", message = message, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
		}
	}

	public void SetChannel(string channel, string json)
	{
		lock (_lock) _channels[channel] = json;
	}

	/// <summary>Records a step's progress, keeping the steps in the order they started. A ratio below 0 means it failed.</summary>
	public void SetProgress(string name, float ratio, bool isKnownLength, string serviceName)
	{
		lock (_lock)
		{
			var step = _progress.FirstOrDefault(p => p.name == name);
			if (step == null) _progress.Add(step = new BundleJobProgressView { name = name });
			step.ratio = ratio;
			step.isKnownLength = isKnownLength;
			step.serviceName = serviceName;
		}
	}

	public BundleJobView ToView(int fromLog)
	{
		lock (_lock)
		{
			return new BundleJobView
			{
				id = id,
				op = op,
				commandLine = commandLine,
				status = status,
				createdAt = createdAt,
				startedAt = startedAt,
				finishedAt = finishedAt,
				logCount = _logs.Count,
				logs = _logs.Skip(Math.Max(0, fromLog)).ToArray(),
				errors = errors.ToArray(),
				progress = _progress.Select(p => new BundleJobProgressView
				{
					name = p.name, ratio = p.ratio, isKnownLength = p.isKnownLength, serviceName = p.serviceName
				}).ToArray(),
				// The command's result; the remaining channels carry progress and build errors.
				resultJson = _channels.TryGetValue("stream", out var result) ? result : null,
				channels = _channels.Where(kv => kv.Key != "stream")
					.Select(kv => new BundleJobChannelView { channel = kv.Key, json = kv.Value }).ToArray(),
			};
		}
	}
}

[Serializable]
public class BundlesWorkspaceView
{
	public string cid;
	public string pid;
	public string zid;
	public string host;
	/// <summary>A string: account ids exceed what a JavaScript number holds exactly.</summary>
	public string ownerAccountId;
	public string ownerEmail;
	public string workingDirectory;
	public BundlePinView[] realmPins;
	public BundlePinView[] zonePins;
}

[Serializable]
public class BundlePinView
{
	public string name;
	public string checksum;
}

[Serializable]
public class BundleJobView
{
	public string id;
	public string op;
	public string commandLine;
	public string status;
	public long createdAt;
	public long startedAt;
	public long finishedAt;
	public int logCount;
	public BundleJobLogView[] logs;
	public string[] errors;
	public BundleJobProgressView[] progress;
	public string resultJson;
	public BundleJobChannelView[] channels;
}

[Serializable]
public class BundleJobLogView
{
	public string level;
	public string message;
	public long ts;
}

[Serializable]
public class BundleJobProgressView
{
	public string name;
	/// <summary>0..1; below 0 when the step failed.</summary>
	public float ratio;
	/// <summary>False for steps that only report started/done.</summary>
	public bool isKnownLength;
	public string serviceName;
}

[Serializable]
public class BundleJobChannelView
{
	public string channel;
	public string json;
}
