using Beamable.Server;
using Newtonsoft.Json;
using System.Diagnostics;
using System.Formats.Tar;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace cli.Services.LocalStack;

/// <summary>One installed tool: the version, the directory that holds its <c>bin/</c>, and where it came from.</summary>
public class ToolchainEntry
{
	public string version;

	/// <summary>The tool's root (a <c>JAVA_HOME</c>-shaped path: <c>&lt;home&gt;/bin/&lt;exe&gt;</c> runs it).</summary>
	public string home;

	/// <summary><c>"downloaded"</c> when this toolchain installed it, <c>"system"</c> when an existing install was adopted.</summary>
	public string source;
}

/// <summary>The on-disk record of what a toolchain directory contains (<c>&lt;dir&gt;/toolchain.json</c>).</summary>
public class ToolchainManifest
{
	public Dictionary<string, ToolchainEntry> tools = new Dictionary<string, ToolchainEntry>(StringComparer.OrdinalIgnoreCase);
}

/// <summary>Knobs for a single <c>beam local setup</c> run.</summary>
public class ToolchainOptions
{
	/// <summary>Re-download and re-extract even when the tool is already present.</summary>
	public bool force;

	/// <summary>Never hit the network: use the <c>downloads/</c> cache, and fail when a needed archive is not in it.</summary>
	public bool offline;

	/// <summary>Adopt an already-installed tool when its version satisfies the pin, instead of downloading a private copy.</summary>
	public bool preferSystem;

	/// <summary>Resolve and report what would happen, but download and write nothing.</summary>
	public bool dryRun;
}

/// <summary>The outcome of ensuring one tool, for reporting.</summary>
public class ToolchainResult
{
	public string toolId;
	public ToolchainEntry entry;

	/// <summary>What happened: <c>"installed"</c>, <c>"cached"</c> (already present), <c>"system"</c>, or <c>"would install"</c>.</summary>
	public string action;

	/// <summary>Set when the tool could not be provisioned; <see cref="entry"/> is null in that case.</summary>
	public string error;

	public bool ok => error == null;
}

/// <summary>
/// Provisions the pinned toolchain (<see cref="ToolchainPins"/>) into a private directory and records what it
/// installed, so <c>beam local up</c> can run the stack against exactly those versions rather than whatever is
/// on <c>PATH</c>.
///
/// Everything is keyed off the directory, so pointing several workspaces at one <c>--toolchain-dir</c> shares a
/// single install and re-running setup is a no-op. Downloads are checksum-verified and extracted to a temp
/// directory before an atomic rename into place, so an interrupted run never leaves a half-extracted tool
/// looking valid.
/// </summary>
public class ToolchainService
{
	/// <summary>Env var an operator can set instead of passing <c>--toolchain-dir</c> on every invocation.</summary>
	public const string EnvVarToolchainDir = "BEAM_TOOLCHAIN_DIR";

	public const string ManifestFileName = "toolchain.json";
	private const string DownloadsDirName = "downloads";

	private readonly string _dir;
	private readonly ToolchainOptions _options;
	private readonly HttpClient _http;

	public string Dir => _dir;
	public ToolchainManifest Manifest { get; private set; }

	public ToolchainService(string dir, ToolchainOptions options, HttpClient http = null)
	{
		_dir = dir;
		WarnIfDirectoryWouldCreateAWorkspaceMarker(dir);
		_options = options ?? new ToolchainOptions();
		// A JDK is ~110 MB and an SDK script pull can be slow on a cold CDN; the default 100s timeout aborts
		// mid-download and reads as a corrupt archive rather than "this took too long".
		_http = http ?? new HttpClient { Timeout = TimeSpan.FromMinutes(30) };
		Manifest = LoadManifest(ManifestPath);
	}

	public string ManifestPath => Path.Combine(_dir, ManifestFileName);
	private string DownloadsDir => Path.Combine(_dir, DownloadsDirName);

