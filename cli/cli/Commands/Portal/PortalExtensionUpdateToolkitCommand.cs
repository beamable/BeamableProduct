using Beamable.Server;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json.Linq;
using System.CommandLine;

namespace cli.Portal;

public class PortalExtensionUpdateToolkitCommandArgs : CommandArgs
{
	public string Version;
	public bool Local;
	public string Registry;
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
	}

	public override async Task<PortalExtensionUpdateToolkitCommandResults> GetResult(PortalExtensionUpdateToolkitCommandArgs args)
	{
		if (args.Local && !string.IsNullOrEmpty(args.Version))
		{
			throw new CliException("Cannot use --local and --version together. Pass one or the other, or neither to use the latest published version");
		}

		var targetVersion = await ResolveTargetVersion(args);
		Log.Information($"Updating {TOOLKIT_PACKAGE} to version [{targetVersion}]");

		var result = new PortalExtensionUpdateToolkitCommandResults { version = targetVersion };

		// Collect every portal extension and portal extension library package.json, de-duplicated by path.
		var targets = new Dictionary<string, string>(StringComparer.Ordinal); // packageJsonPath -> display name

		foreach (var serviceDefinition in args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			         .Where(p => p.Protocol == BeamoProtocolType.PortalExtension))
		{
			var def = serviceDefinition.PortalExtensionDefinition;
			targets[Path.GetFullPath(def.AbsolutePackageJsonPath)] = def.Name;
		}

		foreach (var lib in PortalExtensionAddLibraryCommand.LocateAllLibraries(args.ConfigService.BeamableWorkspace))
		{
			targets[lib.PackageJsonPath] = lib.Name;
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
		await RunInstallsConcurrently(directoriesToInstall);

		Log.Information($"Updated {result.updated.Count} project(s); skipped {result.skipped.Count} project(s) that were already on the target version or had no {TOOLKIT_PACKAGE} dependency");
		return result;
	}

	/// <summary>
	/// Runs <c>npm install</c> in each given directory with a bounded degree of concurrency. The audit and
	/// funding steps are disabled and the cache is preferred, since neither is needed to refresh a single
	/// dependency and both add hundreds of milliseconds per call.
	/// </summary>
	private async Task RunInstallsConcurrently(List<(string name, string directory)> directories)
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
				var handle = StartProcessUtil.Run(
					"npm",
					"install --no-audit --no-fund --prefer-offline",
					useShell: true,
					workingDirectoryPath: target.directory);

				await handle.ExitedTask;
				var result = handle.WaitForResult();
				if (result.exit != 0)
				{
					Log.Warning($"Updated {TOOLKIT_PACKAGE} in [{target.name}], but 'npm install' failed. " +
						$"Run it manually in the project directory to resolve packages. Errors: \n{result.stderr}");
				}
			}
			catch (Exception e)
			{
				// Best-effort: a failure to spawn/await npm must not fail the whole update.
				Log.Warning($"Updated {TOOLKIT_PACKAGE} in [{target.name}], but 'npm install' could not be run. " +
					$"Run it manually in the project directory to resolve packages. Message = [{e.Message}]");
			}
			finally
			{
				gate.Release();
			}
		});

		await Task.WhenAll(installs);
	}

	/// <summary>
	/// Resolves the toolkit version to write based on the options:
	/// --local uses verdaccio's "local" dist-tag, --version validates the version exists in npm or
	/// verdaccio, and with no options the npm registry's "latest" dist-tag is used.
	/// </summary>
	private async Task<string> ResolveTargetVersion(PortalExtensionUpdateToolkitCommandArgs args)
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
				throw new CliException($"No 'local' version of {TOOLKIT_PACKAGE} found on verdaccio [{verdaccio}]. Publish it first with dev-web.sh");
			}

			return localVersion;
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

			return args.Version;
		}

		var latestPackument = await versionService.GetNpmPackument(TOOLKIT_PACKAGE, NPM_REGISTRY);
		if (latestPackument?.DistTags == null ||
		    !latestPackument.DistTags.TryGetValue("latest", out var latestVersion) ||
		    string.IsNullOrEmpty(latestVersion))
		{
			throw new CliException($"Could not determine the latest version of {TOOLKIT_PACKAGE} from the npm registry");
		}

		return latestVersion;
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
