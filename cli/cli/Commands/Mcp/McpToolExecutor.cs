using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Docs;
using cli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace cli.Mcp;

public class McpToolExecutor
{
	// Serialize all in-process beam calls so Console.Out redirection and MSBuildLocator don't race.
	private static readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

	public McpToolExecutor() { }

	public Task<string> ListTypeSectionsAsync()
	{
		var assembly = typeof(McpToolExecutor).Assembly;
		var indexStream = assembly.GetManifestResourceStream("cli.Resources.beamable-types-index.json");

		if (indexStream != null)
		{
			using var reader = new StreamReader(indexStream);
			var indexJson = reader.ReadToEnd();
			var sections = JsonConvert.DeserializeObject<TypeSectionIndex[]>(indexJson);
			if (sections is { Length: > 0 })
			{
				// Append the special "web" section
				var allSections = sections.Append(new TypeSectionIndex
				{
					Name = "web",
					Description = "Beamable Web SDK documentation URL and version resolution",
					TypeCount = 0
				}).ToArray();
				return Task.FromResult(JsonConvert.SerializeObject(allSections, Formatting.None));
			}
		}

		// Fallback: generate live
		var schema = GenerateBeamableTypesSchemaCommand.GenerateLive();
		var sharedCount = schema.UtilityTypes?.Count(t => t.Platform != "MicroserviceOnly") ?? 0;
		var serverCount = schema.UtilityTypes?.Count(t => t.Platform == "MicroserviceOnly") ?? 0;
		var fallback = new[]
		{
			new TypeSectionIndex { Name = "content", Description = "C# content object types with [ContentType] attribute", TypeCount = schema.ContentTypes?.Length ?? 0 },
			new TypeSectionIndex { Name = "federation", Description = "Federation interfaces", TypeCount = schema.FederationTypes?.Length ?? 0 },
			new TypeSectionIndex { Name = "utility-shared", Description = "Shared C# utility types from Beamable.Common", TypeCount = sharedCount },
			new TypeSectionIndex { Name = "utility-server", Description = "Microservice-only C# types from Beamable.Server", TypeCount = serverCount },
			new TypeSectionIndex { Name = "unreal", Description = "Unreal C++ to C# type mapping table", TypeCount = schema.UnrealTypeMappings?.Length ?? 0 },
			new TypeSectionIndex { Name = "web", Description = "Beamable Web SDK documentation URL and version resolution", TypeCount = 0 },
		};
		return Task.FromResult(JsonConvert.SerializeObject(fallback, Formatting.None));
	}

