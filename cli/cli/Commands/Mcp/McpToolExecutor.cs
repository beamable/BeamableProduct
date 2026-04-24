using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Docs;
using cli.Services;
using Newtonsoft.Json;

namespace cli.Mcp;

public class McpToolExecutor
{
	private readonly string _cid;
	private readonly string _pid;

	// Serialize all in-process beam calls so Console.Out redirection and MSBuildLocator don't race.
	private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

	public McpToolExecutor(string cid, string pid)
	{
		_cid = cid;
		_pid = pid;
	}

	public async Task<string> GetTypeSchemaAsync(string section = "", string filter = "")
	{
		var norm = section?.Trim().ToLowerInvariant() ?? "";

		if (norm == "web")
			return BuildWebSdkResponse();

		// Fast path: read the embedded snapshot committed to the repo.
		var schema = McpListTypesCommand.ReadEmbeddedSchema();
		if (schema?.ContentTypes is { Length: > 0 })
			return BuildTypeResponse(schema, norm, filter?.Trim() ?? "");

		// Slow path: generate live via reflection (first build before AfterBuild regen).
		var cmd = "mcp list-types";
		if (!string.IsNullOrEmpty(norm)) cmd += $" --section {norm}";
		if (!string.IsNullOrEmpty(filter)) cmd += $" --filter \"{filter}\"";
		return await ExecuteAsync(cmd);
	}

	private static string BuildWebSdkResponse()
	{
		var webInfo = new
		{
			hint = "The Beamable Web SDK (TypeScript) documentation is hosted online. Read the portal extension's package.json to find the '@beamable/portal-toolkit' version in devDependencies, then use that major.minor version in the URL below.",
			docsUrlPattern = "https://help.beamable.com/WebSDK-{VERSION}/web/user-reference/api-reference/modules/",
			example = "https://help.beamable.com/WebSDK-1.0/web/user-reference/api-reference/modules/",
			sdkPackageName = "@beamable/portal-toolkit",
			versionSource = "devDependencies[\"@beamable/portal-toolkit\"] in the extension's package.json",
			usage = "Use beam_exec('services list') to find portal extensions, then read the extension's package.json to get the SDK version and build the docs URL."
		};
		return JsonConvert.SerializeObject(webInfo, Formatting.None);
	}

	private static string BuildTypeResponse(BeamableTypesSchema schema, string section, string filter)
	{
		if (string.IsNullOrEmpty(section))
		{
			// Overview: counts + namespace list so the AI knows what to request next.
			var namespaces = (schema.UtilityTypes ?? Array.Empty<UtilityTypeEntry>())
				.Select(t => t.Namespace)
				.Where(n => !string.IsNullOrEmpty(n))
				.Distinct()
				.OrderBy(n => n)
				.ToArray();

			var overview = new
			{
				hint = "Pass section='content', 'federation', 'utility', or 'web' to load types. For 'utility', also pass a filter string (namespace prefix or type name keyword) to narrow the large result set. For 'web', get the Beamable Web SDK documentation URL for portal extension development.",
				content = new { count = schema.ContentTypes?.Length ?? 0 },
				federation = new { count = schema.FederationTypes?.Length ?? 0 },
				utility = new { totalCount = schema.UtilityTypes?.Length ?? 0, namespaces },
				web = new { hint = "Pass section='web' to get the Beamable Web SDK (TypeScript) documentation URL for portal extension development" }
			};
			return JsonConvert.SerializeObject(overview, Formatting.None);
		}

		var filtered = McpListTypesCommand.ApplySectionFilter(schema, section, filter);
		return section switch
		{
			"content"    => JsonConvert.SerializeObject(filtered.ContentTypes, Formatting.None),
			"federation" => JsonConvert.SerializeObject(filtered.FederationTypes, Formatting.None),
			"utility"    => JsonConvert.SerializeObject(filtered.UtilityTypes, Formatting.None),
			_            => JsonConvert.SerializeObject(new { error = $"Unknown section '{section}'. Valid: content, federation, utility." }, Formatting.None)
		};
	}

