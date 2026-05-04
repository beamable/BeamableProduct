using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace cli.Commands.Mcp;

public class McpToolExecutor
{
	// Serialize all in-process beam calls so Console.Out redirection and MSBuildLocator don't race.
	private static readonly SemaphoreSlim Lock = new(1, 1);

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

	public Task<string> GetSourceUrlAsync(string platform = "", string version = "", string filePath = "")
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
						hint = "Pass platform ('unity', 'cli', 'web', 'unreal') and version explicitly, " +
						       "or run this tool from within a project directory that has Beamable installed",
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
					var commonSrc = Path.Combine(nugetBase, "beamable.common", detectedVersion, "content", "netstandard2.0", "Runtime");
					var toolingSrc = Path.Combine(nugetBase, "beamable.tooling.common", detectedVersion, "content", "netstandard2.1");
					var runtimeSrc = Path.Combine(nugetBase, "beamable.microservice.runtime", detectedVersion, "content", "net8.0");

					sourcePath = nugetBase;
					commonPaths = new[] { commonSrc, toolingSrc, runtimeSrc };
					hint = "CLI/Microservice source is in the NuGet cache. " +
					       "beamable.common has content types, APIs, and Optional<T>. " +
					       "beamable.tooling.common has callable attributes, federation interfaces, and storage. " +
					       "beamable.microservice.runtime has the Microservice base class and API implementations. " +
					       "Read .cs files directly from these paths.";
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

			if (!string.IsNullOrEmpty(filePath))
			{
				var normalizedPath = filePath.Trim().Replace("\\", "/").TrimStart('/');
				fileFull = Path.Combine(sourcePath, normalizedPath);
			}

			var response = new
			{
				platform = normalizedPlatform,
				detectedVersion,
				sourcePath,
				filePath = fileFull,
				commonPaths,
				hint
			};

			return Task.FromResult(JsonConvert.SerializeObject(response, Formatting.None));
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
			if (ConfigService.TryGetProjectBeamableCLIVersion(dir, out var cliVersion)
			    && !string.IsNullOrEmpty(cliVersion))
			{
				return ("cli", cliVersion);
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

	public Task<string> ExecuteHelpAsync(string commandPath)
	{
		var helpCommand = string.IsNullOrWhiteSpace(commandPath)
			? "--help"
			: $"{commandPath.Trim()} --help";
		return ExecuteAsync(helpCommand);
	}

	public async Task<string> ExecuteAsync(string commandLine)
	{
		await Lock.WaitAsync();
		try
		{
			return await RunInProcessAsync(commandLine);
		}
		finally
		{
			Lock.Release();
		}
	}

	private async Task<string> RunInProcessAsync(string commandLine)
	{
		var fullCommand = EnsureLogStreams(commandLine);

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
			await sw.WriteLineAsync($"{{\"error\":\"{ex.Message.Replace("\"", "\\\"")}\"}}");
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
		{
			var enriched = EnrichErrorIfCommandFailure(errorText, commandLine);
			await sw.WriteLineAsync(JsonConvert.SerializeObject(new { error = enriched }));
		}

		// Commands that implement IEmptyResult produce no output on success.
		// Emit a generic success envelope so the MCP client always receives a
		// non-empty response and knows the command completed without error.
		if (string.IsNullOrWhiteSpace(sw.ToString()))
			await sw.WriteLineAsync(JsonConvert.SerializeObject(new { status = "ok", command = commandLine }));

		return sw.ToString();
	}

	private static readonly string[] CommandErrorPatterns = new[]
	{
		"is not a recognized command",
		"unrecognized command or argument",
		"unrecognized option",
		"required argument missing",
		"Required argument missing",
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

	private sealed class CapturingReporterService : IDataReporterService
	{
		private readonly TextWriter _writer;
		private readonly Dictionary<string, long> _lastProgressWrite = new();
		private const long THROTTLE_INTERVAL_MS = 2000;

		private static readonly HashSet<string> ProgressChannels = new(StringComparer.OrdinalIgnoreCase)
		{
			"progress", "progressStream", "remote_progress"
		};

		public CapturingReporterService(TextWriter writer)
		{
			_writer = writer;
		}

		public void Report<T>(string type, T data)
		{
			if (ProgressChannels.Contains(type))
			{
				var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
				if (_lastProgressWrite.TryGetValue(type, out var last) && now - last < THROTTLE_INTERVAL_MS)
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
