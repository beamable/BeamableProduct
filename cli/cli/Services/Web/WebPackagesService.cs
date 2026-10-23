using Beamable.Server;
using cli.Services.LocalStack;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace cli.Services.Web;

/// <summary>What <c>web: build</c> last built: each package's local version and the fingerprint of its sources.</summary>
public class WebPackagesState
{
	public WebPackageBuild sdk;
	public WebPackageBuild toolkit;
}

public class WebPackageBuild
{
	public string version;
	public string fingerprint;
}

/// <summary>
/// The local builds a workspace's extensions run on while a stack with the web packages runs, written by
/// <c>web: install</c>. In a local run, each extension's dependency install puts these versions over the ones its
/// lockfile pins, without saving them, so package.json and the lockfile never change; a deploy never reads it.
/// <c>beam local stop</c> deletes it and puts the locked versions back.
/// </summary>
public class WebPackagesOverlay
{
	public string registry;
	public Dictionary<string, string> packages = new Dictionary<string, string>(StringComparer.Ordinal);
}

/// <summary>
/// The local loop for <c>@beamable/sdk</c> and <c>@beamable/portal-toolkit</c>, run by the stack's <c>web: *</c> steps:
/// build each package where its sources changed, under a version of its own (<see cref="LocalVersion"/>), publish it to
/// the local registry, and point the stack's extensions at it through <see cref="WebPackagesOverlay"/>. A version is
/// never reused, so nothing is unpublished, and npm's cache and the browser's never hold a stale copy of one.
/// </summary>
public static class WebPackagesService
{
	public const string StateFile = "web-packages.json";
	public const string OverlayFile = "web-overlay.json";

	/// <summary>Left in an extension's node_modules once the overlay is installed there, so stop knows what to put back.</summary>
	public const string OverlayMarker = ".beamable-web-overlay";

	/// <summary>The environment variables the <c>web: *</c> steps get their folders from: arguments are split on spaces,
	/// and a folder can have one.</summary>
	public const string ProductDirVariable = "BEAM_WEB_PRODUCT_DIR";

	/// <inheritdoc cref="ProductDirVariable"/>
	public const string WorkspaceVariable = "BEAM_WEB_WORKSPACE";

	/// <summary>How many npm installs run at once: each resolves a whole project, and one per processor made them queue.</summary>
	public static int MaxParallelInstalls => Math.Clamp(Environment.ProcessorCount / 4, 2, 6);

	private class Package
	{
		public string npmName;
		public string dir;
		public string[] buildSteps;

		/// <summary>Tracked folders the build regenerates, put back afterwards so a local build leaves git clean.</summary>
		public string[] regenerated = Array.Empty<string>();
	}

	private static readonly Package Sdk = new Package
	{
		npmName = WebLocalRegistryService.SdkPackage, dir = "web",
		// web/package.json "prepublishOnly"
		buildSteps = new[] { "build" }
	};

	private static readonly Package Toolkit = new Package
	{
		npmName = WebLocalRegistryService.ToolkitPackage, dir = "beam-portal-toolkit",
		// beam-portal-toolkit/package.json "prepublishOnly"
		buildSteps = new[] { "sync-components --no-copy", "build" },
		regenerated = new[] { Path.Combine("src", "generated") }
	};

	public static string StatePath(string workspace) => Path.Combine(workspace, ".beamable", "local", StateFile);

	public static string OverlayPath(string workspace) => Path.Combine(workspace, ".beamable", "local", OverlayFile);

	/// <summary>
	/// The version local builds of a package start from: the highest version npmjs has, next minor
	/// (<c>0.5.0-rc.8</c> → <c>0.6.0</c>), so a local build is newer than anything published so far. The SDK's own
	/// package.json says 1.0.0, which would put a local build below every release. Null when there's nothing to go on.
	/// </summary>
	public static string NextBase(IEnumerable<string> published)
	{
		(int major, int minor)? highest = null;
		foreach (var version in published ?? Enumerable.Empty<string>())
		{
			var core = (version ?? string.Empty).Split('-', '+')[0].Split('.');
			if (core.Length != 3 || !int.TryParse(core[0], out var major) || !int.TryParse(core[1], out var minor)) continue;
			if (highest == null || (major, minor).CompareTo(highest.Value) > 0) highest = (major, minor);
		}

		return highest is { } h ? $"{h.major}.{h.minor + 1}.0" : null;
	}

