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

		foreach (var (packageJsonPath, name) in targets)
		{
			if (UpdateToolkitVersion(name, packageJsonPath, targetVersion))
			{
				result.updated.Add(name);
			}
			else
			{
				result.skipped.Add(name);
			}
		}

		Log.Information($"Updated {result.updated.Count} project(s); skipped {result.skipped.Count} project(s) without a {TOOLKIT_PACKAGE} dependency");
		return result;
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
	/// Rewrites the @beamable/portal-toolkit reference in every dependency block of the given
	/// package.json that already contains it, then runs a best-effort npm install. Returns false
	/// (and logs) when no toolkit dependency is present, leaving the file untouched.
	/// </summary>
	private bool UpdateToolkitVersion(string name, string packageJsonPath, string targetVersion)
	{
		try
		{
			if (!RewriteToolkitVersion(packageJsonPath, targetVersion, out var previousVersion))
			{
				Log.Information($"Skipping [{name}] - no {TOOLKIT_PACKAGE} dependency found in its package.json");
				return false;
			}

			Log.Information($"Updated [{name}]: {TOOLKIT_PACKAGE} [{previousVersion}] -> [{targetVersion}]");

			// Best-effort install so node_modules reflects the new version. package.json is the source of
			// truth and the extension run flow installs again before building, so a failed/offline install
			// here must not fail the command.
			var directory = Path.GetDirectoryName(packageJsonPath);
			var result = StartProcessUtil.Run("npm", "install", useShell: true, workingDirectoryPath: directory).WaitForResult();
			if (result.exit != 0)
			{
				Log.Warning($"Updated {TOOLKIT_PACKAGE} in [{name}], but 'npm install' failed. " +
					$"Run it manually in the project directory to resolve packages. Errors: \n{result.stderr}");
			}

			return true;
		}
		catch (CliException)
		{
			throw;
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Could not update {TOOLKIT_PACKAGE} in [{name}]. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
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