	public Task<string> ExecuteHelpAsync(string commandPath)
	{
		var helpCommand = string.IsNullOrWhiteSpace(commandPath)
			? "--help"
			: $"{commandPath.Trim()} --help";
		return ExecuteAsync(helpCommand);
	}

	public async Task<string> ExecuteAsync(string commandLine)
	{
		await _lock.WaitAsync();
		try
		{
			return await RunInProcessAsync(commandLine);
		}
		finally
		{
			_lock.Release();
		}
	}

	private async Task<string> RunInProcessAsync(string commandLine)
	{
		var fullCommand = AppendRealmContext(commandLine);

		var sw = new StringWriter();
		var errSw = new StringWriter();
		var capturer = new CapturingReporterService(sw);

		var previousOut = Console.Out;
		var previousErr = Console.Error;
		Console.SetOut(sw);
		Console.SetError(errSw);

		try
		{
			var app = new App();
			app.Configure(
				builder =>
				{
					builder.Remove<IDataReporterService>();
					builder.AddSingleton<IDataReporterService>(capturer);
				},
				overwriteLogger: false);
			app.Build();

			// Run on a plain thread-pool thread to avoid deadlocking the ASP.NET
			// SynchronizationContext that the MCP host uses. Without this, CliWrap's
			// internal async continuations try to resume on the captured context, which
			// is already blocked waiting for this tool call to complete.
			await Task.Run(() => app.RunWithSingleString(fullCommand, useCustomSplitter: false)).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			sw.WriteLine($"{{\"error\":\"{ex.Message.Replace("\"", "\\\"")}\"}}");
		}
		finally
		{
			Console.SetOut(previousOut);
			Console.SetError(previousErr);
		}

		// CliException and other framework errors write to Console.Error, not stdout.
		// Append them as a JSON error line so the MCP client always sees them.
		var errorText = errSw.ToString().Trim();
		if (!string.IsNullOrEmpty(errorText))
			sw.WriteLine(JsonConvert.SerializeObject(new { error = errorText }));

		// Commands that implement IEmptyResult produce no output on success.
		// Emit a generic success envelope so the MCP client always receives a
		// non-empty response and knows the command completed without error.
		if (string.IsNullOrWhiteSpace(sw.ToString()))
			sw.WriteLine(JsonConvert.SerializeObject(new { status = "ok", command = commandLine }));

		return sw.ToString();
	}

	private string AppendRealmContext(string commandLine)
	{
		var lower = commandLine.ToLowerInvariant();
		var extra = string.Empty;

		// Route log messages through IDataReporterService so they are captured
		// in the MCP response alongside structured output.
		if (!lower.Contains("--emit-log-streams"))
			extra += " --emit-log-streams";

		if (!string.IsNullOrWhiteSpace(_cid) && !lower.Contains("--cid"))
			extra += $" --cid {_cid}";
		if (!string.IsNullOrWhiteSpace(_pid) && !lower.Contains("--pid"))
			extra += $" --pid {_pid}";

		return commandLine + extra;
	}

	private sealed class CapturingReporterService : IDataReporterService
	{
		private readonly TextWriter _writer;
		private readonly Dictionary<string, long> _lastProgressWrite = new();
		private const long ThrottleIntervalMs = 2000;

		private static readonly HashSet<string> _progressChannels = new(StringComparer.OrdinalIgnoreCase)
		{
			"progress", "progressStream", "remote_progress"
		};

		public CapturingReporterService(TextWriter writer)
		{
			_writer = writer;
		}

		public void Report<T>(string type, T data)
		{
			if (_progressChannels.Contains(type))
			{
				var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				if (_lastProgressWrite.TryGetValue(type, out var last) && now - last < ThrottleIntervalMs)
					return;
				_lastProgressWrite[type] = now;
			}

			var pt = new ReportDataPoint<T>
			{
				data = data,
				type = type,
				ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			};
			_writer.WriteLine(JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance));
		}
	}
}