	/// <summary>
	/// The default toolchain directory name, under the user profile. Several checkouts then share one install —
	/// a JDK per repo would be gigabytes for no benefit.
	/// </summary>
	/// <remarks>
	/// Deliberately <c>~/.beamable-toolchain</c> and NOT <c>~/.beamable/toolchain</c>. A <c>.beamable</c>
	/// directory is the marker for a Beamable <em>workspace</em>: when a command runs outside one, the CLI falls
	/// back to the home directory, and a <c>~/.beamable</c> would make it treat the entire home directory as a
	/// workspace. That misfires the first-run telemetry consent prompt (App.cs), which throws
	/// "Failed to read input in non-interactive mode" — so simply installing the toolchain would break every
	/// beam command run outside a workspace, on every machine.
	/// </remarks>
	public const string DefaultDirName = ".beamable-toolchain";

	/// <summary>
	/// Resolves the toolchain directory: an explicit path wins, then <see cref="EnvVarToolchainDir"/>, then
	/// <c>~/</c><see cref="DefaultDirName"/>.
	/// </summary>
	public static string ResolveDir(string overrideDir)
	{
		if (!string.IsNullOrWhiteSpace(overrideDir))
			return Path.GetFullPath(overrideDir);

		var fromEnv = Environment.GetEnvironmentVariable(EnvVarToolchainDir);
		if (!string.IsNullOrWhiteSpace(fromEnv))
			return Path.GetFullPath(fromEnv);

		return Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), DefaultDirName);
	}

	/// <summary>
	/// Warns when the chosen toolchain directory sits inside a <c>.beamable</c> folder. Creating one plants the
	/// marker that makes the CLI treat its parent as a Beamable workspace — and if that parent is the home
	/// directory, every beam command run outside a workspace starts failing on the telemetry consent prompt.
	/// A caller can still do this deliberately with <c>--toolchain-dir</c>; it just should not be silent.
	/// </summary>
	public static void WarnIfDirectoryWouldCreateAWorkspaceMarker(string dir)
	{
		if (string.IsNullOrWhiteSpace(dir)) return;

		try
		{
			var segments = Path.GetFullPath(dir)
				.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

			if (segments.Any(s => s.Equals(".beamable", StringComparison.OrdinalIgnoreCase)))
			{
				Log.Warning(
					$"Toolchain directory '{dir}' is inside a '.beamable' folder. That folder marks a Beamable " +
					"workspace, so creating it here can make unrelated beam commands treat this location as one. " +
					$"Prefer a path outside '.beamable' (the default is ~/{DefaultDirName}).");
			}
		}
		catch
		{
			// an unparseable path is the caller's problem, not this warning's
		}
	}

	public static ToolchainManifest LoadManifest(string path)
	{
		try
		{
			if (File.Exists(path))
			{
				var loaded = JsonConvert.DeserializeObject<ToolchainManifest>(File.ReadAllText(path));
				if (loaded != null)
				{
					loaded.tools ??= new Dictionary<string, ToolchainEntry>(StringComparer.OrdinalIgnoreCase);
					return loaded;
				}
			}
		}
		catch (Exception e)
		{
			// A corrupt manifest must not brick setup — the tools on disk are re-probed anyway, and the file is
			// rewritten at the end of the run. The warning is best-effort for the same reason: this is the
			// RECOVERY path, so it must not itself throw when no logger has been configured.
			try
			{
				Log.Warning($"Ignoring unreadable {ManifestFileName} at {path}: {e.Message}");
			}
			catch
			{
				// no logger available; degrading quietly still beats failing here
			}
		}

		return new ToolchainManifest();
	}

	public void SaveManifest()
	{
		if (_options.dryRun) return;

		Directory.CreateDirectory(_dir);
		File.WriteAllText(ManifestPath, JsonConvert.SerializeObject(Manifest, Formatting.Indented));
	}

	// ----------------------------------------------------------------------------------
	// Per-tool layout
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// The sub-directory of a tool's home that holds its executables. Empty for the .NET SDK (whose <c>dotnet</c>
	/// sits at the install root) and for Node on Windows (<c>node.exe</c>/<c>npm.cmd</c> at the archive root).
	/// </summary>
	public static string BinSubdir(string toolId) => toolId switch
	{
		ToolchainPins.Dotnet => string.Empty,
		ToolchainPins.Node => OperatingSystem.IsWindows() ? string.Empty : "bin",
		_ => "bin"
	};

	/// <summary>The executable used to prove a tool is really installed (and to read its version back).</summary>
	public static string ProbeExecutable(string toolId) => toolId switch
	{
		ToolchainPins.Jdk => OperatingSystem.IsWindows() ? "java.exe" : "java",
		ToolchainPins.Maven => OperatingSystem.IsWindows() ? "mvn.cmd" : "mvn",
		ToolchainPins.Dotnet => OperatingSystem.IsWindows() ? "dotnet.exe" : "dotnet",
		ToolchainPins.Node => OperatingSystem.IsWindows() ? "node.exe" : "node",
		_ => null
	};

	/// <summary>Absolute path to a tool's probe executable, given its home.</summary>
	public static string ExecutablePath(string toolId, string home)
	{
		if (string.IsNullOrEmpty(home)) return null;

		var bin = BinSubdir(toolId);
		var dir = string.IsNullOrEmpty(bin) ? home : Path.Combine(home, bin);
		return Path.Combine(dir, ProbeExecutable(toolId));
	}

	/// <summary>The directory to prepend to <c>PATH</c> so this tool (and anything it execs) resolves to it.</summary>
	public static string BinDir(string toolId, string home)
	{
		if (string.IsNullOrEmpty(home)) return null;

		var bin = BinSubdir(toolId);
		return string.IsNullOrEmpty(bin) ? home : Path.Combine(home, bin);
	}

	private string InstallRoot(string toolId, string version) =>
		Path.Combine(_dir, toolId, SanitizeVersion(version));

	/// <summary>Version strings appear in path segments, and Adoptium's carry characters that are not portable.</summary>
	private static string SanitizeVersion(string version)
	{
		var cleaned = new string((version ?? "unknown")
			.Select(c => char.IsLetterOrDigit(c) || c == '.' || c == '-' || c == '_' ? c : '-').ToArray());
		return cleaned.Trim('-', '.');
	}

	// ----------------------------------------------------------------------------------
	// Ensure
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// Makes one tool available, returning what was done. Order of preference: an install this toolchain
	/// already has, then (under <c>--prefer-system</c>) a satisfying system install, then a fresh download.
	/// Never throws for an expected failure — the error lands on the result so one unreachable index does not
	/// abort the other tools.
	/// </summary>
	public async Task<ToolchainResult> EnsureAsync(string toolId, CancellationToken token)
	{
		try
		{
			if (!_options.force && TryExistingInstall(toolId, out var existing))
				return new ToolchainResult { toolId = toolId, entry = existing, action = "cached" };

			if (_options.preferSystem && TrySystemInstall(toolId, out var system))
				return new ToolchainResult { toolId = toolId, entry = system, action = "system" };

			if (toolId == ToolchainPins.Dotnet)
				return await EnsureDotnetAsync(token);

			var download = toolId switch
			{
				ToolchainPins.Jdk => await ToolchainPins.ResolveJdkAsync(_http, token),
				ToolchainPins.Maven => await ToolchainPins.ResolveMavenAsync(_http, token),
				ToolchainPins.Node => await ToolchainPins.ResolveNodeAsync(_http, token),
				_ => throw new CliException($"Unknown toolchain tool '{toolId}'.")
			};

			var root = InstallRoot(toolId, download.version);
			if (_options.dryRun)
			{
				return new ToolchainResult
				{
					toolId = toolId,
					action = "would install",
					entry = new ToolchainEntry { version = download.version, home = root, source = "downloaded" }
				};
			}

			var archive = await FetchAsync(download, token);
			var home = ExtractTool(toolId, archive, root);

			var entry = new ToolchainEntry { version = download.version, home = home, source = "downloaded" };
			Manifest.tools[toolId] = entry;
			return new ToolchainResult { toolId = toolId, entry = entry, action = "installed" };
		}
		catch (Exception e)
		{
			return new ToolchainResult { toolId = toolId, error = e.Message };
		}
	}

	/// <summary>
	/// True when this toolchain already holds a usable copy of the tool. Both the recorded home AND its
	/// executable must exist: a manifest entry alone is not proof, since the directory may have been cleaned.
	/// </summary>
	private bool TryExistingInstall(string toolId, out ToolchainEntry entry)
	{
		entry = null;
		if (!Manifest.tools.TryGetValue(toolId, out var recorded) || string.IsNullOrEmpty(recorded?.home))
			return false;

		var exe = ExecutablePath(toolId, recorded.home);
		if (exe == null || !File.Exists(exe))
			return false;

		entry = recorded;
		return true;
	}

	/// <summary>
	/// Adopts an already-installed tool when its version satisfies the pin (<c>--prefer-system</c>). Deliberately
	/// strict: a system tool that does not match the pin is ignored rather than warned about, because silently
	/// building the Scala reactor with the wrong Maven/JDK is the failure mode this whole command exists to remove.
	/// </summary>
	private bool TrySystemInstall(string toolId, out ToolchainEntry entry)
	{
		entry = null;

		if (toolId == ToolchainPins.Jdk)
		{
			// Reuse the CLI's existing Java-8 discovery (BEAM_JAVA_HOME → JAVA_HOME → java_home → common dirs);
			// it already validates that a candidate really is Java 8.
			if (!JavaPathOption.TryGetJavaHome(out var javaHome, out _))
				return false;

			entry = new ToolchainEntry { version = ReadVersion(toolId, javaHome) ?? "8", home = javaHome, source = "system" };
			return true;
		}

		var home = FindSystemHome(toolId);
		if (home == null)
			return false;

		var version = ReadVersion(toolId, home);
		if (!SatisfiesPin(toolId, version))
			return false;

		entry = new ToolchainEntry { version = version, home = home, source = "system" };
		return true;
	}

	/// <summary>
	/// Locates a system tool by walking <c>PATH</c> for its executable and stepping back out of the bin dir.
	/// <see cref="Process"/> resolution would find the executable but not its home, and Maven/Node both need the
	/// home (not just the exe) on the manifest.
	/// </summary>
	private static string FindSystemHome(string toolId)
	{
		var exeName = ProbeExecutable(toolId);
		var bin = BinSubdir(toolId);

		foreach (var pathDir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
			.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
		{
			string candidate;
			try
			{
				candidate = Path.Combine(pathDir.Trim(), exeName);
			}
			catch
			{
				continue; // an unusable PATH entry (invalid chars) is not worth failing setup over
			}

			if (!File.Exists(candidate))
				continue;

			var dir = Path.GetDirectoryName(Path.GetFullPath(candidate));
			// Step out of the bin dir to get the home, unless the tool's executables live at the root.
			return string.IsNullOrEmpty(bin) ? dir : Path.GetDirectoryName(dir);
		}

		return null;
	}

	/// <summary>Whether a discovered version is acceptable for the pin (exact for Maven/.NET, major for Node).</summary>
	public static bool SatisfiesPin(string toolId, string version)
	{
		if (string.IsNullOrWhiteSpace(version)) return false;

		return toolId switch
		{
			ToolchainPins.Maven => version.StartsWith(ToolchainPins.MavenVersion, StringComparison.Ordinal),
			ToolchainPins.Dotnet => version.StartsWith("10.", StringComparison.Ordinal),
			ToolchainPins.Node => version.TrimStart('v').StartsWith(ToolchainPins.NodeMajor + ".", StringComparison.Ordinal),
			ToolchainPins.Jdk => version.Contains("1.8.") || version.StartsWith("8", StringComparison.Ordinal),
			_ => false
		};
	}

	/// <summary>The version a tool reports, or null when it cannot be run.</summary>
	public static string ReadVersion(string toolId, string home)
	{
		var exe = ExecutablePath(toolId, home);
		if (exe == null || !File.Exists(exe)) return null;

		var (args, fromStdErr) = toolId switch
		{
			// `java -version` writes to stderr on 8.
			ToolchainPins.Jdk => ("-version", true),
			ToolchainPins.Maven => ("-version", false),
			ToolchainPins.Dotnet => ("--version", false),
			_ => ("--version", false)
		};

		var output = RunCapture(exe, args, out var exitCode, workingDirectory: null);
		if (exitCode != 0 && string.IsNullOrWhiteSpace(output)) return null;

		var text = output?.Trim();
		if (string.IsNullOrWhiteSpace(text)) return null;

		var firstLine = text.Split('\n').FirstOrDefault()?.Trim();
		return toolId switch
		{
			// `java version "1.8.0_502"` / `openjdk version "1.8.0_502"`
			ToolchainPins.Jdk => ExtractQuoted(firstLine) ?? firstLine,
			// `Apache Maven 3.9.9 (hash)`
			ToolchainPins.Maven => firstLine?.Split(' ').Skip(2).FirstOrDefault() ?? firstLine,
			_ => firstLine
		};

		static string ExtractQuoted(string line)
		{
			if (line == null) return null;
			var open = line.IndexOf('"');
			var close = open < 0 ? -1 : line.IndexOf('"', open + 1);
			return close > open ? line.Substring(open + 1, close - open - 1) : null;
		}
	}

	// ----------------------------------------------------------------------------------
	// .NET SDK (install script rather than a plain archive)
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// Installs the pinned .NET SDK by running Microsoft's official install script with <c>--install-dir</c>
	/// pointed into the toolchain. The SDK is not published as a single "extract me" archive per platform the
	/// way the JDK/Maven/Node are, and the script already resolves the right build for the host.
	/// </summary>
	private async Task<ToolchainResult> EnsureDotnetAsync(CancellationToken token)
	{
		var root = InstallRoot(ToolchainPins.Dotnet, ToolchainPins.DotnetVersion);

		if (_options.dryRun)
		{
			return new ToolchainResult
			{
				toolId = ToolchainPins.Dotnet,
				action = "would install",
				entry = new ToolchainEntry { version = ToolchainPins.DotnetVersion, home = root, source = "downloaded" }
			};
		}

		var script = Path.Combine(DownloadsDir, ToolchainPins.DotnetInstallScriptName);
		if (!File.Exists(script))
		{
			if (_options.offline)
				throw new CliException($"--offline: {script} is not in the download cache. Re-run without --offline once.");

			Directory.CreateDirectory(DownloadsDir);
			var body = await _http.GetStringAsync(ToolchainPins.DotnetInstallScriptUrl, token);
			File.WriteAllText(script, body);
			MakeExecutable(script);
		}

		Directory.CreateDirectory(root);
		var (exe, args) = OperatingSystem.IsWindows()
			? ("powershell", $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Version {ToolchainPins.DotnetVersion} -InstallDir \"{root}\"")
			: ("/bin/sh", $"\"{script}\" --version {ToolchainPins.DotnetVersion} --install-dir \"{root}\"");

		Log.Information($"Installing .NET SDK {ToolchainPins.DotnetVersion} into {root} ...");
		var output = RunCapture(exe, args, out var exitCode, workingDirectory: _dir);
		if (exitCode != 0)
			throw new CliException($"dotnet-install failed (exit {exitCode}): {Tail(output)}");

		var installed = ExecutablePath(ToolchainPins.Dotnet, root);
		if (!File.Exists(installed))
			throw new CliException($"dotnet-install reported success but produced no {installed}.");

		var entry = new ToolchainEntry { version = ToolchainPins.DotnetVersion, home = root, source = "downloaded" };
		Manifest.tools[ToolchainPins.Dotnet] = entry;
		return new ToolchainResult { toolId = ToolchainPins.Dotnet, entry = entry, action = "installed" };
	}

	// ----------------------------------------------------------------------------------
	// Download + extract
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// Returns the local path of a verified archive, downloading it into <c>downloads/</c> when it is not
	/// already cached. A cached file whose digest does not match is deleted and re-fetched rather than reused —
	/// the usual cause is an interrupted earlier run.
	/// </summary>
	private async Task<string> FetchAsync(ToolchainPins.Download download, CancellationToken token)
	{
		Directory.CreateDirectory(DownloadsDir);
		var target = Path.Combine(DownloadsDir, download.fileName);

		if (File.Exists(target))
		{
			if (VerifyDigest(target, download, out _))
				return target;

			Log.Warning($"Cached {download.fileName} failed its checksum — re-downloading.");
			File.Delete(target);
		}

		if (_options.offline)
			throw new CliException($"--offline: {download.fileName} is not in the download cache at {DownloadsDir}.");

		Log.Information($"Downloading {download.fileName} ...");
		var temp = target + ".partial";
		try
		{
			using (var res = await _http.GetAsync(download.url, HttpCompletionOption.ResponseHeadersRead, token))
			{
				if (!res.IsSuccessStatusCode)
					throw new CliException($"GET {download.url} returned {(int)res.StatusCode} {res.ReasonPhrase}.");

				await using var src = await res.Content.ReadAsStreamAsync(token);
				await using var dst = File.Create(temp);
				await src.CopyToAsync(dst, token);
			}

			if (!VerifyDigest(temp, download, out var actual))
			{
				throw new CliException(
					$"Checksum mismatch for {download.fileName}: expected {download.sha256 ?? download.sha512}, got {actual}. " +
					"The download was corrupted or the upstream artifact changed — nothing was installed.");
			}

			File.Move(temp, target, overwrite: true);
			return target;
		}
		finally
		{
			// Never leave a .partial behind to be mistaken for a cached archive on the next run.
			if (File.Exists(temp))
			{
				try { File.Delete(temp); } catch { /* best effort */ }
			}
		}
	}

	/// <summary>
	/// Verifies a file against whichever digest the upstream publishes. Returns true when no digest was
	/// available at all — the alternative is refusing to install from a source that simply does not publish one.
	/// </summary>
	private static bool VerifyDigest(string path, ToolchainPins.Download download, out string actual)
	{
		actual = null;
		if (!string.IsNullOrWhiteSpace(download.sha256))
		{
			actual = HashFile(path, SHA256.Create());
			return string.Equals(actual, download.sha256.Trim().ToLowerInvariant(), StringComparison.Ordinal);
		}

		if (!string.IsNullOrWhiteSpace(download.sha512))
		{
			actual = HashFile(path, SHA512.Create());
			return string.Equals(actual, download.sha512.Trim().ToLowerInvariant(), StringComparison.Ordinal);
		}

		Log.Warning($"{Path.GetFileName(path)} was not checksum-verified — upstream published no digest.");
		return true;
	}

	private static string HashFile(string path, HashAlgorithm algorithm)
	{
		using (algorithm)
		{
			using var stream = File.OpenRead(path);
			return Convert.ToHexString(algorithm.ComputeHash(stream)).ToLowerInvariant();
		}
	}

	/// <summary>
	/// Extracts an archive into <paramref name="root"/> and returns the tool's home inside it.
	///
	/// Extraction goes to a sibling temp directory first and is then moved into place, so a run interrupted
	/// mid-extract cannot leave a partial install that <see cref="TryExistingInstall"/> would accept.
	/// </summary>
	private string ExtractTool(string toolId, string archive, string root)
	{
		if (Directory.Exists(root))
			Directory.Delete(root, recursive: true);

		var staging = root + ".staging";
		if (Directory.Exists(staging))
			Directory.Delete(staging, recursive: true);

		Directory.CreateDirectory(staging);
		try
		{
			Extract(archive, staging);

			// These archives all wrap their content in a single top-level directory (apache-maven-3.9.9/,
			// node-v22.x.y-darwin-arm64/, jdk8u502-b07/). Unwrap it so the layout is <root>/bin/... rather than
			// <root>/<vendor-dir>/bin/..., which would have to be re-discovered on every read of the manifest.
			var unwrapped = UnwrapSingleDirectory(staging);

			Directory.CreateDirectory(Path.GetDirectoryName(root));
			Directory.Move(unwrapped, root);

			var home = ResolveHome(toolId, root);
			MakeBinExecutable(BinDir(toolId, home));

			var exe = ExecutablePath(toolId, home);
			if (!File.Exists(exe))
				throw new CliException($"Extracted {toolId} to {root} but found no executable at {exe}.");

			return home;
		}
		finally
		{
			if (Directory.Exists(staging))
			{
				try { Directory.Delete(staging, recursive: true); } catch { /* best effort */ }
			}
		}
	}

	private static void Extract(string archive, string destination)
	{
		if (archive.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
		{
			ZipFile.ExtractToDirectory(archive, destination, overwriteFiles: true);
			return;
		}

		using var file = File.OpenRead(archive);
		using var gzip = new GZipStream(file, CompressionMode.Decompress);
		TarFile.ExtractToDirectory(gzip, destination, overwriteFiles: true);
	}

	/// <summary>
	/// When a directory holds exactly one sub-directory and nothing else, returns that sub-directory —
	/// the vendor wrapper folder every one of these archives has.
	/// </summary>
	private static string UnwrapSingleDirectory(string dir)
	{
		var entries = Directory.GetFileSystemEntries(dir);
		if (entries.Length == 1 && Directory.Exists(entries[0]))
			return entries[0];

		return dir;
	}

	/// <summary>
	/// The tool's home inside its install root. Only the macOS JDK differs: Temurin/Zulu ship a
	/// <c>.jdk</c> bundle whose real <c>JAVA_HOME</c> is <c>Contents/Home</c>, so pointing
	/// <c>JAVA_HOME</c> at the extracted root would give a home with no <c>bin/java</c> in it.
	/// </summary>
	private static string ResolveHome(string toolId, string root)
	{
		if (toolId == ToolchainPins.Jdk)
		{
			var bundleHome = Path.Combine(root, "Contents", "Home");
			if (Directory.Exists(bundleHome))
				return bundleHome;

			// Some macOS tarballs wrap the bundle itself (zulu-8.jdk/Contents/Home).
			foreach (var child in Directory.GetDirectories(root, "*.jdk"))
			{
				var nested = Path.Combine(child, "Contents", "Home");
				if (Directory.Exists(nested))
					return nested;
			}
		}

		return root;
	}

	/// <summary>
	/// Restores the executable bit on a tool's launchers. Tar entries carry their mode and .NET applies it, but
	/// zip entries do not carry Unix permissions at all — so on a POSIX host a zip-sourced tool would extract
	/// unrunnable. Cheap enough to do unconditionally.
	/// </summary>
	private static void MakeBinExecutable(string binDir)
	{
		if (OperatingSystem.IsWindows() || string.IsNullOrEmpty(binDir) || !Directory.Exists(binDir))
			return;

		foreach (var file in Directory.GetFiles(binDir))
			MakeExecutable(file);
	}

	private static void MakeExecutable(string file)
	{
		if (OperatingSystem.IsWindows()) return;

		try
		{
			var mode = File.GetUnixFileMode(file);
			File.SetUnixFileMode(file, mode
				| UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);
		}
		catch (Exception e)
		{
			Log.Debug($"Could not mark {file} executable: {e.Message}");
		}
	}

	// ----------------------------------------------------------------------------------
	// Manifest projection
	// ----------------------------------------------------------------------------------

	/// <summary>
	/// Projects the installed tools into the block a local-stack manifest carries, so <c>beam local up</c> can
	/// substitute <c>${java}</c>/<c>${maven}</c>/<c>${node}</c>/<c>${dotnet}</c> and build a <c>PATH</c> without
	/// re-probing anything.
	/// </summary>
	public LocalStackToolchain ToManifestBlock() => ToManifestBlock(Manifest, _dir);

	/// <inheritdoc cref="ToManifestBlock()"/>
	public static LocalStackToolchain ToManifestBlock(ToolchainManifest manifest, string dir)
	{
		string HomeOf(string toolId) =>
			manifest != null && manifest.tools.TryGetValue(toolId, out var entry) ? entry?.home : null;

		return new LocalStackToolchain
		{
			dir = dir,
			java = HomeOf(ToolchainPins.Jdk),
			maven = HomeOf(ToolchainPins.Maven),
			dotnet = HomeOf(ToolchainPins.Dotnet),
			node = HomeOf(ToolchainPins.Node)
		};
	}

	/// <summary>
	/// Reads whatever <c>beam local setup</c> installed into <paramref name="overrideDir"/> (or the default
	/// location), as a manifest block — or null when no toolchain is there.
	///
	/// This is what lets <c>setup</c> run BEFORE <c>init</c>: setup records its installs in the toolchain
	/// directory, and <c>init</c> then picks them up on its own. Nothing has to be typed twice, and the
	/// <c>.beamable</c> workspace is not a prerequisite for installing a JDK.
	///
	/// Only a toolchain whose executables actually exist is returned — a stale record pointing at a deleted
	/// directory must not be baked into a fresh manifest.
	/// </summary>
	public static LocalStackToolchain TryReadInstalled(string overrideDir = null)
	{
		try
		{
			var dir = ResolveDir(overrideDir);
			var manifest = LoadManifest(Path.Combine(dir, ManifestFileName));
			if (manifest.tools.Count == 0) return null;

			var usable = manifest.tools
				.Where(kv =>
				{
					var exe = ExecutablePath(kv.Key, kv.Value?.home);
					return exe != null && File.Exists(exe);
				})
				.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);

			if (usable.Count == 0) return null;

			return ToManifestBlock(new ToolchainManifest { tools = usable }, dir);
		}
		catch
		{
			return null; // never let toolchain discovery break the caller
		}
	}

	/// <summary>
	/// The JDK 8 home <c>beam local setup</c> installed, or null. Used as the highest-priority source when
	/// resolving <c>JAVA_HOME</c>, so the pinned JDK wins over whatever the machine happens to have — which is
	/// the whole point of installing it.
	/// </summary>
	public static string TryReadInstalledJavaHome(string overrideDir = null) =>
		TryReadInstalled(overrideDir)?.java;

	/// <summary>Runs a process to completion, returning stdout+stderr combined.</summary>
	private static string RunCapture(string exe, string arguments, out int exitCode, string workingDirectory)
	{
		var psi = new ProcessStartInfo
		{
			FileName = exe,
			Arguments = arguments,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true
		};

		if (!string.IsNullOrEmpty(workingDirectory) && Directory.Exists(workingDirectory))
			psi.WorkingDirectory = workingDirectory;

		var output = new StringBuilder();
		try
		{
			using var proc = Process.Start(psi);
			if (proc == null)
			{
				exitCode = -1;
				return null;
			}

			output.Append(proc.StandardOutput.ReadToEnd());
			output.Append(proc.StandardError.ReadToEnd());
			proc.WaitForExit();
			exitCode = proc.ExitCode;
		}
		catch (Exception e)
		{
			exitCode = -1;
			return e.Message;
		}

		return output.ToString();
	}

	/// <summary>The last few lines of a captured output, for an error message that stays readable.</summary>
	private static string Tail(string output, int lines = 8)
	{
		if (string.IsNullOrWhiteSpace(output)) return "(no output)";

		var all = output.TrimEnd().Split('\n');
		return string.Join("\n", all.Skip(Math.Max(0, all.Length - lines)).Select(l => l.TrimEnd()));
	}
}