	public async Task<string> GetTypeSchemaAsync(string section = "", string filter = "")
	{
		var norm = section?.Trim().ToLowerInvariant() ?? "";

		if (string.IsNullOrEmpty(norm))
			return await ListTypeSectionsAsync();

		if (norm == "web")
			return BuildWebSdkResponse();

		// Try loading section-specific embedded resource
		var resourceName = $"cli.Resources.beamable-types-{norm}.json";
		var stream = typeof(McpToolExecutor).Assembly.GetManifestResourceStream(resourceName);
		if (stream != null)
		{
			using var reader = new StreamReader(stream);
			var json = reader.ReadToEnd();

			var f = filter?.Trim() ?? "";
			if (string.IsNullOrEmpty(f))
				return json;

			// Apply filter based on section type
			if (norm.StartsWith("utility"))
			{
				var types = JsonConvert.DeserializeObject<UtilityTypeEntry[]>(json);
				var filtered = McpListTypesCommand.FilterUtility(types ?? Array.Empty<UtilityTypeEntry>(), f);
				return JsonConvert.SerializeObject(filtered, Formatting.None);
			}
			if (norm == "content")
			{
				var types = JsonConvert.DeserializeObject<ContentTypeEntry[]>(json);
				var filtered = Array.FindAll(types ?? Array.Empty<ContentTypeEntry>(), t =>
					(t.TypeName?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
					(t.ClassName?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
					(t.Namespace?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
				return JsonConvert.SerializeObject(filtered, Formatting.None);
			}
			if (norm == "federation")
			{
				var types = JsonConvert.DeserializeObject<FederationTypeEntry[]>(json);
				var filtered = Array.FindAll(types ?? Array.Empty<FederationTypeEntry>(), t =>
					(t.InterfaceName?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0 ||
					(t.Namespace?.IndexOf(f, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
				return JsonConvert.SerializeObject(filtered, Formatting.None);
			}

			return json;
		}

		// Fallback: monolithic file or live generation
		var schema = McpListTypesCommand.ReadEmbeddedSchema();
		if (schema?.ContentTypes is { Length: > 0 })
			return BuildTypeResponse(schema, norm, filter?.Trim() ?? "");

		var cmd = "mcp list-types";
		if (!string.IsNullOrEmpty(norm)) cmd += $" --section {norm}";
		if (!string.IsNullOrEmpty(filter)) cmd += $" --filter \"{filter}\"";
		return await ExecuteAsync(cmd);
	}

	private static string BuildWebSdkResponse()
	{
		var webInfo = new
		{
			hint = "The Beamable Web SDK (@beamable/sdk) is a transitive dependency of @beamable/portal-toolkit. " +
			       "The toolkit version in devDependencies is NOT the SDK version. " +
			       "To find the actual SDK version: read node_modules/@beamable/portal-toolkit/package.json and check peerDependencies[\"@beamable/sdk\"]. " +
			       "Use that major.minor version in the docs URL below.",
			docsUrlPattern = "https://help.beamable.com/WebSDK-{VERSION}/web/user-reference/api-reference/modules/",
			example = "https://help.beamable.com/WebSDK-1.0/web/user-reference/api-reference/modules/",
			sdkPackageName = "@beamable/sdk",
			toolkitPackageName = "@beamable/portal-toolkit",
			versionResolution = new[]
			{
				"1. Read the extension's package.json to confirm @beamable/portal-toolkit is in devDependencies",
				"2. Read node_modules/@beamable/portal-toolkit/package.json",
				"3. Get the @beamable/sdk version from peerDependencies (e.g. '1.0.0')",
				"4. Use the major.minor (e.g. '1.0') in the docs URL"
			},
			usage = "Use beam_exec('services list') to find portal extensions, then follow the versionResolution steps to build the docs URL."
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
			"content"        => JsonConvert.SerializeObject(filtered.ContentTypes, Formatting.None),
			"federation"     => JsonConvert.SerializeObject(filtered.FederationTypes, Formatting.None),
			"utility" or "utility-shared" or "utility-server"
			                 => JsonConvert.SerializeObject(filtered.UtilityTypes, Formatting.None),
			"unreal"         => JsonConvert.SerializeObject(filtered.UnrealTypeMappings, Formatting.None),
			_                => JsonConvert.SerializeObject(new { error = $"Unknown section '{section}'. Valid: content, federation, utility-shared, utility-server, unreal, web." }, Formatting.None)
		};
	}

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

			// Auto-detect platform and version if not provided
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

			// Construct tag name and URLs
			string tagName;
			string sourceUrl;
			string[] commonPaths;
			string hint;

			switch (normalizedPlatform)
			{
				case "unity":
					tagName = $"unity-sdk-{detectedVersion}";
					sourceUrl = $"https://github.com/beamable/BeamableProduct/tree/{tagName}";
					commonPaths = new[] { "client/Packages/com.beamable/", "client/Packages/com.beamable.server/" };
					hint = "Unity SDK source. The 'client/Packages/com.beamable/' directory contains the main SDK code.";
					break;
				case "cli":
					tagName = $"cli-{detectedVersion}";
					sourceUrl = $"https://github.com/beamable/BeamableProduct/tree/{tagName}";
					commonPaths = new[] { "cli/cli/", "cli/beamable.common/", "cli/beamable.server.common/", "microservice/" };
					hint = "CLI source. The 'cli/cli/' directory contains the main CLI code.";
					break;
				case "web":
					tagName = $"web-sdk-{detectedVersion}";
					sourceUrl = $"https://github.com/beamable/BeamableProduct/tree/{tagName}";
					commonPaths = new[] { "web/" };
					hint = "Web SDK source. The 'web/' directory contains the main Web SDK code.";
					break;
				case "unreal":
					tagName = "unreal-local";
					var pluginsDir = FindPluginsDir(startDir);
					sourceUrl = pluginsDir ?? startDir;
					commonPaths = Array.Empty<string>();
					hint = "Unreal SDK source is local. The Plugins/BeamableCore directory contains the SDK code.";
					break;
				default:
					var errorResult = new
					{
						error = $"Unknown platform '{normalizedPlatform}'",
						hint = "Valid platforms: 'unity', 'cli', 'web', 'unreal'"
					};
					return Task.FromResult(JsonConvert.SerializeObject(errorResult, Formatting.None));
			}

			string fileUrl = null;
			if (!string.IsNullOrEmpty(filePath))
			{
				var normalizedPath = filePath.Trim().Replace("\\", "/").TrimStart('/');
				fileUrl = normalizedPlatform == "unreal"
					? Path.Combine(sourceUrl, normalizedPath)
					: $"{sourceUrl}/{normalizedPath}";
			}

			var fallbackUrl = "https://github.com/beamable/BeamableProduct/tree/main";

			var response = new
			{
				platform = normalizedPlatform,
				detectedVersion,
				tagName,
				sourceUrl,
				fileUrl,
				fallbackUrl,
				commonPaths,
				hint
			};

			return Task.FromResult(JsonConvert.SerializeObject(response, Formatting.None));
		}
		catch (Exception ex)
		{
			var errorResult = new
			{
				error = $"Failed to resolve source URL: {ex.Message}",
				hint = "Pass platform and version explicitly if auto-detection is not working"
			};
			return Task.FromResult(JsonConvert.SerializeObject(errorResult, Formatting.None));
		}
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
