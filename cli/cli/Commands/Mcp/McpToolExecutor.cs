using Beamable.Common.BeamCli;
using Beamable.Server.Common;
using cli.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using static cli.ConfigService;

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

	private const int DefaultFileReadLimit = 65_536;

	/// <param name="startDirectory">Directory detection starts from. Defaults to the current directory.</param>
	public Task<string> GetSourceCode(string platform = "", string version = "", string filePath = "", int offset = 0, int limit = 0, string startDirectory = null)
	{
		try
		{
			var startDir = string.IsNullOrEmpty(startDirectory) ? Directory.GetCurrentDirectory() : startDirectory;
			var normalizedPlatform = platform?.Trim().ToLowerInvariant() ?? "";
			var detectedVersion = version?.Trim() ?? "";
			WebSdkDetection webDetection = null;

			if (string.IsNullOrEmpty(normalizedPlatform))
			{
				var detected = TryDetectUnity(startDir)
				               ?? TryDetectWebSdk(startDir, scanSubdirectories: false)
				               ?? TryDetectCli(startDir)
				               ?? TryDetectUnreal(startDir)
				               ?? TryDetectWebSdk(startDir, scanSubdirectories: true);

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

				normalizedPlatform = detected.Value.platform;
				if (string.IsNullOrEmpty(detectedVersion))
				{
					detectedVersion = detected.Value.version;
				}
			}
			else if (string.IsNullOrEmpty(detectedVersion) && IsKnownPlatform(normalizedPlatform))
			{
				// Only accept a version detected for the requested platform. Taking whatever another
				// detector found (e.g. the CLI version of a workspace that also holds a web project)
				// reports the wrong version and links a tag that does not exist.
				if (normalizedPlatform == "web")
				{
					webDetection = DetectWebSdk(startDir);
					detectedVersion = webDetection?.Version ?? "";
				}
				else
				{
					detectedVersion = TryDetectPlatform(normalizedPlatform, startDir)?.version ?? "";
				}

				if (string.IsNullOrEmpty(detectedVersion))
				{
					return Task.FromResult(BuildVersionNotFoundResponse(normalizedPlatform, startDir, webDetection));
				}
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
					webDetection ??= DetectWebSdk(startDir);
					var localModules = webDetection?.LocalSdkDir;
					if (localModules != null)
					{
						sourcePath = localModules;
						commonPaths = new[] { localModules };
						hint = "Web SDK source is local in node_modules. Read files directly.";
					}
					else
					{
						// Web SDK releases are tagged 'web-sdk-{npm version}' (see .github/workflows/release-web.yml).
						sourcePath = $"https://github.com/beamable/BeamableProduct/tree/web-sdk-{detectedVersion}";
						commonPaths = new[] { "web/" };
						hint = "Web SDK local node_modules not found. Falling back to GitHub URL.";
					}

					if (webDetection != null && webDetection.IsRange && webDetection.Version == detectedVersion)
					{
						hint += $" The version was taken from the semver range '{webDetection.Range}' in {webDetection.SourceFile};" +
						        " the installed version may be newer. Install dependencies or pass version explicitly for an exact match.";
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

			if (normalizedPlatform == "web" && webDetection != null && webDetection.Version == detectedVersion)
			{
				if (webDetection.IsRange)
				{
					responseObj["versionRange"] = webDetection.Range;
				}
				if (webDetection.SourceFile != null)
				{
					responseObj["versionSource"] = webDetection.SourceFile;
				}
			}

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

	private static bool IsKnownPlatform(string platform)
	{
		return platform is "unity" or "cli" or "web" or "unreal";
	}

	private static (string platform, string version)? TryDetectPlatform(string platform, string startDir)
	{
		switch (platform)
		{
			case "unity": return TryDetectUnity(startDir);
			case "web": return TryDetectWebSdk(startDir, scanSubdirectories: true);
			case "cli": return TryDetectCli(startDir);
			case "unreal": return TryDetectUnreal(startDir);
			default: return null;
		}
	}

	private static string BuildVersionNotFoundResponse(string platform, string startDir, WebSdkDetection webDetection)
	{
		var hint = platform == "web"
			? "No installed node_modules/@beamable/sdk and no '@beamable/sdk' entry in the dependencies or devDependencies" +
			  " of a package.json was found in this directory, its parents, or its subdirectories up to two levels deep." +
			  " Pass version explicitly (the web SDK version is its npm version, e.g. '1.2.1')," +
			  " or install the web project's dependencies and run this tool again."
			: $"No Beamable {platform} SDK version was found from this directory. Pass version explicitly," +
			  " or run this tool from within a project directory that has Beamable installed.";

		var result = new JObject
		{
			["error"] = $"Could not detect the Beamable {platform} SDK version",
			["platform"] = platform,
			["detectedVersion"] = JValue.CreateNull(),
			["hint"] = hint,
			["searchedFrom"] = startDir
		};

		if (webDetection != null && webDetection.IsRange)
		{
			result["versionRange"] = webDetection.Range;
			result["versionSource"] = webDetection.SourceFile;
			result["hint"] = $"Found '@beamable/sdk' range '{webDetection.Range}' in {webDetection.SourceFile}," +
			                 " which does not name a single version. Pass version explicitly," +
			                 " or install the web project's dependencies and run this tool again.";
		}

		return result.ToString(Formatting.None);
	}

	/// <summary>
	/// What <see cref="DetectWebSdk"/> found about the Beamable web SDK (<c>@beamable/sdk</c>).
	/// </summary>
	public class WebSdkDetection
	{
		/// <summary>The installed version, or one taken from a simple semver range. Null when no single version is known.</summary>
		public string Version;

		/// <summary>The raw dependency spec (e.g. <c>^1.2.1</c>) when the version did not come from an installed package.</summary>
		public string Range;

		/// <summary>The package.json the version or range was read from.</summary>
		public string SourceFile;

		/// <summary>The installed <c>node_modules/@beamable/sdk</c> directory, when one exists.</summary>
		public string LocalSdkDir;

		public bool IsRange => !string.IsNullOrEmpty(Range);
	}

	private const int WebSdkSubdirectoryScanDepth = 2;

	/// <summary>
	/// Looks for the Beamable web SDK from <paramref name="startDir"/>, its parents and (when
	/// <paramref name="scanSubdirectories"/> is set) its subdirectories up to two levels deep, so a
	/// workspace root finds e.g. <c>web/package.json</c>. An installed
	/// <c>node_modules/@beamable/sdk/package.json</c> version wins over a range declared in a
	/// dependent's <c>dependencies</c> or <c>devDependencies</c>. Returns null when nothing is found.
	/// </summary>
	public static WebSdkDetection DetectWebSdk(string startDir, bool scanSubdirectories = true)
	{
		var searchDirs = GetWebSdkSearchDirectories(startDir, scanSubdirectories);
		var localSdkDir = searchDirs
			.Select(d => Path.Combine(d, "node_modules", "@beamable", "sdk"))
			.FirstOrDefault(Directory.Exists);

		foreach (var dir in searchDirs)
		{
			var installedPkg = Path.Combine(dir, "node_modules", "@beamable", "sdk", "package.json");
			var installed = ReadJsonString(installedPkg, "version");
			if (!string.IsNullOrEmpty(installed))
			{
				return new WebSdkDetection
				{
					Version = installed,
					SourceFile = installedPkg,
					LocalSdkDir = Path.GetDirectoryName(installedPkg)
				};
			}
		}

		foreach (var dir in searchDirs)
		{
			var spec = ReadDeclaredWebSdkSpec(dir, out var sourceFile);
			if (string.IsNullOrEmpty(spec))
			{
				continue;
			}

			var resolved = VersionFromRange(spec);
			return new WebSdkDetection
			{
				Version = resolved,
				Range = resolved == spec ? null : spec,
				SourceFile = sourceFile,
				LocalSdkDir = localSdkDir
			};
		}

		return localSdkDir == null ? null : new WebSdkDetection { LocalSdkDir = localSdkDir };
	}

	/// <summary>
	/// Turns a dependency spec into a single version: exact versions pass through, and a lone
	/// <c>^</c>, <c>~</c>, <c>=</c>, <c>&gt;=</c> or <c>v</c> prefix is stripped. Returns null for
	/// anything that does not name one version (compound ranges, wildcards, tags, file/git specs).
	/// </summary>
	public static string VersionFromRange(string spec)
	{
		if (string.IsNullOrWhiteSpace(spec))
		{
			return null;
		}

		var candidate = spec.Trim();
		if (candidate.StartsWith(">="))
		{
			candidate = candidate.Substring(2);
		}
		candidate = candidate.TrimStart('^', '~', '=', 'v', ' ');

		return Regex.IsMatch(candidate, @"^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$")
			? candidate
			: null;
	}

	private static (string platform, string version)? TryDetectWebSdk(string startDir, bool scanSubdirectories)
	{
		var detection = DetectWebSdk(startDir, scanSubdirectories);
		if (string.IsNullOrEmpty(detection?.Version))
		{
			return null;
		}

		return ("web", detection.Version);
	}

	private static List<string> GetWebSdkSearchDirectories(string startDir, bool scanSubdirectories)
	{
		var dirs = new List<string> { startDir };

		if (scanSubdirectories)
		{
			var level = new List<string> { startDir };
			for (var depth = 0; depth < WebSdkSubdirectoryScanDepth; depth++)
			{
				var next = new List<string>();
				foreach (var parent in level)
				{
					next.AddRange(GetScannableSubdirectories(parent));
				}
				dirs.AddRange(next);
				level = next;
			}
		}

		for (var dir = Path.GetDirectoryName(startDir); dir != null; dir = Path.GetDirectoryName(dir))
		{
			dirs.Add(dir);
		}

		return dirs;
	}

	private static IEnumerable<string> GetScannableSubdirectories(string dir)
	{
		try
		{
			return Directory.GetDirectories(dir)
				.Where(d =>
				{
					var name = Path.GetFileName(d);
					return !name.StartsWith(".") && !name.Equals("node_modules", StringComparison.OrdinalIgnoreCase);
				})
				.OrderBy(d => d, StringComparer.Ordinal)
				.ToList();
		}
		catch (UnauthorizedAccessException)
		{
			return Array.Empty<string>();
		}
		catch (IOException)
		{
			return Array.Empty<string>();
		}
	}

	private static string ReadDeclaredWebSdkSpec(string dir, out string sourceFile)
	{
		sourceFile = null;
		var packageJsonPath = Path.Combine(dir, "package.json");
		var json = ReadJson(packageJsonPath);
		if (json == null)
		{
			return null;
		}

		var deps = json["dependencies"] as JObject;
		var devDeps = json["devDependencies"] as JObject;

		var sdkSpec = deps?["@beamable/sdk"]?.ToString() ?? devDeps?["@beamable/sdk"]?.ToString();
		if (!string.IsNullOrEmpty(sdkSpec))
		{
			sourceFile = packageJsonPath;
			return sdkSpec;
		}

		// Portal extensions depend on the toolkit, which declares the SDK as a peer dependency.
		var hasPortalToolkit = deps?["@beamable/portal-toolkit"] != null || devDeps?["@beamable/portal-toolkit"] != null;
		if (hasPortalToolkit)
		{
			var toolkitPkgPath = Path.Combine(dir, "node_modules", "@beamable", "portal-toolkit", "package.json");
			var peerSpec = (ReadJson(toolkitPkgPath)?["peerDependencies"] as JObject)?["@beamable/sdk"]?.ToString();
			if (!string.IsNullOrEmpty(peerSpec))
			{
				sourceFile = toolkitPkgPath;
				return peerSpec;
			}
		}

		return null;
	}

	private static JObject ReadJson(string path)
	{
		if (!File.Exists(path))
		{
			return null;
		}

		try
		{
			return JObject.Parse(File.ReadAllText(path));
		}
		catch
		{
			// ignore unreadable or malformed files
			return null;
		}
	}

	private static string ReadJsonString(string path, string key)
	{
		return ReadJson(path)?[key]?.ToString();
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

	public async Task<string> ExecuteAsync(string commandLine)
	{
		await Lock.WaitAsync();
		try
		{
			var run = await RunInProcessAsync(PrepareCommandLine(commandLine));

			// A wrong guess at a command's arguments should cost one call, not three: answer a parse
			// error with the command's help so the caller can retry straight away.
			if (run.isParseError && !IsHelpRequest(commandLine))
			{
				var helpPath = GetCommandPath(commandLine);
				var help = await RunInProcessAsync(PrepareCommandLine(string.IsNullOrEmpty(helpPath) ? "--help" : $"{helpPath} --help"));
				return TruncateIfNeeded(BuildInvalidArgumentsResponse(commandLine, run.errorText, helpPath, help.output));
			}

			return TruncateIfNeeded(run.output);
		}
		finally
		{
			Lock.Release();
		}
	}

	private async Task<(string output, string errorText, bool isParseError)> RunInProcessAsync(string fullCommand)
	{
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
			await sw.WriteLineAsync(JsonConvert.SerializeObject(new { error = errorText }));

		// With --emit-log-streams, parse errors arrive as structured "error" stream messages instead.
		var parseErrors = capturer.ErrorMessages.Append(errorText).Where(IsParseError).ToList();
		var isParseError = parseErrors.Count > 0;
		if (isParseError)
			errorText = string.Join("\n", parseErrors);

		// Commands that implement IEmptyResult produce no output on success.
		// Emit a generic success envelope so the MCP client always receives a
		// non-empty response and knows the command completed without error.
		if (string.IsNullOrWhiteSpace(sw.ToString()))
			await sw.WriteLineAsync(JsonConvert.SerializeObject(new { status = "ok", command = fullCommand }));

		return (sw.ToString(), errorText, isParseError);
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
		"required command was not provided",
		"cannot parse argument",
	};

	private static readonly Regex RequiredOptionPattern = new(@"option '[^']+' is required", RegexOptions.IgnoreCase);

	/// <summary>True when stderr holds a System.CommandLine parse error rather than a failure from the command itself.</summary>
	public static bool IsParseError(string errorText)
	{
		if (string.IsNullOrWhiteSpace(errorText)) return false;
		var lower = errorText.ToLowerInvariant();
		return CommandErrorPatterns.Any(p => lower.Contains(p)) || RequiredOptionPattern.IsMatch(errorText);
	}

	private static readonly Regex TokenPattern = new(@"""[^""]*""|'[^']*'|\S+");

	private static List<string> Tokenize(string commandLine) =>
		TokenPattern.Matches(commandLine ?? "").Select(m => m.Value).ToList();

	private static bool HasToken(List<string> tokens, params string[] names) =>
		tokens.Any(t => names.Contains(t, StringComparer.OrdinalIgnoreCase));

	private static bool IsHelpRequest(string commandLine) =>
		HasToken(Tokenize(commandLine), "--help", "-h", "-?", "/?", "/h");

	/// <summary>
	/// The leading command words of a command line (everything before the first option), e.g.
	/// "project new service Foo --name x" → "project new service Foo". Asking for help on that prints the
	/// help of the deepest command it names.
	/// </summary>
	public static string GetCommandPath(string commandLine)
	{
		var words = Tokenize(commandLine).TakeWhile(t => !t.StartsWith("-"));
		return string.Join(" ", words);
	}

	/// <summary>
	/// Adds the flags every in-process MCP call needs: <c>--emit-log-streams</c>, so logs are captured in the
	/// response, and, under the MCP server, <c>-q</c>, so a command never blocks on a prompt nobody can answer.
	/// Flags go before a <c>--</c> separator if there is one.
	/// </summary>
	public static string PrepareCommandLine(string commandLine, bool runningInMcpServer)
	{
		commandLine = (commandLine ?? "").Trim();
		var tokens = Tokenize(commandLine);
		var separator = tokens.IndexOf("--");
		var beforeSeparator = separator >= 0 ? tokens.Take(separator).ToList() : tokens;

		var extra = new List<string>();
		if (!HasToken(beforeSeparator, "--emit-log-streams"))
			extra.Add("--emit-log-streams");
		if (runningInMcpServer && !HasToken(beforeSeparator, "-q", "--quiet"))
			extra.Add("-q");
		if (extra.Count == 0)
			return commandLine;

		if (separator < 0)
			return string.Join(" ", tokens.Concat(extra));
		return string.Join(" ", tokens.Take(separator).Concat(extra).Concat(tokens.Skip(separator)));
	}

	private static string PrepareCommandLine(string commandLine) =>
		PrepareCommandLine(commandLine, App.IsRunningInMcpServer);

	private static string BuildInvalidArgumentsResponse(string commandLine, string errorText, string helpPath, string helpOutput)
	{
		var sb = new StringBuilder();
		sb.AppendLine($"Invalid arguments for `beam {commandLine.Trim()}`:");
		sb.AppendLine(errorText);
		sb.AppendLine();
		sb.AppendLine(string.IsNullOrEmpty(helpPath)
			? "Available commands (from `beam --help`):"
			: $"Help for `beam {helpPath}` — fix the arguments and call beam_exec again:");
		sb.AppendLine(helpOutput.Trim());
		return sb.ToString();
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

		private static readonly HashSet<string> SuppressedChannels = new(StringComparer.OrdinalIgnoreCase)
		{
			"logs"
		};

		/// <summary>The <c>message</c> of every "error" stream payload reported so far.</summary>
		public List<string> ErrorMessages { get; } = new();

		public CapturingReporterService(TextWriter writer)
		{
			_writer = writer;
		}

		public void Report<T>(string type, T data)
		{
			if (SuppressedChannels.Contains(type))
				return;

			if (string.Equals(type, "error", StringComparison.OrdinalIgnoreCase) && data != null)
			{
				try
				{
					var message = (string)JObject.FromObject(data)["message"];
					if (!string.IsNullOrEmpty(message)) ErrorMessages.Add(message);
				}
				catch (Exception)
				{
					// not an object with a message; nothing to record
				}
			}

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
