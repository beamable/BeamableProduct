using Beamable.Common.BeamCli;
using Beamable.Common.BeamCli.Contracts;
using Beamable.Server.Common;
using cli.Services;
using cli.Services.CliInvocation;
using cli.Services.HttpServer;
using ModelContextProtocol;
using ModelContextProtocol.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Reflection;
using System.Text;
using static cli.ConfigService;

namespace cli.Commands.Mcp;

public class McpToolExecutor
{
	/// <summary>Creates the app each command runs in. Tests replace it to control what a command does.</summary>
	public Func<App> AppFactory { get; set; } = () => new App();

	public static List<(string name, string content)> GetEmbeddedSkills()
	{
		var assembly = typeof(McpToolExecutor).Assembly;
		const string prefix = "cli.Docs.Skills.";
		const string suffix = ".md";

		return assembly.GetManifestResourceNames()
			.Where(r => r.StartsWith(prefix) && r.EndsWith(suffix))
			.OrderBy(r => r)
			.Select(r => (
				name: r.Substring(prefix.Length, r.Length - prefix.Length - suffix.Length),
				content: ReadEmbeddedResource(assembly, r)
			))
			.ToList();
	}

	public static Task<string> GetSkillAsync(string skillName = "")
	{
		var skills = GetEmbeddedSkills()
			.Select(s => (s.name, summary: ExtractDescription(s.content), s.content))
			.ToList();

		var normalized = skillName?.Trim().ToLowerInvariant().Replace(" ", "-") ?? "";

		if (string.IsNullOrEmpty(normalized))
		{
			var catalog = skills.Select(s => new { name = s.name, summary = s.summary });
			return Task.FromResult(JsonConvert.SerializeObject(catalog, Formatting.None));
		}

		var match = skills.FirstOrDefault(s =>
			s.name.Equals(normalized, StringComparison.OrdinalIgnoreCase));

		if (match == default)
		{
			var available = string.Join(", ", skills.Select(s => s.name));
			return Task.FromResult(
				JsonConvert.SerializeObject(new { error = $"Unknown skill '{skillName}'. Available: {available}" }));
		}

		return Task.FromResult(match.content);
	}

	public static string ExtractDescription(string content)
	{
		if (!content.StartsWith("---"))
			return content.Split('\n', 2)[0].Trim();

		foreach (var line in content.Split('\n'))
		{
			if (line.StartsWith("description:", StringComparison.OrdinalIgnoreCase))
				return line.Substring("description:".Length).Trim();
		}

		return content.Split('\n', 2)[0].Trim();
	}

	private static string ReadEmbeddedResource(Assembly assembly, string resourceName)
	{
		using var stream = assembly.GetManifestResourceStream(resourceName);
		if (stream == null) return string.Empty;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}

	private const int DefaultFileReadLimit = 65_536;

