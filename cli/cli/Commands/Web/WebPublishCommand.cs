using Beamable.Common;
using Beamable.Server;
using cli.Services;
using cli.Services.Web;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli.Web;

public class WebPublishCommandArgs : CommandArgs
{
	public string ProductDir;
	public string Registry;
	public string Cdn;
	public string Only;
	public string Version;
	public bool SkipBuild;
	public bool ForceInstall;
}

public class WebPublishedPackage
{
	/// <summary>The npm package name.</summary>
	public string package;
	/// <summary>The version in the source package.json (a release placeholder; not what gets published).</summary>
	public string sourceVersion;
	/// <summary>The local-dev version it was published as.</summary>
	public string publishedVersion;
}

public class WebPublishCommandResults
{
	public string registry;
	/// <summary>The local-dev version every published package shares.</summary>
	public string version;
	public List<WebPublishedPackage> published = new List<WebPublishedPackage>();
}

/// <summary>
/// Builds the local <c>@beamable/sdk</c> and <c>@beamable/portal-toolkit</c> and publishes them to the
/// local Verdaccio registry under an incrementing local-dev version. Backs <c>dev-web.sh</c>.
///
/// <para>
/// Both packages are published as the single fixed version <c>0.0.123</c> — the same "this is a developer
/// build" sentinel the .NET side uses — and the toolkit's <c>@beamable/sdk</c> peer dependency is pointed
/// at it, because that peer dep is what the Portal reads to decide which SDK to load for an extension.
/// </para>
/// <para>
/// Holding the version still keeps consumers' pins stable, so they are written once instead of on every
/// publish. The cost is that content changes under a version that already exists, so this command has to
/// unpublish before publishing, and flush the CDN's cache afterwards.
/// </para>
/// </summary>
public class WebPublishCommand : AtomicCommand<WebPublishCommandArgs, WebPublishCommandResults>, IStandaloneCommand, ISkipManifest
{
	private const string OnlySdk = "sdk";
	private const string OnlyToolkit = "toolkit";

	/// <summary>A web package that can be shadow-published, and the build recipe from its own prepublishOnly script.</summary>
	private class WebPackage
	{
		public string key;
		public string npmName;
		public string relativeDir;
		/// <summary>pnpm invocations to run, in order, to produce the publishable output.</summary>
		public string[] buildSteps;
		/// <summary>
		/// Directories of TRACKED files that the build steps regenerate, relative to the package. They are
		/// snapshotted before the build and restored afterwards so a local publish never dirties the repo.
		/// </summary>
		public string[] regeneratedTrackedDirs = Array.Empty<string>();
	}

	private static readonly WebPackage Sdk = new WebPackage
	{
		key = OnlySdk,
		npmName = WebLocalRegistryService.SdkPackage,
		relativeDir = "web",
		// mirrors web/package.json "prepublishOnly": "pnpm build"
		buildSteps = new[] { "build" }
	};

	private static readonly WebPackage Toolkit = new WebPackage
	{
		key = OnlyToolkit,
		npmName = WebLocalRegistryService.ToolkitPackage,
		relativeDir = "beam-portal-toolkit",
		// mirrors beam-portal-toolkit/package.json "prepublishOnly": "pnpm sync-components --no-copy && pnpm build"
		buildSteps = new[] { "sync-components --no-copy", "build" },
		// sync-components rewrites these, stamping the current package version into web-types.json. Without
		// restoring them a publish leaves modified tracked files behind — which is how a "0.0.123-local19"
		// version stamp ended up committed under the old script flow.
		regeneratedTrackedDirs = new[] { Path.Combine("src", "generated") }
	};