	/// <summary>A local build's version: the base, then <c>-0.</c> and the time, which sorts each build after the last
	/// and below the base's own prereleases (<c>-dev.N</c>, <c>-rc.N</c>). Build metadata after <c>+</c> would be ignored
	/// by semver, so two builds would compare equal.</summary>
	public static string LocalVersion(string baseVersion, DateTime utc) => $"{baseVersion}-0.{utc:yyyyMMddHHmmss}";

	/// <summary>
	/// Builds the SDK and the toolkit where their sources changed since the last build, or both with
	/// <paramref name="force"/>, each under a new local version. A new SDK rebuilds the toolkit too, which names the SDK it
	/// runs on and compiles against it; a toolkit change alone keeps the SDK as it is.
	/// </summary>
	public static async Task<WebPackagesState> Build(string productDir, string workspace, bool force, CancellationToken token)
	{
		var statePath = StatePath(workspace);
		var state = Load<WebPackagesState>(statePath) ?? new WebPackagesState();
		var sdkPrint = LocalStackBuilds.FolderFingerprint(Path.Combine(productDir, Sdk.dir));
		var toolkitPrint = LocalStackBuilds.FolderFingerprint(Path.Combine(productDir, Toolkit.dir));

		var buildSdk = force || Stale(productDir, Sdk, state.sdk, sdkPrint);
		var buildToolkit = buildSdk || Stale(productDir, Toolkit, state.toolkit, toolkitPrint);
		if (!buildToolkit)
		{
			Log.Information($"Up to date: {Sdk.npmName}@{state.sdk.version}, {Toolkit.npmName}@{state.toolkit.version}.");
			return state;
		}

		var now = DateTime.UtcNow;
		if (buildSdk)
		{
			var version = LocalVersion(await BaseFor(productDir, Sdk, state.sdk, token), now);
			BuildPackage(productDir, Sdk, version, null);
			state.sdk = new WebPackageBuild { version = version, fingerprint = sdkPrint };
			Save(statePath, state);
		}

		var toolkitVersion = LocalVersion(await BaseFor(productDir, Toolkit, state.toolkit, token), now);
		BuildPackage(productDir, Toolkit, toolkitVersion, state.sdk.version);
		state.toolkit = new WebPackageBuild { version = toolkitVersion, fingerprint = toolkitPrint };
		Save(statePath, state);
		return state;
	}

	private static bool Stale(string productDir, Package package, WebPackageBuild last, string fingerprint) =>
		last?.version == null || fingerprint == null || last.fingerprint != fingerprint
		|| !Directory.Exists(Path.Combine(productDir, package.dir, "dist"));

	/// <summary>The base for <paramref name="package"/>'s next local version: from npmjs, else the last local build's
	/// (offline), else its own package.json's.</summary>
	private static async Task<string> BaseFor(string productDir, Package package, WebPackageBuild last, CancellationToken token)
	{
		var fromNpm = NextBase(await PublishedVersions(package.npmName, token));
		if (fromNpm != null) return fromNpm;
		if (last?.version != null) return last.version.Split('-')[0];
		return NextBase(new[] { WebLocalRegistryService.ReadOwnVersion(Path.Combine(productDir, package.dir, "package.json")) });
	}

