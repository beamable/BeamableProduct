using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.DeploymentCommands;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.LocalWeb;

/// <summary>A request a local web app's API refuses; <see cref="Status"/> is the HTTP status to answer with.</summary>
public class LocalWebRequestException : Exception
{
	public int Status { get; }

	public LocalWebRequestException(int status, string message) : base(message)
	{
		Status = status;
	}
}

/// <summary>The routes one local web app calls, under <c>/api/</c>.</summary>
public interface ILocalWebApi
{
	/// <summary>
	/// Answers an authorized request. <paramref name="parts"/> are the path's segments after <c>api</c>. Throws
	/// <see cref="LocalWebRequestException"/> for a bad one.
	/// </summary>
	object Dispatch(string method, string[] parts, NameValueCollection query, string body);
}

/// <summary>
/// Serves one of the CLI's local web apps (<c>beam local portal</c>, <c>beam bundles management</c>) on 127.0.0.1:
/// the page itself from the files built into this CLI (<see cref="LocalWebAssets"/>), and its API under
/// <c>/api/</c>, which runs operations with this CLI's credentials and files.
/// <para>
/// So only this machine can connect, every request must address 127.0.0.1 by IP (a DNS-rebinding guard), and API
/// calls must carry this run's session token and come from the page's own origin. The token reaches the page in
/// the URL fragment, which browsers never send to a server.
/// </para>
/// </summary>
public class LocalWebServer
{
	/// <summary>First port tried. Clear of `beam server serve`'s range (8342+), which `server ps` probes.</summary>
	public const int DEFAULT_PORT = 8360;
	const int MAX_PORT = DEFAULT_PORT + 100;

	readonly string _app;
	readonly ILocalWebApi _api;
	readonly byte[] _token;
	HttpListener _listener;

	public int Port { get; private set; }

	/// <summary>The session secret the page must send as <c>Authorization: Bearer</c>. Fresh for every run.</summary>
	public string Token { get; }

	/// <summary>What a browser sends as <c>Origin</c> for the page this server serves.</summary>
	public string Origin => $"http://127.0.0.1:{Port}";

	/// <summary>The page, carrying the session token in its fragment.</summary>
	public string Url => $"{Origin}/#token={Token}";

	/// <param name="app">The app's folder in <see cref="LocalWebAssets"/> (<c>stacks</c>, <c>bundles</c>).</param>
	public LocalWebServer(string app, ILocalWebApi api, string token = null)
	{
		_app = app;
		_api = api;
		Token = token ?? Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
			.TrimEnd('=').Replace('+', '-').Replace('/', '_');
		_token = Encoding.UTF8.GetBytes(Token);
	}

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

		throw new CliException($"No free port between {DEFAULT_PORT} and {MAX_PORT} for the local web app.");
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
		var path = req.Url!.AbsolutePath;
		try
		{
			resp.Headers["Cache-Control"] = "no-store";
			if (req.Headers["Host"] != $"127.0.0.1:{Port}")
			{
				await WriteJson(resp, 403, new { message = "Address the local web app by 127.0.0.1." });
				return;
			}

			if (path == "/api" || path.StartsWith("/api/", StringComparison.Ordinal))
			{
				await HandleApi(req, resp, path);
				return;
			}

			if (req.HttpMethod != "GET")
			{
				await WriteJson(resp, 405, new { message = $"{req.HttpMethod} {path} is not allowed." });
				return;
			}

			if (!LocalWebAssets.TryRead(_app, path, out var bytes, out var contentType))
			{
				await WriteJson(resp, 404, new
				{
					message = LocalWebAssets.Has(_app)
						? $"No file {path}."
						: "This CLI was built without its web apps (cli/local-web); build it with pnpm available."
				});
				return;
			}

			resp.StatusCode = 200;
			resp.ContentType = contentType;
			await resp.OutputStream.WriteAsync(bytes);
		}
		catch (Exception ex)
		{
			try
			{
				await WriteJson(resp, 500, new { message = ex.Message });
			}
			catch
			{
				// The response has already started; nothing more to tell the browser.
			}
		}
		finally
		{
			resp.Close();
		}
	}

	async Task HandleApi(HttpListenerRequest req, HttpListenerResponse resp, string path)
	{
		int status;
		object body;
		try
		{
			(status, body) = Authorize(req.Headers["Origin"], req.Headers["Authorization"]);
			if (status == 200)
			{
				string text;
				using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
				{
					text = await reader.ReadToEndAsync();
				}

				var parts = path.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries).Skip(1)
					.Select(Uri.UnescapeDataString).ToArray();
				body = _api.Dispatch(req.HttpMethod, parts, req.QueryString, text);
			}
		}
		catch (LocalWebRequestException ex)
		{
			(status, body) = (ex.Status, new { message = ex.Message });
		}

		await WriteJson(resp, status, body);
	}

	/// <summary>
	/// Whether an API call may go through: status 200, or the status and body to refuse it with. The page calls its
	/// API from its own origin; a browser sends no <c>Origin</c> on a same-origin GET, and any other origin is refused.
	/// </summary>
	public (int status, object body) Authorize(string origin, string authorization)
	{
		if (origin != null && origin != Origin)
			return (403, new { message = $"Only {Origin} may call this API." });

		const string prefix = "Bearer ";
		var presented = authorization != null && authorization.StartsWith(prefix, StringComparison.Ordinal)
			? Encoding.UTF8.GetBytes(authorization.Substring(prefix.Length))
			: Array.Empty<byte>();
		return CryptographicOperations.FixedTimeEquals(presented, _token)
			? (200, null)
			: (401, new { message = "Open the page from the link the CLI printed; it carries the session token." });
	}

	static async Task WriteJson(HttpListenerResponse resp, int status, object body)
	{
		resp.StatusCode = status;
		if (body == null) return;
		var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(body));
		resp.ContentType = "application/json; charset=utf-8";
		await resp.OutputStream.WriteAsync(bytes);
	}
}