	public WebPublishCommand() : base("publish",
		"Build the local Beamable web SDK and Portal Toolkit and publish them to the local registry under a new local-dev version")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--product-dir", "Absolute path to the BeamableProduct checkout holding web/ and beam-portal-toolkit/; defaults to searching upwards from the working directory"),
			(args, i) => args.ProductDir = i);
		AddOption(new Option<string>("--registry", () => WebLocalRegistryService.DefaultRegistry, "The local npm registry to publish to"),
			(args, i) => args.Registry = i);
		AddOption(new Option<string>("--cdn", () => WebLocalRegistryService.DefaultCdn, "The local unpkg-style CDN whose file cache is flushed after publishing"),
			(args, i) => args.Cdn = i);
		AddOption(new Option<string>("--only", "Rebuild just one package, either 'sdk' or 'toolkit'; both are still published, since their versions must match"),
			(args, i) => args.Only = i);
		AddOption(new Option<string>("--version", "Publish as this version instead of the standard local-dev version (0.0.123)"),
			(args, i) => args.Version = i);
		AddOption(new Option<bool>("--skip-build", "Publish whatever is already built instead of rebuilding first"),
			(args, i) => args.SkipBuild = i);
		AddOption(new Option<bool>("--force-install", "Run 'pnpm install' before building even when node_modules already exists; use after the packages' dependencies changed"),
			(args, i) => args.ForceInstall = i);
	}

	public override async Task<WebPublishCommandResults> GetResult(WebPublishCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.Only) && args.Only != OnlySdk && args.Only != OnlyToolkit)
		{
			throw new CliException($"--only must be '{OnlySdk}' or '{OnlyToolkit}', but was [{args.Only}]");
		}

		var registry = string.IsNullOrEmpty(args.Registry) ? WebLocalRegistryService.DefaultRegistry : args.Registry;
		var cdn = string.IsNullOrEmpty(args.Cdn) ? WebLocalRegistryService.DefaultCdn : args.Cdn;
		var service = args.Provider.GetService<WebLocalRegistryService>();

		var productDir = ResolveProductDir(args);

		if (!await service.IsRegistryReachable(registry))
		{
			throw new CliException(
				$"The local registry at [{registry}] is not reachable. Start it with 'beam local up' (when initialized with --with-web-registry), " +
				$"or run 'docker compose up -d' in [{Path.Combine(productDir, "portal-localdev")}]");
		}

		// One fixed version, shared by both packages. Deliberately NOT the version in either source
		// package.json: the SDK's is a 1.0.0 placeholder CI stamps at release time, and neither says
		// anything about being a local build. Holding it constant is what keeps consumers' pins stable.
		var version = string.IsNullOrEmpty(args.Version) ? WebLocalRegistryService.LocalDevVersion : args.Version;
		Log.Information($"Publishing as [{version}]");

		// BOTH packages are always published, at the same version — `--only` selects what gets rebuilt,
		// not what gets published. They cannot be published independently: the Portal resolves an
		// extension's SDK through the toolkit's @beamable/sdk peer dependency, so a toolkit published at
		// version N names an SDK at version N. Publishing only one leaves that peer dep pointing at a
		// version nothing published, and the SDK fetch 404s at runtime.
		var packages = new List<WebPackage> { Sdk, Toolkit };

		var results = new WebPublishCommandResults { registry = registry, version = version };

		foreach (var pkg in packages)
		{
			// Skip the build for the package that wasn't selected — its dist is unchanged and gets
			// republished as-is. Unless there is no dist yet, in which case it has to be built.
			var skipThisBuild = args.SkipBuild
				|| (!string.IsNullOrEmpty(args.Only)
					&& args.Only != pkg.key
					&& Directory.Exists(Path.Combine(productDir, pkg.relativeDir, "dist")));

			if (skipThisBuild && !args.SkipBuild)
			{
				Log.Verbose($"Not rebuilding {pkg.npmName} (--only {args.Only}); republishing its existing dist");
			}

			results.published.Add(PublishPackage(productDir, pkg, version, registry, skipThisBuild, args.ForceInstall));
		}

		// Load-bearing now that the version repeats: the CDN caches file contents keyed by pkg@version, so
		// without this it would keep serving the build we just replaced.
		await service.FlushCdnCache(cdn);

		Log.Information($"Published {results.published.Count} package(s) as [{version}] to [{registry}].");
		Log.Information("Refresh the projects that use it with 'beam web use' (run in the repository that holds them).");
		return results;
	}

	/// <summary>
	/// Removes this version from the local registry so it can be published again — Verdaccio rejects a
	/// publish for a version it already holds. Only ever touches the local-dev version, which exists
	/// nowhere upstream, so it cannot disturb anything the npm uplink serves.
	/// </summary>
	private static void UnpublishExisting(WebPackage pkg, string version, string registry)
	{
		try
		{
			var result = StartProcessUtil.Run("npm",
				$"unpublish {pkg.npmName}@{version} --force --registry {registry} {WebLocalRegistryService.AuthTokenFlag(registry)}",
				useShell: true).WaitForResult();

			// A "not found" here is the normal first-publish case, so a non-zero exit isn't an error. A real
			// problem surfaces on the publish that follows, with a clearer message.
			Log.Verbose(result.exit == 0
				? $"  Removed the previous {pkg.npmName}@{version} from the registry"
				: $"  No existing {pkg.npmName}@{version} to remove; continuing");
		}
		catch (Exception e)
		{
			Log.Verbose($"  Could not remove {pkg.npmName}@{version}: {e.Message}");
		}
	}

	private static WebPublishedPackage PublishPackage(
		string productDir,
		WebPackage pkg,
		string publishAsVersion,
		string registry,
		bool skipBuild,
		bool forceInstall)
	{
		var dir = Path.Combine(productDir, pkg.relativeDir);
		if (!Directory.Exists(dir))
		{
			throw new CliException($"No [{pkg.relativeDir}] directory at [{productDir}]. Pass --product-dir to point at the BeamableProduct checkout");
		}

		var packageJsonPath = Path.Combine(dir, "package.json");
		var sourceVersion = WebLocalRegistryService.ReadOwnVersion(packageJsonPath);

		Log.Information($"--- {pkg.npmName} ---");
		Log.Information($"  Publishing source version [{sourceVersion}] as local build [{publishAsVersion}]");

		// Everything below runs with the manifest temporarily stamped to the local-dev version, and every
		// tracked file it touches snapshotted. The finally block always puts them back: these are tracked
		// files, and leaving a local version stamp behind is exactly the leak into git the old script flow
		// kept causing (beam-portal-toolkit/src/generated/web-types.json still carries one).
		var originalManifest = File.ReadAllText(packageJsonPath);
		var regeneratedSnapshot = SnapshotFiles(dir, pkg.regeneratedTrackedDirs);

		try
		{
			// Stamped BEFORE the build so generated artifacts that bake the version (the toolkit's
			// web-types.json) agree with the version actually published. For the toolkit this also points
			// its @beamable/sdk peer dep at the same version — that peer dep is what the Portal reads to
			// pick an extension's SDK, so the runtime chain only resolves locally if the two agree.
			StampManifest(packageJsonPath, publishAsVersion, pkg.key == OnlyToolkit ? publishAsVersion : null);

			if (!skipBuild)
			{
				// Dependencies are installed when they're missing, or on request — a normal iteration
				// shouldn't pay for a pnpm install, but one is required after the package's own
				// dependencies changed, since the build would otherwise compile against stale ones.
				if (forceInstall || !Directory.Exists(Path.Combine(dir, "node_modules")))
				{
					RunPnpm(dir, "install", pkg.npmName);
				}

				foreach (var step in pkg.buildSteps)
				{
					RunPnpm(dir, step, pkg.npmName);
				}
			}

			// The version repeats every run, so the old one has to go first.
			UnpublishExisting(pkg, publishAsVersion, registry);

			// --ignore-scripts because the build steps above already ran this package's prepublishOnly recipe.
			var publish = StartProcessUtil.Run("npm",
				$"publish --registry {registry} --tag {WebLocalRegistryService.LocalDistTag} --ignore-scripts {WebLocalRegistryService.AuthTokenFlag(registry)}",
				useShell: true, workingDirectoryPath: dir).WaitForResult();
			if (publish.exit != 0)
			{
				throw new CliException($"Failed to publish {pkg.npmName}@{publishAsVersion} to [{registry}]. " +
					$"Errors: \n{publish.stderr}\nAll logs: {publish.stdout}");
			}
		}
		finally
		{
			File.WriteAllText(packageJsonPath, originalManifest);
			RestoreFiles(regeneratedSnapshot);
		}

		Log.Information($"  Published {pkg.npmName}@{publishAsVersion}");
		return new WebPublishedPackage
		{
			package = pkg.npmName,
			sourceVersion = sourceVersion,
			publishedVersion = publishAsVersion
		};
	}

	/// <summary>
	/// Writes the shadow version into a package.json, and for the toolkit also aligns its
	/// <c>@beamable/sdk</c> peer dependency, since the Portal reads that peer dep to decide which SDK
	/// version to load for an extension. Preserves the rest of the file.
	/// </summary>
	private static void StampManifest(string packageJsonPath, string version, string sdkVersionOrNull)
	{
		var root = JObject.Parse(File.ReadAllText(packageJsonPath));
		root["version"] = version;

		if (!string.IsNullOrEmpty(sdkVersionOrNull))
		{
			if (root["peerDependencies"] is JObject peers && peers[WebLocalRegistryService.SdkPackage] != null)
			{
				peers[WebLocalRegistryService.SdkPackage] = sdkVersionOrNull;
			}
		}

		File.WriteAllText(packageJsonPath, root.ToString(Formatting.Indented));
	}

	/// <summary>
	/// Reads every file under the given package-relative directories into memory so the build's edits to
	/// tracked, generated sources can be undone. These directories hold a handful of small files, so this
	/// stays cheap.
	/// </summary>
	private static Dictionary<string, byte[]> SnapshotFiles(string packageDir, string[] relativeDirs)
	{
		var snapshot = new Dictionary<string, byte[]>(StringComparer.Ordinal);
		foreach (var relative in relativeDirs)
		{
			var absolute = Path.Combine(packageDir, relative);
			if (!Directory.Exists(absolute))
			{
				continue;
			}

			foreach (var file in Directory.GetFiles(absolute, "*", SearchOption.AllDirectories))
			{
				try
				{
					snapshot[file] = File.ReadAllBytes(file);
				}
				catch (Exception e)
				{
					Log.Verbose($"Could not snapshot [{file}] for restore: {e.Message}");
				}
			}
		}

		return snapshot;
	}

	/// <summary>
	/// Restores a <see cref="SnapshotFiles"/> result, but only where the content actually differs, so the
	/// file timestamps of untouched files are left alone. Best-effort: a failure here means a modified
	/// generated file is left behind, which is a nuisance rather than a broken publish.
	/// </summary>
	private static void RestoreFiles(Dictionary<string, byte[]> snapshot)
	{
		foreach (var (file, contents) in snapshot)
		{
			try
			{
				if (File.Exists(file) && File.ReadAllBytes(file).AsSpan().SequenceEqual(contents))
				{
					continue;
				}

				File.WriteAllBytes(file, contents);
				Log.Verbose($"Restored generated file [{file}]");
			}
			catch (Exception e)
			{
				Log.Warning($"Could not restore [{file}] after publishing: {e.Message}");
			}
		}
	}

	private static void RunPnpm(string directory, string arguments, string packageName)
	{
		Log.Verbose($"  [cmd] pnpm {arguments}");
		var result = StartProcessUtil.Run("pnpm", arguments, useShell: true, workingDirectoryPath: directory).WaitForResult();
		if (result.exit != 0)
		{
			throw new CliException($"'pnpm {arguments}' failed for {packageName} in [{directory}]. " +
				$"Errors: \n{result.stderr}\nAll logs: {result.stdout}");
		}
	}

	private static string ResolveProductDir(WebPublishCommandArgs args)
	{
		if (!string.IsNullOrEmpty(args.ProductDir))
		{
			if (!Directory.Exists(args.ProductDir))
			{
				throw new CliException($"--product-dir [{args.ProductDir}] does not exist");
			}

			return Path.GetFullPath(args.ProductDir);
		}

		var found = WebLocalRegistryService.FindProductDir(Directory.GetCurrentDirectory());
		if (string.IsNullOrEmpty(found))
		{
			throw new CliException(
				"Could not find a BeamableProduct checkout containing web/ and beam-portal-toolkit/. " +
				"Run this from inside that repository, or pass --product-dir");
		}

		Log.Verbose($"Using product directory [{found}]");
		return found;
	}
}