	private static async Task<List<string>> PublishedVersions(string package, CancellationToken token)
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
			var json = JObject.Parse(await http.GetStringAsync($"{WebLocalRegistryService.PublicRegistry.TrimEnd('/')}/{package.Replace("/", "%2f")}", token));
			return (json["versions"] as JObject)?.Properties().Select(p => p.Name).ToList() ?? new List<string>();
		}
		catch (Exception e) when (e is not OperationCanceledException)
		{
			Log.Warning($"Couldn't ask npmjs for the versions of {package} ({e.Message}); the local version keeps its last base.");
			return new List<string>();
		}
	}

	/// <summary>
	/// Builds one package with its package.json stamped to <paramref name="version"/> (the toolkit's SDK peer and dev
	/// dependency to <paramref name="sdkVersion"/>, and its SDK pointed at the local one), so what the build bakes in
	/// matches what gets published. Every tracked file it touches is put back.
	/// </summary>
	private static void BuildPackage(string productDir, Package package, string version, string sdkVersion)
	{
		var dir = Path.Combine(productDir, package.dir);
		Log.Information($"Building {package.npmName}@{version}");
		using var stamped = new Stamped(dir, package, version, sdkVersion);
		if (!Directory.Exists(Path.Combine(dir, "node_modules")))
			Pnpm(dir, "install", package.npmName);

		using var localSdk = sdkVersion != null ? SdkLink.To(dir, Path.Combine(productDir, Sdk.dir)) : null;
		foreach (var step in package.buildSteps)
			Pnpm(dir, step, package.npmName);
	}

	/// <summary>
	/// Publishes what <see cref="Build"/> recorded to the local registry, each version once: one the registry already
	/// has is left as it is, so a run after a purged registry publishes again and any other run publishes nothing.
	/// </summary>
	public static async Task Publish(string productDir, string workspace, string registry, CancellationToken token)
	{
		var state = Load<WebPackagesState>(StatePath(workspace));
		if (state?.sdk == null || state.toolkit == null)
			throw new CliException("There's no local build of the web packages yet. Run the stack with :build, or start it again so web: build runs.");

		foreach (var (package, build, sdkVersion) in new[] { (Sdk, state.sdk, (string)null), (Toolkit, state.toolkit, state.sdk.version) })
		{
			if (await RegistryHas(registry, package.npmName, build.version, token))
			{
				Log.Information($"{package.npmName}@{build.version} is in the local registry already.");
				continue;
			}

			var dir = Path.Combine(productDir, package.dir);
			using (new Stamped(dir, package, build.version, sdkVersion, regenerated: false))
			{
				// --ignore-scripts: web: build already ran the package's prepublishOnly recipe.
				var result = StartProcessUtil.Run("npm",
					$"publish --registry {registry} --tag {WebLocalRegistryService.LocalDistTag} --ignore-scripts {WebLocalRegistryService.AuthTokenFlag(registry)}",
					useShell: true, workingDirectoryPath: dir).WaitForResult();
				if (result.exit != 0)
					throw new CliException($"Couldn't publish {package.npmName}@{build.version} to {registry}:\n{result.stderr}\n{result.stdout}".Trim());
			}

			Log.Information($"Published {package.npmName}@{build.version} to {registry}.");
		}
	}

	/// <summary>True when <paramref name="registry"/> serves <paramref name="version"/> of <paramref name="package"/>.</summary>
	public static async Task<bool> RegistryHas(string registry, string package, string version, CancellationToken token)
	{
		try
		{
			using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
			var json = JObject.Parse(await http.GetStringAsync($"{registry.TrimEnd('/')}/{package.Replace("/", "%2f")}", token));
			return json["versions"]?[version] != null;
		}
		catch (Exception e) when (e is not OperationCanceledException)
		{
			Log.Verbose($"Couldn't ask {registry} for {package}: {e.Message}");
			return false;
		}
	}

	/// <summary>Points the workspace's extensions at the last local build: writes the overlay their installs read.</summary>
	public static WebPackagesOverlay WriteOverlay(string workspace, string registry)
	{
		var state = Load<WebPackagesState>(StatePath(workspace));
		if (state?.sdk == null || state.toolkit == null)
			throw new CliException("There's no local build of the web packages to point the extensions at yet.");

		var overlay = new WebPackagesOverlay
		{
			registry = registry,
			packages = { [Toolkit.npmName] = state.toolkit.version, [Sdk.npmName] = state.sdk.version }
		};
		Save(OverlayPath(workspace), overlay);
		Log.Information($"The extensions this stack runs use {Toolkit.npmName}@{state.toolkit.version} and {Sdk.npmName}@{state.sdk.version}.");
		return overlay;
	}

	public static WebPackagesOverlay LoadOverlay(string workspace) =>
		string.IsNullOrEmpty(workspace) ? null : Load<WebPackagesOverlay>(OverlayPath(workspace));

	/// <summary>The npm arguments that install <paramref name="overlay"/>'s versions without saving them, from the
	/// local registry for the <c>@beamable</c> scope only, so everything else still comes from npmjs.</summary>
	public static string OverlayInstallArgs(WebPackagesOverlay overlay) =>
		$"install --no-save --no-audit --no-fund {string.Join(" ", overlay.packages.Select(p => $"{p.Key}@{p.Value}"))} " +
		$"{WebLocalRegistryService.ScopedRegistryFlag(overlay.registry)} {WebLocalRegistryService.AuthTokenFlag(overlay.registry)}";

	/// <summary>
	/// Installs the overlay's versions into an extension over what its lockfile pinned. package.json and the lockfile
	/// are put back should npm write either, so a local build can never reach a tracked file.
	/// </summary>
	public static void ApplyOverlay(string projectDir, WebPackagesOverlay overlay)
	{
		var tracked = new[] { "package.json", "package-lock.json" }.Select(f => Path.Combine(projectDir, f)).Where(File.Exists)
			.ToDictionary(f => f, File.ReadAllBytes);
		try
		{
			var args = OverlayInstallArgs(overlay);
			var result = StartProcessUtil.Run("npm", args, useShell: true, workingDirectoryPath: projectDir).WaitForResult();
			// A library's exact peer on a released toolkit doesn't take a local one; peers are installed explicitly here.
			if (result.exit != 0 && $"{result.stderr}{result.stdout}".Contains("ERESOLVE", StringComparison.Ordinal))
				result = StartProcessUtil.Run("npm", args + " --legacy-peer-deps", useShell: true, workingDirectoryPath: projectDir).WaitForResult();
			if (result.exit != 0)
				throw new CliException($"Couldn't install the local web packages in {projectDir}:\n{result.stderr}\n{result.stdout}".Trim());

			File.WriteAllText(Path.Combine(projectDir, "node_modules", OverlayMarker), JsonConvert.SerializeObject(overlay.packages));
		}
		finally
		{
			foreach (var (file, bytes) in tracked)
				if (!File.Exists(file) || !File.ReadAllBytes(file).AsSpan().SequenceEqual(bytes)) File.WriteAllBytes(file, bytes);
		}
	}

	/// <summary>
	/// Undoes the overlay: deletes it, and puts each extension that installed it back on the versions its lockfile
	/// pins. Returns how many were put back.
	/// </summary>
	public static int RemoveOverlay(string workspace)
	{
		if (string.IsNullOrEmpty(workspace)) return 0;
		var overlayPath = OverlayPath(workspace);
		if (File.Exists(overlayPath)) File.Delete(overlayPath);

		var projects = WebLocalRegistryService.FindExtensionProjects(workspace)
			.Select(p => Path.GetDirectoryName(p.packageJsonPath))
			.Where(dir => File.Exists(Path.Combine(dir, "node_modules", OverlayMarker)))
			.ToList();
		if (projects.Count == 0) return 0;

		Log.Information($"Putting {projects.Count} extension(s) back on the web packages their lockfiles pin.");
		Parallel.ForEach(projects, new ParallelOptions { MaxDegreeOfParallelism = MaxParallelInstalls }, dir =>
		{
			var result = StartProcessUtil.Run("npm", "install --no-audit --no-fund", useShell: true, workingDirectoryPath: dir).WaitForResult();
			if (result.exit == 0) File.Delete(Path.Combine(dir, "node_modules", OverlayMarker));
			else Log.Warning($"Couldn't put {dir} back on its locked web packages; run `npm install` there. npm said: {result.stderr}");
		});
		return projects.Count;
	}

	/// <summary>
	/// Watches the SDK's and the toolkit's sources. After a burst of saves settles, builds what changed, publishes it,
	/// points the overlay at it and calls <paramref name="changed"/> (the stack restarts its extensions, which install
	/// the new build). A failed build is reported and the watch goes on. Runs until <paramref name="token"/> ends.
	/// </summary>
	public static async Task Watch(string productDir, string workspace, string registry, Func<Task> changed, CancellationToken token)
	{
		var lastChange = DateTime.MinValue;
		var pending = new SemaphoreSlim(0);
		var building = false;
		var watchers = new List<FileSystemWatcher>();
		foreach (var folder in new[] { Sdk, Toolkit }.Select(p => Path.Combine(productDir, p.dir, "src")).Where(Directory.Exists))
		{
			var watcher = new FileSystemWatcher(folder) { IncludeSubdirectories = true, EnableRaisingEvents = true };
			FileSystemEventHandler onChange = (_, e) =>
			{
				// The toolkit's build regenerates src/generated; that is the build's own output, not an edit.
				if (building || e.FullPath.Replace('\\', '/').Contains("/generated/", StringComparison.Ordinal)) return;
				lastChange = DateTime.UtcNow;
				pending.Release();
			};
			watcher.Changed += onChange;
			watcher.Created += onChange;
			watcher.Deleted += onChange;
			watcher.Renamed += (s, e) => onChange(s, e);
			watchers.Add(watcher);
		}

		Log.Information($"Watching {string.Join(" and ", watchers.Select(w => w.Path))} for changes.");
		try
		{
			while (!token.IsCancellationRequested)
			{
				await pending.WaitAsync(token);
				while (DateTime.UtcNow - lastChange < TimeSpan.FromSeconds(1))
					await Task.Delay(250, token);
				while (pending.CurrentCount > 0) pending.Wait(0);

				building = true;
				try
				{
					var before = LoadOverlay(workspace)?.packages.GetValueOrDefault(Toolkit.npmName);
					await Build(productDir, workspace, force: false, token);
					await Publish(productDir, workspace, registry, token);
					if (WriteOverlay(workspace, registry).packages[Toolkit.npmName] != before)
						await changed();
				}
				catch (CliException e)
				{
					Log.Error($"{e.Message}\nWatching for the next change.");
				}
				finally
				{
					building = false;
				}
			}
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			foreach (var watcher in watchers) watcher.Dispose();
		}
	}

	private static void Pnpm(string dir, string arguments, string package)
	{
		var result = StartProcessUtil.Run("pnpm", arguments, useShell: true, workingDirectoryPath: dir).WaitForResult();
		if (result.exit != 0)
			throw new CliException($"'pnpm {arguments}' failed for {package} in {dir}:\n{result.stderr}\n{result.stdout}".Trim());
	}

	private static T Load<T>(string path) where T : class
	{
		try
		{
			return File.Exists(path) ? JsonConvert.DeserializeObject<T>(File.ReadAllText(path)) : null;
		}
		catch (JsonException)
		{
			return null;
		}
	}

	private static void Save(string path, object value)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		File.WriteAllText(path, JsonConvert.SerializeObject(value, Formatting.Indented));
	}

	/// <summary>A package's package.json stamped to a local version (and, for the toolkit, its SDK), with its regenerated
	/// tracked files snapshotted; disposing puts every one back as it was.</summary>
	private sealed class Stamped : IDisposable
	{
		private readonly Dictionary<string, byte[]> _snapshot = new Dictionary<string, byte[]>(StringComparer.Ordinal);

		public Stamped(string dir, Package package, string version, string sdkVersion, bool regenerated = true)
		{
			var manifest = Path.Combine(dir, "package.json");
			_snapshot[manifest] = File.ReadAllBytes(manifest);
			foreach (var folder in regenerated ? package.regenerated : Array.Empty<string>())
			{
				var full = Path.Combine(dir, folder);
				if (!Directory.Exists(full)) continue;
				foreach (var file in Directory.GetFiles(full, "*", SearchOption.AllDirectories))
					_snapshot[file] = File.ReadAllBytes(file);
			}

			var root = JObject.Parse(File.ReadAllText(manifest));
			root["version"] = version;
			if (sdkVersion != null)
			{
				foreach (var block in new[] { "peerDependencies", "devDependencies" })
					if (root[block] is JObject deps && deps[Sdk.npmName] != null)
						deps[Sdk.npmName] = sdkVersion;
			}

			File.WriteAllText(manifest, root.ToString(Formatting.Indented));
		}

		public void Dispose()
		{
			foreach (var (file, bytes) in _snapshot)
			{
				try
				{
					if (!File.Exists(file) || !File.ReadAllBytes(file).AsSpan().SequenceEqual(bytes))
						File.WriteAllBytes(file, bytes);
				}
				catch (Exception e)
				{
					Log.Warning($"Couldn't put {file} back after the local build: {e.Message}");
				}
			}
		}
	}

	/// <summary>
	/// The toolkit's <c>node_modules/@beamable/sdk</c> pointed at the local SDK while it builds, so it compiles against
	/// the SDK it will name, not the published one it declares for development; disposing puts pnpm's link back.
	/// </summary>
	private sealed class SdkLink : IDisposable
	{
		private readonly string _link;
		private readonly string _original;

		private SdkLink(string link, string original)
		{
			_link = link;
			_original = original;
		}

		public static SdkLink To(string toolkitDir, string sdkDir)
		{
			var link = Path.Combine(toolkitDir, "node_modules", "@beamable", "sdk");
			var info = new DirectoryInfo(link);
			if (!info.Exists || info.LinkTarget == null) return null;

			var original = info.LinkTarget;
			Relink(link, sdkDir);
			return new SdkLink(link, original);
		}

		public void Dispose()
		{
			try
			{
				Relink(_link, _original);
			}
			catch (Exception e)
			{
				Log.Warning($"Couldn't point {_link} back at {_original} ({e.Message}); `pnpm install` in the toolkit restores it.");
			}
		}

		/// <summary>Replaces the link; a junction on Windows, which pnpm uses there too and needs no privilege.</summary>
		private static void Relink(string link, string target)
		{
			Directory.Delete(link);
			if (OperatingSystem.IsWindows())
			{
				var result = StartProcessUtil.Run("cmd", $"/c mklink /J \"{link}\" \"{Path.GetFullPath(target, Path.GetDirectoryName(link)!)}\"", useShell: true).WaitForResult();
				if (result.exit != 0) throw new CliException($"Couldn't link {link} to {target}: {result.stderr}");
			}
			else
			{
				Directory.CreateSymbolicLink(link, target);
			}
		}
	}
}