	public Task<string> GetSourceCode(string platform = "", string version = "", string filePath = "", int offset = 0, int limit = 0)
	{
		try
		{
			var startDir = Directory.GetCurrentDirectory();
			var normalizedPlatform = platform?.Trim().ToLowerInvariant() ?? "";
			var detectedVersion = version?.Trim() ?? "";

			if (string.IsNullOrEmpty(normalizedPlatform) || string.IsNullOrEmpty(detectedVersion))
			{
				var detected = TryDetectUnity(startDir)
				               ?? TryDetectWebSdk(startDir)
				               ?? TryDetectCli(startDir)
				               ?? TryDetectUnreal(startDir);

				if (detected == null)
				{
					var result = new
					{
						error = "Could not auto-detect Beamable SDK platform or version",
						hint = "Pass platform ('unity', 'cli', 'web', 'unreal') and version explicitly," +
						       " or run this tool from within a project directory that has Beamable installed",
						searchedFrom = startDir
					};
					return Task.FromResult(JsonConvert.SerializeObject(result, Formatting.None));
				}

				if (string.IsNullOrEmpty(normalizedPlatform))
					normalizedPlatform = detected.Value.platform;
				if (string.IsNullOrEmpty(detectedVersion))
					detectedVersion = detected.Value.version;
			}

			detectedVersion = NormalizeVersion(detectedVersion);

			string sourcePath;
			string[] commonPaths;
			string hint;
			string fileFull = null;

			switch (normalizedPlatform)
			{
				case "unity":
				{
					var localPkg = FindUnityPackageCache(startDir, detectedVersion);
					if (localPkg != null)
					{
						sourcePath = localPkg;
						commonPaths = new[] { Path.Combine(localPkg, "Runtime"), Path.Combine(localPkg, "Common") };
						hint = "Unity SDK source is local in the Library PackageCache. Read files directly.";
					}
					else
					{
						sourcePath = $"https://github.com/beamable/BeamableProduct/tree/unity-sdk-{detectedVersion}";
						commonPaths = new[] { "client/Packages/com.beamable/", "client/Packages/com.beamable.server/" };
						hint = "Unity SDK local PackageCache not found. Falling back to GitHub URL.";
					}
					break;
				}
				case "cli":
				{
					var nugetBase = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
						".nuget", "packages");
					var commonSrc = Path.Combine(nugetBase, "beamable.common", detectedVersion, "content", "sourceCode", "Runtime");
					var toolingSrc = Path.Combine(nugetBase, "beamable.tooling.common", detectedVersion, "content", "sourceCode");
					var runtimeSrc = Path.Combine(nugetBase, "beamable.microservice.runtime", detectedVersion, "content", "sourceCode");
					var runtimeBuild = Path.Combine(nugetBase, "beamable.microservice.runtime", detectedVersion, "build");
					// build folders needs to use Target Framework path because NuGet auto-imports .props/.targets from build/{TargetFramework}/
					var commonBuild = Path.Combine(nugetBase, "beamable.common", detectedVersion, "build", "netstandard2.0");

					sourcePath = nugetBase;
					commonPaths = new[] { commonSrc, toolingSrc, runtimeSrc, runtimeBuild, commonBuild };
					hint = "Read files from the 'commonPaths' directories listed below — those are the actual source locations." +
					       " Do NOT read from 'sourcePath' directly in sandbox environments (it is the broad NuGet cache root)." +
					       " commonPaths[0] (beamable.common) has content types, APIs, and Optional<T>." +
					       " commonPaths[1] (beamable.tooling.common) has callable attributes, federation interfaces, and storage." +
					       " commonPaths[2] (beamable.microservice.runtime) has the Microservice base class and API implementations." +
					       " commonPaths[3] (beamable.microservice.runtime/build) has MSBuild .targets and .props for OAPI gen, build validation, and collector resolution." +
					       " commonPaths[4] (beamable.common/build) has MSBuild .props for the common package.";
					break;
				}
				case "web":
				{
					var localModules = FindWebSdkLocal(startDir);
					if (localModules != null)
					{
						sourcePath = localModules;
						commonPaths = new[] { localModules };
						hint = "Web SDK source is local in node_modules. Read files directly.";
					}
					else
					{
						sourcePath = $"https://github.com/beamable/BeamableProduct/tree/web-sdk-{detectedVersion}";
						commonPaths = new[] { "web/" };
						hint = "Web SDK local node_modules not found. Falling back to GitHub URL.";
					}
					break;
				}
				case "unreal":
				{
					var pluginsDir = FindPluginsDir(startDir);
					sourcePath = pluginsDir ?? startDir;
					commonPaths = Array.Empty<string>();
					hint = "Unreal SDK source is local. The Plugins/BeamableCore directory contains the SDK code.";
					break;
				}
				default:
				{
					var errorResult = new
					{
						error = $"Unknown platform '{normalizedPlatform}'",
						hint = "Valid platforms: 'unity', 'cli', 'web', 'unreal'"
					};
					return Task.FromResult(JsonConvert.SerializeObject(errorResult, Formatting.None));
				}
			}

			string fileContent = null;
			int totalLength = 0;
			bool hasMore = false;

			if (!string.IsNullOrEmpty(filePath))
			{
				var resolved = ResolveFilePath(filePath, sourcePath, commonPaths);

				if (resolved != null && File.Exists(resolved))
				{
					fileFull = Path.GetFullPath(resolved);

					if (IsPathUnderAllowedDirectory(fileFull, sourcePath, commonPaths))
					{
						try
						{
							var fullText = File.ReadAllText(fileFull);
							totalLength = fullText.Length;
							var effectiveLimit = limit <= 0 || limit > DefaultFileReadLimit ? DefaultFileReadLimit : limit;
							var clampedOffset = Math.Max(0, Math.Min(offset, fullText.Length));
							var chunkLength = Math.Min(effectiveLimit, fullText.Length - clampedOffset);
							fileContent = fullText.Substring(clampedOffset, chunkLength);
							hasMore = clampedOffset + chunkLength < totalLength;
						}
						catch (Exception ex)
						{
							hint += $" (file read error: {ex.Message})";
						}
					}
					else
					{
						fileFull = null;
						hint += " (requested file path is outside allowed SDK directories)";
					}
				}
				else
				{
					var normalizedPath = filePath.Trim().Replace("\\", "/").TrimStart('/');
					fileFull = !sourcePath.StartsWith("http")
						? Path.Combine(sourcePath, normalizedPath)
						: normalizedPath;
				}
			}

			var responseObj = new JObject
			{
				["platform"] = normalizedPlatform,
				["detectedVersion"] = detectedVersion,
				["sourcePath"] = sourcePath,
				["commonPaths"] = new JArray(commonPaths),
				["hint"] = hint
			};

			if (fileFull != null)
				responseObj["filePath"] = fileFull;

			if (fileContent != null)
			{
				responseObj["content"] = fileContent;
				responseObj["totalLength"] = totalLength;
				responseObj["offset"] = Math.Max(0, Math.Min(offset, totalLength));
				responseObj["hasMore"] = hasMore;
				if (hasMore)
				{
					var nextOffset = Math.Max(0, Math.Min(offset, totalLength)) + fileContent.Length;
					responseObj["nextOffset"] = nextOffset;
					responseObj["remaining"] = totalLength - nextOffset;
				}
			}

			return Task.FromResult(responseObj.ToString(Formatting.None));
		}
		catch (Exception ex)
		{
			var errorResult = new
			{
				error = $"Failed to resolve source path: {ex.Message}",
				hint = "Pass platform and version explicitly if auto-detection is not working"
			};
			return Task.FromResult(JsonConvert.SerializeObject(errorResult, Formatting.None));
		}
	}

	private static string FindUnityPackageCache(string startDir, string version)
	{
		var dir = startDir;
		while (dir != null)
		{
			var cacheDir = Path.Combine(dir, "Library", "PackageCache");
			if (Directory.Exists(cacheDir))
			{
				var match = Directory.GetDirectories(cacheDir, $"com.beamable@{version}*").FirstOrDefault()
				            ?? Directory.GetDirectories(cacheDir, "com.beamable@*").FirstOrDefault();
				if (match != null) return match;
			}
			dir = Path.GetDirectoryName(dir);
		}
		return null;
	}

	private static string FindWebSdkLocal(string startDir)
	{
		var dir = startDir;
		while (dir != null)
		{
			var sdkDir = Path.Combine(dir, "node_modules", "@beamable", "sdk");
			if (Directory.Exists(sdkDir)) return sdkDir;
			dir = Path.GetDirectoryName(dir);
		}
		return null;
	}

	private static (string platform, string version)? TryDetectUnity(string startDir)
	{
		var dir = startDir;
		while (dir != null)
		{
			var manifestPath = Path.Combine(dir, "Packages", "manifest.json");
			if (File.Exists(manifestPath))
			{
				try
				{
					var json = JObject.Parse(File.ReadAllText(manifestPath));
					var deps = json["dependencies"] as JObject;
					var versionStr = deps?["com.beamable"]?.ToString();
					if (!string.IsNullOrEmpty(versionStr) && !versionStr.StartsWith("file:"))
						return ("unity", versionStr);
				}
				catch
				{
					// ignore parse errors
				}
			}

			dir = Path.GetDirectoryName(dir);
		}

		return null;
	}

	private static (string platform, string version)? TryDetectWebSdk(string startDir)
	{
		var dir = startDir;
		while (dir != null)
		{
			var packageJsonPath = Path.Combine(dir, "package.json");
			if (File.Exists(packageJsonPath))
			{
				try
				{
					var json = JObject.Parse(File.ReadAllText(packageJsonPath));
					var deps = json["dependencies"] as JObject;
					var devDeps = json["devDependencies"] as JObject;

					var hasBeamableSdk = deps?["@beamable/sdk"] != null;
					var hasPortalToolkit = deps?["@beamable/portal-toolkit"] != null
					                      || devDeps?["@beamable/portal-toolkit"] != null;

					if (hasBeamableSdk || hasPortalToolkit)
					{
						// Try to read the installed portal-toolkit package.json for the peer dependency version
						var toolkitPkgPath = Path.Combine(dir, "node_modules", "@beamable", "portal-toolkit", "package.json");
						if (File.Exists(toolkitPkgPath))
						{
							var toolkitJson = JObject.Parse(File.ReadAllText(toolkitPkgPath));
							var peerVersion = (toolkitJson["peerDependencies"] as JObject)?["@beamable/sdk"]?.ToString();
							if (!string.IsNullOrEmpty(peerVersion))
								return ("web", peerVersion.TrimStart('^', '~'));
						}

						// Fall back to direct @beamable/sdk version from dependencies
						var sdkVersion = deps?["@beamable/sdk"]?.ToString();
						if (!string.IsNullOrEmpty(sdkVersion))
							return ("web", sdkVersion.TrimStart('^', '~'));
					}
				}
				catch
				{
					// ignore parse errors
				}
			}

			dir = Path.GetDirectoryName(dir);
		}

		return null;
	}

	private static (string platform, string version)? TryDetectCli(string startDir)
	{
		var dir = startDir;
		while (dir != null)
		{
			if (TryGetProjectBeamableCLIVersion(dir, out var cliVersion)
			    && !string.IsNullOrEmpty(cliVersion))
			{
				return ("cli", cliVersion);
			}

			var manifestPath = Path.Combine(dir, ".config", "dotnet-tools.json");
			if (File.Exists(manifestPath))
			{
				var content = File.ReadAllText(manifestPath);
				var match = System.Text.RegularExpressions.Regex.Match(content,
					@"beamable.*?""([0-9]+\.[0-9]+\.[0-9]+(?:\.[0-9]+)?[^""]*)"",",
					System.Text.RegularExpressions.RegexOptions.Singleline);
				if (match.Success)
					return ("cli", match.Groups[1].Value);
			}

			dir = Path.GetDirectoryName(dir);
		}

		return null;
	}

	private static (string platform, string version)? TryDetectUnreal(string startDir)
	{
		var dir = startDir;
		while (dir != null)
		{
			var pluginsDir = Path.Combine(dir, "Plugins");
			if (Directory.Exists(pluginsDir) && Directory.Exists(Path.Combine(pluginsDir, "BeamableCore")))
				return ("unreal", "local");

			dir = Path.GetDirectoryName(dir);
		}

		return null;
	}

	private static string FindPluginsDir(string startDir)
	{
		var dir = startDir;
		while (dir != null)
		{
			var pluginsDir = Path.Combine(dir, "Plugins");
			if (Directory.Exists(pluginsDir) && Directory.Exists(Path.Combine(pluginsDir, "BeamableCore")))
				return pluginsDir;

			dir = Path.GetDirectoryName(dir);
		}

		return null;
	}

	private static string NormalizeVersion(string version)
	{
		if (string.IsNullOrEmpty(version))
			return version;

		var parts = version.Split('.');
		if (parts.Length == 4 && parts[3] == "0")
			return string.Join(".", parts[0], parts[1], parts[2]);

		return version;
	}

	private static string ResolveFilePath(string filePath, string sourcePath, string[] commonPaths)
	{
		var normalized = filePath.Trim().Replace("\\", "/").TrimStart('/');

		if (Path.IsPathRooted(normalized))
		{
			var abs = Path.GetFullPath(normalized);
			return File.Exists(abs) ? abs : null;
		}

		if (!sourcePath.StartsWith("http") && Path.IsPathRooted(sourcePath))
		{
			var candidate = Path.GetFullPath(Path.Combine(sourcePath, normalized));
			if (File.Exists(candidate))
				return candidate;
		}

		foreach (var cp in commonPaths)
		{
			if (cp.StartsWith("http") || !Path.IsPathRooted(cp))
				continue;
			var candidate = Path.GetFullPath(Path.Combine(cp, normalized));
			if (File.Exists(candidate))
				return candidate;
		}

		if (!normalized.Contains('/') && !normalized.Contains('\\'))
		{
			foreach (var cp in commonPaths)
			{
				if (cp.StartsWith("http") || !Path.IsPathRooted(cp) || !Directory.Exists(cp))
					continue;
				try
				{
					var matches = Directory.GetFiles(cp, normalized, SearchOption.AllDirectories);
					if (matches.Length > 0)
						return Path.GetFullPath(matches[0]);
				}
				catch (UnauthorizedAccessException) { }
				catch (IOException) { }
			}
		}

		return null;
	}

	private static bool IsPathUnderAllowedDirectory(string resolvedPath, string sourcePath, string[] commonPaths)
	{
		var canonical = Path.GetFullPath(resolvedPath);
		var allowed = new List<string>();

		if (!sourcePath.StartsWith("http") && Path.IsPathRooted(sourcePath))
			allowed.Add(Path.GetFullPath(sourcePath));

		foreach (var cp in commonPaths)
		{
			if (!cp.StartsWith("http") && Path.IsPathRooted(cp))
				allowed.Add(Path.GetFullPath(cp));
		}

		return allowed.Any(dir =>
			canonical.StartsWith(dir, StringComparison.OrdinalIgnoreCase));
	}

	public Task<string> ExecuteHelpAsync(string commandPath)
	{
		var helpCommand = string.IsNullOrWhiteSpace(commandPath)
			? "--help"
			: $"{commandPath.Trim()} --help";
		return ExecuteAsync(helpCommand);
	}

	/// <summary>
	/// Runs a beam command and returns what it reported. The command is cancelled with
	/// <paramref name="cancellationToken"/>, and its progress channels are forwarded to <paramref name="progress"/>.
	/// </summary>
	public async Task<CallToolResult> ExecuteCommandAsync(string commandLine,
		IProgress<ProgressNotificationValue> progress = null,
		CancellationToken cancellationToken = default)
	{
		var fullCommand = EnsureCommandFlags(commandLine);
		var capturer = new CapturingReporterService(progress);

		// Some commands (`local logs`, for one) write their output straight to the console.
		using var console = InvocationConsole.Begin(out var output);
		// Run on a plain thread-pool thread to avoid deadlocking a SynchronizationContext the MCP
		// host may have captured; CliWrap's continuations would otherwise try to resume on it.
		var exitCode = await Task.Run(() => CliInvocationCore.Execute(new CliInvocationRequest
		{
			ReadCommand = () => Task.FromResult((fullCommand,
				JsonConvert.SerializeObject(new ServerRequest { commandLine = fullCommand }))),
			UseCustomSplitter = false,
			App = AppFactory(),
			Refuse = RefuseNeverEndingCommand,
			CreateReporter = _ => capturer,
			ReportSetupFailure = report =>
			{
				capturer.Report(report.type, report.data);
				return Task.CompletedTask;
			},
			CancellationToken = cancellationToken
		}), CancellationToken.None).ConfigureAwait(false);

		return capturer.ToResult(commandLine, exitCode, cancellationToken.IsCancellationRequested, output);
	}

	/// <summary>Runs a command whose output is console text, such as help, and returns that text.</summary>
	public async Task<string> ExecuteAsync(string commandLine)
	{
		var fullCommand = EnsureLogStreams(commandLine);
		var capturer = new CapturingReporterService(null);

		using (InvocationConsole.Begin(out var output))
		{
			// Run on a plain thread-pool thread to avoid deadlocking a SynchronizationContext the MCP
			// host may have captured; CliWrap's continuations would otherwise try to resume on it.
			await Task.Run(() => CliInvocationCore.Execute(new CliInvocationRequest
			{
				ReadCommand = () => Task.FromResult((fullCommand,
					JsonConvert.SerializeObject(new ServerRequest { commandLine = fullCommand }))),
				UseCustomSplitter = false,
				App = AppFactory(),
				CreateReporter = _ => capturer,
				ReportSetupFailure = report =>
				{
					capturer.Report(report.type, report.data);
					return Task.CompletedTask;
				}
			})).ConfigureAwait(false);

			var sw = new StringWriter();
			await sw.WriteAsync(output.StandardOutput);
			await sw.WriteAsync(capturer.EventText());

			// CliException and other framework errors write to Console.Error, not stdout.
			// Append them as a JSON error line so the MCP client always sees them.
			var errorText = output.StandardError.Trim();
			if (!string.IsNullOrEmpty(errorText))
			{
				var enriched = EnrichErrorIfCommandFailure(errorText, commandLine);
				await sw.WriteLineAsync(JsonConvert.SerializeObject(new { error = enriched }));
			}

			// Commands that implement IEmptyResult produce no output on success.
			// Emit a generic success envelope so the MCP client always receives a
			// non-empty response and knows the command completed without error.
			if (string.IsNullOrWhiteSpace(sw.ToString()))
				await sw.WriteLineAsync(JsonConvert.SerializeObject(new { status = "ok", command = commandLine }));

			return TruncateIfNeeded(sw.ToString());
		}
	}

	private const int MaxOutputLength = 32_768;
	private const int TruncateHeadKeep = 4_096;
	private const int TruncateTailKeep = 24_576;

	private static string TruncateIfNeeded(string output)
	{
		if (output.Length <= MaxOutputLength)
			return output;

		var head = output.Substring(0, TruncateHeadKeep);
		var tail = output.Substring(output.Length - TruncateTailKeep);
		var dropped = output.Length - TruncateHeadKeep - TruncateTailKeep;
		return head + $"\n\n... [{dropped} characters truncated — showing first {TruncateHeadKeep} and last {TruncateTailKeep} characters] ...\n\n" + tail;
	}

	private static readonly string[] CommandErrorPatterns = new[]
	{
		"is not a recognized command",
		"unrecognized command or argument",
		"unrecognized option",
		"required argument missing",
	};

	private static string EnrichErrorIfCommandFailure(string errorText, string commandLine)
	{
		var lower = errorText.ToLowerInvariant();
		var isCommandError = CommandErrorPatterns.Any(p => lower.Contains(p.ToLowerInvariant()));
		if (!isCommandError) return errorText;

		return errorText + $"\n\nIMPORTANT: The command ({commandLine}) or its arguments may have changed. Before retrying:\n" +
		       "1. Call beam_list_commands() to get the current list of all available commands\n" +
		       "2. Call beam_get_help(\"<command>\") for the specific command to get its current options and arguments\n" +
		       "3. Retry with the corrected command\n\n" +
		       "Do NOT guess or retry with the same arguments.";
	}

	private static string EnsureLogStreams(string commandLine)
	{
		var lower = commandLine.ToLowerInvariant();

		// Route log messages through IDataReporterService so they are captured
		// in the MCP response alongside structured output.
		if (!lower.Contains("--emit-log-streams"))
			return commandLine + " --emit-log-streams";

		return commandLine;
	}

	/// <summary>
	/// A tool call waits for the command to finish, so servers, log tails and --watch modes would hang it.
	/// </summary>
	private static string RefuseNeverEndingCommand(App app, string commandLine)
	{
		if (app.CommandProvider?.CanBuildService<RootCommand>() != true)
		{
			return null;
		}

		var root = app.CommandProvider.GetService<RootCommand>();

		var parsed = new Parser(root).Parse(commandLine);
		if (parsed.CommandResult.Command is IStreamsIndefinitely)
		{
			return $"'{parsed.CommandResult.Command.Name}' runs until it is stopped, so it cannot be run as a tool call. " +
			       "Run it in a terminal instead.";
		}

		var watches = parsed.CommandResult.Children.OfType<OptionResult>()
			.Any(o => o.Option.Name == "watch" && o.GetValueOrDefault() is true);
		if (watches)
		{
			return "--watch keeps the command running until it is stopped, so it cannot be run as a tool call. " +
			       "Run it without --watch to get the current state.";
		}

		return null;
	}

	/// <summary>
	/// The flags a command needs to report everything through IDataReporterService: logs as a "logs" channel and,
	/// with --raw, failures on the "error" channel even when stdout is not redirected (as in `server serve`).
	/// </summary>
	private static string EnsureCommandFlags(string commandLine)
	{
		var withLogs = EnsureLogStreams(commandLine);
		return withLogs.ToLowerInvariant().Contains("--raw") ? withLogs : withLogs + " --raw";
	}

	private sealed class CapturingReporterService : IDataReporterService
	{
		private readonly object _lock = new();
		private readonly List<JObject> _events = new();
		private readonly List<CliLogMessage> _logs = new();
		private readonly IProgress<ProgressNotificationValue> _progress;
		private float _lastProgress;
		private bool _reportedError;

		private static readonly HashSet<string> ProgressChannels = new(StringComparer.OrdinalIgnoreCase)
		{
			"progress", "progressStream", "remote_progress"
		};

		// The fields progress payloads use for a 0..1 completion ratio.
		private static readonly string[] RatioFields = { "ratio", "progressRatio", "progress" };

		private static readonly HashSet<string> ReportedLogLevels = new(StringComparer.OrdinalIgnoreCase)
		{
			"Warning", "Error", "Critical", "Fatal"
		};

		public CapturingReporterService(IProgress<ProgressNotificationValue> progress)
		{
			_progress = progress;
		}

		public void Report<T>(string type, T data)
		{
			var pt = new ReportDataPoint<T>
			{
				data = data,
				type = type,
				ts = DateTimeOffset.Now.ToUnixTimeMilliseconds()
			};
			var json = JObject.Parse(JsonConvert.SerializeObject(pt, UnitySerializationSettings.Instance));

			lock (_lock)
			{
				if (data is CliLogMessage log)
				{
					// Warnings and errors explain a result; the rest would only crowd the agent's context.
					if (ReportedLogLevels.Contains(log.logLevel ?? ""))
					{
						_logs.Add(log);
					}
					return;
				}

				if (ProgressChannels.Contains(type))
				{
					ReportProgress(json["data"]);
					return;
				}

				if (type.StartsWith(DefaultErrorStream.CHANNEL, StringComparison.Ordinal))
				{
					_reportedError = true;
				}
				_events.Add(json);
			}
		}

		private void ReportProgress(JToken data)
		{
			if (_progress == null)
			{
				return;
			}

			var ratio = RatioFields
				.Select(f => data?[f])
				.FirstOrDefault(t => t != null && t.Type is JTokenType.Float or JTokenType.Integer);
			// MCP requires progress to increase, so payloads without a ratio count up instead.
			var next = ratio != null
				? Math.Max(_lastProgress, Math.Clamp(ratio.Value<float>(), 0f, 1f))
				: _lastProgress + 1;
			_lastProgress = next;
			_progress.Report(new ProgressNotificationValue
			{
				Progress = next,
				Total = ratio != null ? 1f : null,
				Message = data?.ToString(Formatting.None)
			});
		}

		/// <summary>Each reported event's JSON on its own line.</summary>
		public string EventText()
		{
			lock (_lock)
			{
				return string.Concat(_events.Select(e => e.ToString(Formatting.None) + Environment.NewLine));
			}
		}

		public CallToolResult ToResult(string commandLine, int exitCode, bool cancelled, InvocationConsole.Capture console = null)
		{
			List<JObject> events;
			List<CliLogMessage> logs;
			lock (_lock)
			{
				events = _events.ToList();
				logs = _logs.ToList();
			}

			var text = new StringBuilder();
			foreach (var evt in events)
			{
				text.AppendLine(evt.ToString(Formatting.None));
			}
			foreach (var log in logs)
			{
				text.AppendLine(JsonConvert.SerializeObject(new { log = log.message, level = log.logLevel }));
			}

			// What the command printed itself rather than reported.
			var printed = console?.StandardOutput ?? "";
			text.Append(printed);

			var isError = exitCode != 0 || _reportedError || cancelled;
			if (isError && !_reportedError && !cancelled)
			{
				// A failure without an error event only explains itself on stderr.
				var stderr = console?.StandardError.Trim();
				if (!string.IsNullOrEmpty(stderr))
				{
					text.AppendLine(JsonConvert.SerializeObject(new { error = EnrichErrorIfCommandFailure(stderr, commandLine) }));
				}
			}
			if (cancelled)
			{
				text.AppendLine(JsonConvert.SerializeObject(new { error = "The command was cancelled.", command = commandLine }));
			}
			else if (isError)
			{
				var errors = string.Join("\n", events
					.Where(e => ((string)e["type"] ?? "").StartsWith(DefaultErrorStream.CHANNEL, StringComparison.Ordinal))
					.Select(e => (string)e["data"]?["message"]));
				var enriched = EnrichErrorIfCommandFailure(errors, commandLine);
				if (enriched != errors)
				{
					text.AppendLine(JsonConvert.SerializeObject(new { hint = enriched[errors.Length..].Trim() }));
				}
			}

			// Commands that implement IEmptyResult produce no output on success.
			// Emit a generic success envelope so the MCP client always receives a
			// non-empty response and knows the command completed without error.
			if (text.Length == 0)
			{
				text.AppendLine(JsonConvert.SerializeObject(new { status = "ok", command = commandLine }));
			}

			var structured = new JObject
			{
				["command"] = commandLine,
				["exitCode"] = exitCode,
				["events"] = new JArray(events)
			};
			if (printed.Length > 0)
			{
				structured["console"] = TruncateIfNeeded(printed);
			}

			return new CallToolResult
			{
				Content = new List<ContentBlock> { new TextContentBlock { Text = TruncateIfNeeded(text.ToString()) } },
				StructuredContent = System.Text.Json.JsonSerializer.SerializeToElement(
					System.Text.Json.Nodes.JsonNode.Parse(structured.ToString(Formatting.None))),
				IsError = isError
			};
		}
	}
}
