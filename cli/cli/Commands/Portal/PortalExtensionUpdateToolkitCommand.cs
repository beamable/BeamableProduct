using Beamable.Server;
using cli.Services;
using cli.Services.Web;
using cli.Services.Bundles;
using cli.Utils;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using static Beamable.Common.Constants.Features.PortalExtension;

namespace cli.Portal;

public class PortalExtensionUpdateToolkitCommandArgs : CommandArgs
{
	public string Version;
	public bool Local;
	public string Registry;
	public bool BundlesOnly;
	public List<string> Ids = new List<string>();
}

public class PortalExtensionUpdateToolkitCommandResults
{
	public string version;
	public List<string> updated = new List<string>();
	public List<string> skipped = new List<string>();
}

public class PortalExtensionUpdateToolkitCommand : AtomicCommand<PortalExtensionUpdateToolkitCommandArgs, PortalExtensionUpdateToolkitCommandResults>
{
	private const string TOOLKIT_PACKAGE = "@beamable/portal-toolkit";
	private const string NPM_REGISTRY = "https://registry.npmjs.org";
	private const string DEFAULT_VERDACCIO_REGISTRY = "http://localhost:4873";

	private static readonly string[] DependencyBlocks = { "dependencies", "devDependencies", "peerDependencies" };

	public PortalExtensionUpdateToolkitCommand() : base("update-toolkit", "Updates the @beamable/portal-toolkit version of every Portal Extension and Portal Extension library in the workspace")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--version", "The @beamable/portal-toolkit version to update to; must exist in the npm registry or in verdaccio"),
			(args, i) => args.Version = i);
		AddOption(new Option<bool>("--local", "Update to the version currently published locally in verdaccio"),
			(args, i) => args.Local = i);
		AddOption(new Option<string>("--registry", () => DEFAULT_VERDACCIO_REGISTRY, "The verdaccio registry URL used for --local and for version existence checks"),
			(args, i) => args.Registry = i);
		AddOption(new Option<bool>("--bundles-only", "Only update Portal Extensions that are components of a bundle, skipping every other extension and library"),
			(args, i) => args.BundlesOnly = i);
		AddOption(new Option<List<string>>(
				name: "--ids",
				description: "Only update the Portal Extensions with these beamoIds (separated by whitespace); libraries are skipped") { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.OneOrMore },
			(args, i) => args.Ids = i);
	}

	public override async Task<PortalExtensionUpdateToolkitCommandResults> GetResult(PortalExtensionUpdateToolkitCommandArgs args)
	{
		if (args.Local && !string.IsNullOrEmpty(args.Version))
		{
			throw new CliException("Cannot use --local and --version together. Pass one or the other, or neither to use the latest published version");
		}

		var (targetVersion, installRegistry) = await ResolveTargetVersion(args);
		Log.Information($"Updating {TOOLKIT_PACKAGE} to version [{targetVersion}]");

		var result = new PortalExtensionUpdateToolkitCommandResults { version = targetVersion };

		// Collect every targeted portal extension and (unless a filter narrows the run) library package.json,
		// de-duplicated by path.
		var targets = new Dictionary<string, string>(StringComparer.Ordinal); // packageJsonPath -> display name

		var portalExtensions = SelectPortalExtensions(args);
		foreach (var serviceDefinition in portalExtensions)
		{
			var def = serviceDefinition.PortalExtensionDefinition;
			targets[Path.GetFullPath(def.AbsolutePackageJsonPath)] = def.Name;
		}

		if (!IsFilteringExtensions(args))
		{
			// No filter: the default is to update every tracked library in the workspace too.
			foreach (var lib in PortalExtensionAddLibraryCommand.LocateAllLibraries(args.ConfigService.BeamableWorkspace))
			{
				targets[lib.PackageJsonPath] = lib.Name;
			}
		}
		else
		{
			// Filtered run: a library isn't a bundle component and can't be named by --ids, but a selected
			// extension can still depend on one of our template-created libraries. Those must move to the same
			// toolkit version or the extension/library peer-dependency check would fail, so pull in the tracked
			// libraries the selected extensions reference (transitively).
			var selectedExtensionPackageJsonPaths = portalExtensions
				.Select(p => Path.GetFullPath(p.PortalExtensionDefinition.AbsolutePackageJsonPath));
			foreach (var lib in CollectDependentTrackedLibraries(selectedExtensionPackageJsonPaths, args.ConfigService.BeamableWorkspace))
			{
				targets[lib.PackageJsonPath] = lib.Name;
			}
		}

		// Phase 1: rewrite every package.json (fast, local file writes). Collect the directories whose toolkit
		// version actually changed so we only reinstall those.
		var directoriesToInstall = new List<(string name, string directory)>();
		foreach (var (packageJsonPath, name) in targets)
		{
			string previousVersion;
			bool found;
			try
			{
				found = RewriteToolkitVersion(packageJsonPath, targetVersion, out previousVersion);
			}
			catch (Exception e)
			{
				throw new CliException(
					$"Could not update {TOOLKIT_PACKAGE} in [{name}]. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
			}

			if (!found)
			{
				Log.Trace($"Skipping [{name}] - no {TOOLKIT_PACKAGE} dependency found in its package.json");
				result.skipped.Add(name);
				continue;
			}

			if (string.Equals(previousVersion, targetVersion, StringComparison.Ordinal))
			{
				// package.json already pointed at the target version; node_modules is already in sync, so there
				// is nothing for npm to do. This makes a re-run with the same version effectively instant.
				Log.Trace($"[{name}] already on {TOOLKIT_PACKAGE} [{targetVersion}] - skipping install");
				result.skipped.Add(name);
				continue;
			}

			Log.Trace($"Updated [{name}]: {TOOLKIT_PACKAGE} [{previousVersion}] -> [{targetVersion}]");
			result.updated.Add(name);
			directoriesToInstall.Add((name, Path.GetDirectoryName(packageJsonPath)));
		}

		// Phase 2: refresh node_modules. Each target has its own node_modules, so the installs are independent
		// and run concurrently; only the global npm cache is shared, which npm locks. Best-effort: package.json
		// is the source of truth and the run flow installs again before building, so a failed install only warns.
		await RunInstallsConcurrently(directoriesToInstall, installRegistry);

		Log.Information($"Updated {result.updated.Count} project(s); skipped {result.skipped.Count} project(s) that were already on the target version or had no {TOOLKIT_PACKAGE} dependency");
		return result;
	}

	/// <summary>
	/// True when the run has been narrowed to specific extensions via --bundles-only or --ids. In that mode
	/// libraries are skipped, since neither filter can name a library.
	/// </summary>
	private static bool IsFilteringExtensions(PortalExtensionUpdateToolkitCommandArgs args)
	{
		return args.BundlesOnly || (args.Ids != null && args.Ids.Count > 0);
	}

	/// <summary>
	/// Selects the portal extension service definitions to update, applying the --ids and --bundles-only
	/// filters. --ids restricts to the given beamoIds (validated: every id must resolve to a portal
	/// extension). --bundles-only restricts to extensions that are components of some *.beam.bundle.json.
	/// The two filters compose (their intersection is used when both are given).
	/// </summary>
	private static List<BeamoServiceDefinition> SelectPortalExtensions(PortalExtensionUpdateToolkitCommandArgs args)
	{
		var portalExtensions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(p => p.Protocol == BeamoProtocolType.PortalExtension)
			.ToList();

		if (args.Ids != null && args.Ids.Count > 0)
		{
			var requested = args.Ids.Distinct().ToList();
			var known = portalExtensions.Select(p => p.BeamoId).ToHashSet(StringComparer.Ordinal);
			var missing = requested.Where(id => !known.Contains(id)).ToList();
			if (missing.Count > 0)
			{
				throw new CliException($"Couldn't find Portal Extensions with the beamoIds: [{string.Join(", ", missing)}]");
			}

			var requestedSet = requested.ToHashSet(StringComparer.Ordinal);
			portalExtensions = portalExtensions.Where(p => requestedSet.Contains(p.BeamoId)).ToList();
		}

		if (args.BundlesOnly)
		{
			// Bundle components are beamoIds; for a portal extension the beamoId equals its package.json name.
			var bundledComponents = BundleWorkspace.Discover(args.ConfigService)
				.SelectMany(b => b.components)
				.ToHashSet(StringComparer.Ordinal);
			portalExtensions = portalExtensions.Where(p => bundledComponents.Contains(p.BeamoId)).ToList();
		}

		return portalExtensions;
	}

	/// <summary>
	/// Walks the "dependencies" of each selected extension and collects the workspace's tracked portal
	/// extension libraries they reference — the ones created from our template, discoverable via
	/// <see cref="PortalExtensionAddLibraryCommand.LocateAllLibraries"/> (a plain npm dependency that merely
	/// shares a name with a tracked library is not matched). The walk is transitive: a collected library's own
	/// dependencies are scanned too, so a library that depends on another tracked library is included. This is
	/// only used for filtered runs; an unfiltered run already updates every library.
	/// </summary>
	private static List<PortalExtensionAddLibraryCommand.PortalExtensionLibraryLocation> CollectDependentTrackedLibraries(
		IEnumerable<string> extensionPackageJsonPaths, string workspace)
	{
		var trackedLibraries = PortalExtensionAddLibraryCommand.LocateAllLibraries(workspace);
		if (trackedLibraries.Count == 0)
		{
			return new List<PortalExtensionAddLibraryCommand.PortalExtensionLibraryLocation>();
		}

		// The dependency key add-library writes is the library's package.json name. First tracked library wins
		// on a duplicate name, matching LocateLibrary's FirstOrDefault.
		var librariesByName = new Dictionary<string, PortalExtensionAddLibraryCommand.PortalExtensionLibraryLocation>(StringComparer.Ordinal);
		foreach (var lib in trackedLibraries)
		{
			if (!librariesByName.ContainsKey(lib.Name))
			{
				librariesByName[lib.Name] = lib;
			}
		}

		var collected = new Dictionary<string, PortalExtensionAddLibraryCommand.PortalExtensionLibraryLocation>(StringComparer.Ordinal); // packageJsonPath -> lib
		var toVisit = new Queue<string>(extensionPackageJsonPaths.Select(Path.GetFullPath));
		var visited = new HashSet<string>(StringComparer.Ordinal);

		while (toVisit.Count > 0)
		{
			var packageJsonPath = toVisit.Dequeue();
			if (!visited.Add(packageJsonPath))
			{
				continue;
			}

			foreach (var depName in ReadDependencyNames(packageJsonPath))
			{
				if (!librariesByName.TryGetValue(depName, out var lib))
				{
					continue; // not one of our tracked libraries
				}

				if (collected.TryAdd(lib.PackageJsonPath, lib))
				{
					// Newly discovered: scan its dependencies too, so transitively referenced libraries are caught.
					toVisit.Enqueue(Path.GetFullPath(lib.PackageJsonPath));
				}
			}
		}

		return collected.Values.ToList();
	}

	/// <summary>
	/// Reads the names declared in the "dependencies" block of a package.json, or an empty sequence if the
	/// file is missing/unparseable or has no such block.
	/// </summary>
	private static IEnumerable<string> ReadDependencyNames(string packageJsonPath)
	{
		try
		{
			var root = JObject.Parse(File.ReadAllText(packageJsonPath));
			if (root[EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME] is JObject dependencies)
			{
				return dependencies.Properties().Select(p => p.Name).ToList();
			}
		}
		catch
		{
			// A missing or malformed package.json contributes no library references.
		}

		return Enumerable.Empty<string>();
	}

	/// <summary>
	/// Runs <c>npm install</c> in each given directory with a bounded degree of concurrency. The audit and
	/// funding steps are disabled and the cache is preferred, since neither is needed to refresh a single
	/// dependency and both add hundreds of milliseconds per call. The registry is passed explicitly so the
	/// install does not inherit the user's global npm config (which, behind a proxy, may point at verdaccio).
	/// </summary>
	private async Task RunInstallsConcurrently(List<(string name, string directory)> directories, string registry)
	{
		if (directories.Count == 0)
		{
			return;
		}

		using var gate = new SemaphoreSlim(Math.Max(1, Environment.ProcessorCount));

		var installs = directories.Select(async target =>
		{
			await gate.WaitAsync();
			try
			{
				// A local developer build (0.0.123-*) exists only on the local registry, so the install has
				// to be routed there or npm 404s against npmjs. --prefer-offline is dropped in that case:
				// the whole point is to fetch a version that was just published.
				// A local dev build already carries its own "--registry <local>" in localArgs, so the explicit
				// --registry is only added on the normal path (where it stops npm inheriting the user's global
				// config, which behind a proxy may point at verdaccio).
				var localArgs = WebLocalRegistryService.InstallArgsFor(target.directory);
				var arguments = string.IsNullOrEmpty(localArgs)
					? $"install --no-audit --no-fund --prefer-offline --registry {registry}"
					: "install --no-audit --no-fund" + localArgs;

				var handle = StartProcessUtil.Run(
					"npm",
					arguments,
					useShell: true,
					workingDirectoryPath: target.directory);

				await handle.ExitedTask;
				var result = handle.WaitForResult();
				if (result.exit != 0)
				{
					Log.Warning($"Updated {TOOLKIT_PACKAGE} in [{target.name}], but 'npm install' failed." +
						$" Run it manually in the project directory to resolve packages. Errors: \n{result.stderr}");
				}
			}
			catch (Exception e)
			{
				// Best-effort: a failure to spawn/await npm must not fail the whole update.
				Log.Warning($"Updated {TOOLKIT_PACKAGE} in [{target.name}], but 'npm install' could not be run." +
					$" Run it manually in the project directory to resolve packages. Message = [{e.Message}]");
			}
			finally
			{
				gate.Release();
			}
		});

		await Task.WhenAll(installs);
	}

	/// <summary>
	/// Resolves the toolkit version to write based on the options, along with the npm registry that
	/// <c>npm install</c> must use for that version: --local uses verdaccio's "local" dist-tag (installed
	/// from verdaccio), --version validates the version exists in npm or verdaccio (installed from whichever
	/// registry actually has it), and with no options the public npm registry's "latest" dist-tag is used
	/// (installed from the public registry). The registry is resolved here — rather than left to the user's
	/// global npm config — so that, behind a proxy whose default registry points at verdaccio, a non-local
	/// update still pulls the toolkit from the public npm registry.
	/// </summary>
	private async Task<(string version, string registry)> ResolveTargetVersion(PortalExtensionUpdateToolkitCommandArgs args)
	{
		var versionService = args.Provider.GetService<VersionService>();
		var verdaccio = string.IsNullOrEmpty(args.Registry) ? DEFAULT_VERDACCIO_REGISTRY : args.Registry;

		if (args.Local)
		{
			var packument = await versionService.GetNpmPackument(TOOLKIT_PACKAGE, verdaccio);
			if (packument?.DistTags == null ||
			    !packument.DistTags.TryGetValue("local", out var localVersion) ||
			    string.IsNullOrEmpty(localVersion))
			{
				throw new CliException($"No 'local' version of {TOOLKIT_PACKAGE} found on verdaccio [{verdaccio}]. Publish it first with 'beam web publish'");
			}

			return (localVersion, verdaccio);
		}

		if (!string.IsNullOrEmpty(args.Version))
		{
			var npmPackument = await versionService.GetNpmPackument(TOOLKIT_PACKAGE, NPM_REGISTRY, throwOnError: false);
			var existsInNpm = npmPackument?.Versions?.ContainsKey(args.Version) == true;

			var existsInVerdaccio = false;
			if (!existsInNpm)
			{
				var verdaccioPackument = await versionService.GetNpmPackument(TOOLKIT_PACKAGE, verdaccio, throwOnError: false);
				existsInVerdaccio = verdaccioPackument?.Versions?.ContainsKey(args.Version) == true;
			}

			if (!existsInNpm && !existsInVerdaccio)
			{
				throw new CliException($"Version [{args.Version}] of {TOOLKIT_PACKAGE} was not found in the npm registry or in verdaccio [{verdaccio}]");
			}

			// Install from whichever registry actually has the version; prefer the public registry when both do.
			return (args.Version, existsInNpm ? NPM_REGISTRY : verdaccio);
		}

		var latestPackument = await versionService.GetNpmPackument(TOOLKIT_PACKAGE, NPM_REGISTRY);
		if (latestPackument?.DistTags == null ||
		    !latestPackument.DistTags.TryGetValue("latest", out var latestVersion) ||
		    string.IsNullOrEmpty(latestVersion))
		{
			throw new CliException($"Could not determine the latest version of {TOOLKIT_PACKAGE} from the npm registry");
		}

		return (latestVersion, NPM_REGISTRY);
	}

	/// <summary>
	/// Rewrites the @beamable/portal-toolkit version in every dependency block (dependencies,
	/// devDependencies, peerDependencies) of the package.json at <paramref name="packageJsonPath"/>
	/// that already references it, and writes the file back. Returns false and leaves the file
	/// untouched when no block references the toolkit. The previous version (the first occurrence
	/// found) is returned via <paramref name="previousVersion"/>. Kept pure (no npm install) so it
	/// can be unit-tested without a network or node toolchain.
	/// </summary>
	public static bool RewriteToolkitVersion(string packageJsonPath, string targetVersion, out string previousVersion)
	{
		previousVersion = null;
		var root = JObject.Parse(File.ReadAllText(packageJsonPath));

		var found = false;
		foreach (var block in DependencyBlocks)
		{
			if (root[block] is JObject dependencies && dependencies[TOOLKIT_PACKAGE] != null)
			{
				previousVersion ??= dependencies[TOOLKIT_PACKAGE].ToString();
				dependencies[TOOLKIT_PACKAGE] = targetVersion;
				found = true;
			}
		}

		if (!found)
		{
			return false;
		}

		File.WriteAllText(packageJsonPath, root.ToString(Newtonsoft.Json.Formatting.Indented));
		return true;
	}
}