/// <summary>
/// The web apps built into this CLI from <c>cli/local-web</c>: <c>LocalWeb/&lt;app&gt;/</c> (its page, script and
/// styles) and <c>LocalWeb/runtime/</c> (the portal's components and theme, from <c>@beamable/portal-toolkit</c>),
/// embedded under those names by cli.csproj.
/// </summary>
public static class LocalWebAssets
{
	const string Root = "LocalWeb/";

	static readonly Dictionary<string, string> Resources = typeof(LocalWebAssets).Assembly.GetManifestResourceNames()
		.Where(n => n.Replace('\\', '/').StartsWith(Root, StringComparison.Ordinal))
		.ToDictionary(n => n.Replace('\\', '/').Substring(Root.Length), n => n, StringComparer.Ordinal);

	static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
	{
		[".html"] = "text/html; charset=utf-8",
		[".js"] = "text/javascript; charset=utf-8",
		[".css"] = "text/css; charset=utf-8",
		[".json"] = "application/json; charset=utf-8",
		[".svg"] = "image/svg+xml",
		[".png"] = "image/png",
		[".woff2"] = "font/woff2",
	};

	/// <summary>Whether this CLI was built with <paramref name="app"/>.</summary>
	public static bool Has(string app) => Resources.ContainsKey($"{app}/index.html");

	/// <summary>
	/// The file a request path names: <c>/</c> is the app's page, <c>/runtime/…</c> the shared runtime, anything else
	/// a file of the app. Only names that were embedded are found, so no path can reach anything else.
	/// </summary>
	public static bool TryRead(string app, string path, out byte[] bytes, out string contentType)
	{
		bytes = null;
		contentType = null;
		var relative = path.TrimStart('/');
		var name = relative.Length == 0 ? $"{app}/index.html"
			: relative.StartsWith("runtime/", StringComparison.Ordinal) ? relative
			: $"{app}/{relative}";
		if (!Resources.TryGetValue(name, out var resource)) return false;

		using var stream = typeof(LocalWebAssets).Assembly.GetManifestResourceStream(resource);
		if (stream == null) return false;
		using var memory = new MemoryStream();
		stream.CopyTo(memory);
		bytes = memory.ToArray();
		contentType = ContentTypes.TryGetValue(Path.GetExtension(name), out var type) ? type : "application/octet-stream";
		return true;
	}
}

/// <summary>
/// Runs a local web app's operations as background jobs. Each job invokes a CLI command in a fresh in-process
/// <see cref="App"/> (as `beam server` and `beam mcp` do) and records what it reports. Jobs run one at a time: the
/// console is redirected for the duration and commands share the workspace files. Callers build the command's
/// arguments from a whitelist; nothing a page sends reaches the parser unchecked.
/// </summary>
public class CliJobRunner
{
	const int MAX_JOBS = 50;

	readonly SemaphoreSlim _gate = new(1, 1);
	readonly ConcurrentDictionary<string, CliJob> _jobs = new();
	readonly string[] _globalArgs;

	/// <param name="globalArgs">Options every job runs with, so it targets the same cid/pid/host as this CLI.</param>
	public CliJobRunner(string[] globalArgs)
	{
		_globalArgs = globalArgs;
	}

	public CliJob Start(string op, string[] commandArgs)
	{
		var job = new CliJob
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

	public CliJob Get(string jobId)
	{
		if (string.IsNullOrEmpty(jobId) || !_jobs.TryGetValue(jobId, out var job))
			throw new LocalWebRequestException(404, $"No job [{jobId}].");
		return job;
	}

	public IEnumerable<CliJob> All() => _jobs.Values.OrderByDescending(j => j.createdAt);

	public void Cancel(string jobId)
	{
		var job = Get(jobId);
		job.cancelRequested = true;
		job.lifecycle?.Cancel();
	}

	/// <summary>The routes every app with jobs shares: list, start (with <paramref name="buildArgs"/>), poll and cancel.</summary>
	public object Dispatch(string method, string[] parts, NameValueCollection query, string body,
		Func<JObject, (string op, string[] args)> buildArgs)
	{
		switch (method, parts.Length)
		{
			case ("GET", 1):
				return All().Select(j => j.ToView(int.MaxValue)).ToArray();
			case ("POST", 1):
			{
				var (op, args) = buildArgs(ParseBody(body));
				return Start(op, args).ToView(0);
			}
			case ("GET", 2):
				return Get(parts[1]).ToView(int.TryParse(query?["fromLog"], out var from) ? from : 0);
			case ("POST", 3) when parts[2] == "cancel":
				Cancel(parts[1]);
				return Get(parts[1]).ToView(int.MaxValue);
			default:
				throw new LocalWebRequestException(404, $"No route {method} /api/{string.Join('/', parts)}.");
		}
	}

	/// <summary>A request body as a JSON object; an empty body is an empty object.</summary>
	public static JObject ParseBody(string body)
	{
		try
		{
			return string.IsNullOrWhiteSpace(body) ? new JObject() : JObject.Parse(body);
		}
		catch (JsonException)
		{
			throw new LocalWebRequestException(400, "The request body is not a JSON object.");
		}
	}

	async Task Run(CliJob job, string[] commandArgs)
	{
		await _gate.WaitAsync();
		var previousOut = Console.Out;
		var previousErr = Console.Error;
		var errors = new StringWriter();
		try
		{
			if (job.cancelRequested)
			{
				job.status = CliJob.STATUS_CANCELLED;
				return;
			}

			job.status = CliJob.STATUS_RUNNING;
			job.startedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

			Console.SetOut(TextWriter.Null);
			Console.SetError(errors);

			var app = new App();
			app.Configure(builder =>
			{
				builder.Remove<IDataReporterService>();
				builder.AddSingleton<IDataReporterService, CliJobReporter>(provider =>
				{
					job.lifecycle = provider.GetService<AppLifecycle>();
					return new CliJobReporter(job);
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
				? CliJob.STATUS_CANCELLED
				: exitCode == 0 && job.errors.Count == 0
					? CliJob.STATUS_SUCCEEDED
					: CliJob.STATUS_FAILED;
		}
		catch (Exception ex)
		{
			job.AddError(ex.Message);
			job.status = CliJob.STATUS_FAILED;
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
public class CliJobReporter : IDataReporterService
{
	readonly CliJob _job;

	public CliJobReporter(CliJob job)
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

public class CliJob
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
	readonly List<CliJobLogView> _logs = new();
	public readonly List<string> errors = new();
	readonly Dictionary<string, string> _channels = new();
	readonly List<CliJobProgressView> _progress = new();

	public void AddLog(string level, string message, long ts)
	{
		lock (_lock) _logs.Add(new CliJobLogView { level = level, message = message, ts = ts });
	}

	public void AddError(string message)
	{
		lock (_lock)
		{
			errors.Add(message);
			_logs.Add(new CliJobLogView { level = "Error", message = message, ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });
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
			if (step == null) _progress.Add(step = new CliJobProgressView { name = name });
			step.ratio = ratio;
			step.isKnownLength = isKnownLength;
			step.serviceName = serviceName;
		}
	}

	public CliJobView ToView(int fromLog)
	{
		lock (_lock)
		{
			return new CliJobView
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
				progress = _progress.Select(p => new CliJobProgressView
				{
					name = p.name, ratio = p.ratio, isKnownLength = p.isKnownLength, serviceName = p.serviceName
				}).ToArray(),
				// The command's result; the remaining channels carry progress, build errors and listings.
				resultJson = _channels.TryGetValue("stream", out var result) ? result : null,
				channels = _channels.Where(kv => kv.Key != "stream")
					.Select(kv => new CliJobChannelView { channel = kv.Key, json = kv.Value }).ToArray(),
			};
		}
	}
}

[Serializable]
public class CliJobView
{
	public string id;
	public string op;
	public string commandLine;
	public string status;
	public long createdAt;
	public long startedAt;
	public long finishedAt;
	public int logCount;
	public CliJobLogView[] logs;
	public string[] errors;
	public CliJobProgressView[] progress;
	public string resultJson;
	public CliJobChannelView[] channels;
}

[Serializable]
public class CliJobLogView
{
	public string level;
	public string message;
	public long ts;
}

[Serializable]
public class CliJobProgressView
{
	public string name;
	/// <summary>0..1; below 0 when the step failed.</summary>
	public float ratio;
	/// <summary>False for steps that only report started/done.</summary>
	public bool isKnownLength;
	public string serviceName;
}

[Serializable]
public class CliJobChannelView
{
	public string channel;
	public string json;
}
