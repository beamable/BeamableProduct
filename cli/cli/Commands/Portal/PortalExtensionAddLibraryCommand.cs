using Beamable.Server;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using static Beamable.Common.Constants.Features.PortalExtension;

namespace cli.Portal;

public class PortalExtensionAddLibraryCommandArgs : CommandArgs
{
	public string LibraryName;
	public List<string> ExtensionNames = new List<string>();
}

public class PortalExtensionAddLibraryCommand : AppCommand<PortalExtensionAddLibraryCommandArgs>, IEmptyResult
{
	public PortalExtensionAddLibraryCommand() : base("add-library", "Adds a shared TypeScript library as a dependency of one or more Portal Extensions")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("library", description: "The shared library that will be a new dependency of the specified Portal Extensions"),
			(args, i) => args.LibraryName = i);
		AddOption(new Option<List<string>>(
				name: "--extensions",
				description: "The list of Portal Extension names that the library will be added to (separated by whitespace)") { AllowMultipleArgumentsPerToken = true, Arity = ArgumentArity.OneOrMore },
			(args, i) => args.ExtensionNames = i);
	}

	public override Task Handle(PortalExtensionAddLibraryCommandArgs args)
	{
		if (args.ExtensionNames == null || args.ExtensionNames.Count == 0)
		{
			throw new CliException("No Portal Extensions were specified. Use --extensions to list one or more extension names.");
		}

		var libraryPath = LocateLibrary(args.ConfigService.BeamableWorkspace, args.LibraryName);

		if (libraryPath == null)
		{
			throw new CliException(
				$"Couldn't find a Portal Extension library with the name: [{args.LibraryName}]. " +
				$"Create one with 'beam project new portal-extension-lib {args.LibraryName}'.");
		}

		var requestedNames = args.ExtensionNames.Distinct().ToList();
		var portalExtensions = args.BeamoLocalSystem.BeamoManifest.ServiceDefinitions
			.Where(p => p.Protocol == BeamoProtocolType.PortalExtension)
			.Where(p => requestedNames.Contains(p.PortalExtensionDefinition.Name))
			.ToList();

		var foundNames = portalExtensions
			.Select(p => p.PortalExtensionDefinition.Name)
			.ToHashSet();
		var missingNames = requestedNames.Where(n => !foundNames.Contains(n)).ToList();

		if (missingNames.Count > 0)
		{
			throw new CliException($"Couldn't find Portal Extension services with the names: [{string.Join(", ", missingNames)}]");
		}

		foreach (var serviceDefinition in portalExtensions)
		{
			AddLibraryToExtension(args, serviceDefinition.PortalExtensionDefinition, libraryPath);
		}

		return Task.CompletedTask;
	}

	private void AddLibraryToExtension(PortalExtensionAddLibraryCommandArgs args, PortalExtensionDef extension, string libraryPath)
	{
		try
		{
			var packagePath = extension.AbsolutePackageJsonPath;
			string jsonContent = File.ReadAllText(packagePath);

			JObject root = JObject.Parse(jsonContent);

			var specifier = ComputeFileSpecifier(extension.AbsolutePath, libraryPath);

			var dependencies = root[EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME] as JObject;
			if (dependencies == null)
			{
				dependencies = new JObject();
				root[EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME] = dependencies;
			}

			// Upsert: re-running with a moved library updates the stale path instead of erroring.
			dependencies[args.LibraryName] = specifier;

			File.WriteAllText(packagePath, root.ToString(Newtonsoft.Json.Formatting.Indented));

			// Install so the file: dependency is symlinked into node_modules and types resolve immediately.
			// Best-effort: package.json is the source of truth, and the extension run flow installs deps
			// again before building, so a failed/offline install here must not fail the command.
			var result = StartProcessUtil.Run("npm", "install", useShell: true, workingDirectoryPath: extension.AbsolutePath).WaitForResult();
			if (result.exit != 0)
			{
				Log.Warning($"Added library [{args.LibraryName}] to [{extension.Name}], but 'npm install' failed. " +
					$"Run it manually in the extension directory to resolve types. Errors: \n{result.stderr}");
			}
		}
		catch (CliException)
		{
			throw;
		}
		catch (Exception e)
		{
			throw new CliException(
				$"Could not add library [{args.LibraryName}] to extension [{extension.Name}]. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}
	}

	/// <summary>
	/// Builds the npm "file:" dependency specifier pointing from a portal extension directory to a
	/// shared library directory.
	/// </summary>
	public static string ComputeFileSpecifier(string extensionDir, string libAbsPath)
	{
		var relative = Path.GetRelativePath(extensionDir, libAbsPath).Replace('\\', '/');
		return $"file:{relative}";
	}

	/// <summary>
	/// Scans the workspace for any "package.json" marked as a portal extension library (via the
	/// "beamable": { "portalExtensionLib": true } property) whose name matches the given library name.
	/// Returns the library's absolute directory, or null if no matching library exists.
	/// </summary>
	public static string LocateLibrary(string workspace, string libName)
	{
		if (string.IsNullOrEmpty(workspace))
		{
			return null;
		}

		foreach (var packagePath in Directory.EnumerateFiles(workspace, "package.json", SearchOption.AllDirectories))
		{
			try
			{
				var info = JsonConvert.DeserializeObject<BeamoLocalSystem.PortalExtensionPackageInfo>(File.ReadAllText(packagePath));
				if (info?.BeamableProperties?.IsPortalExtensionLib == true &&
				    string.Equals(info.Name, libName, StringComparison.Ordinal))
				{
					return Path.GetFullPath(Path.GetDirectoryName(packagePath));
				}
			}
			catch
			{
				// ignore files that aren't valid package.json libraries
			}
		}

		return null;
	}

	/// <summary>
	/// Verifies that every "file:" library dependency in the extension's package.json still resolves to
	/// the library's real location. If a library moved (it can still be found by name in the workspace
	/// but the recorded path is wrong/broken), the path is auto-repaired. If a referenced library can't
	/// be found anywhere, a comprehensive exception is thrown so the user knows exactly what to fix.
	/// </summary>
	public static void ValidateAndRepairLibraryDependencies(PortalExtensionDef extension, string workspace)
	{
		var packagePath = extension.AbsolutePackageJsonPath;
		if (!File.Exists(packagePath))
		{
			return;
		}

		JObject root = JObject.Parse(File.ReadAllText(packagePath));
		var dependencies = root[EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME] as JObject;
		if (dependencies == null)
		{
			return;
		}

		var changed = false;

		foreach (var (depName, token) in dependencies)
		{
			var value = token?.ToString();
			if (string.IsNullOrEmpty(value) || !value.StartsWith("file:"))
			{
				continue;
			}

			var relPath = value.Substring("file:".Length);
			var resolvedAbs = Path.GetFullPath(Path.Combine(extension.AbsolutePath, relPath));

			var libAbsPath = LocateLibrary(workspace, depName);

			if (libAbsPath != null)
			{
				var expectedAbs = Path.GetFullPath(libAbsPath);
				var alreadyCorrect = string.Equals(
					resolvedAbs.TrimEnd(Path.DirectorySeparatorChar),
					expectedAbs.TrimEnd(Path.DirectorySeparatorChar),
					StringComparison.Ordinal) && Directory.Exists(resolvedAbs);

				if (!alreadyCorrect)
				{
					var repaired = ComputeFileSpecifier(extension.AbsolutePath, libAbsPath);
					Log.Warning($"Repairing library dependency [{depName}] in extension [{extension.Name}]: [{value}] -> [{repaired}]");
					dependencies[depName] = repaired;
					changed = true;
				}
			}
			else if (!Directory.Exists(resolvedAbs))
			{
				// References a library that can't be found by name and whose recorded path no longer exists.
				throw new CliException(
					$"Portal Extension [{extension.Name}] depends on library [{depName}] via [{value}], " +
					$"but that path does not exist (resolved to [{resolvedAbs}]) and no matching library was found. " +
					$"Recreate it with 'beam project new portal-extension-lib {depName}' or re-add it with " +
					$"'beam portal extension add-library {extension.Name} {depName}'.");
			}
		}

		if (changed)
		{
			File.WriteAllText(packagePath, root.ToString(Newtonsoft.Json.Formatting.Indented));
		}
	}

	private const string TOOLKIT_PACKAGE = "@beamable/portal-toolkit";

	/// <summary>
	/// Validates that the extension and its file:-linked libraries agree on the @beamable/portal-toolkit
	/// version, using npm's own dependency resolver rather than comparing strings ourselves.
	///
	/// npm does not evaluate a symlinked file: package's peerDependencies against the consumer, so a plain
	/// install never catches this. We therefore run a NON-mutating dry-run with `--install-links`
	/// (which makes npm resolve the library as a real package, evaluating its peers) and
	/// `--strict-peer-deps` (which turns an incompatible peer into an ERESOLVE failure). The dry-run does
	/// not touch node_modules, so the real symlinked install — and the live-reload dev loop — is unaffected.
	///
	/// The strict resolver also reports unrelated peer noise (e.g. react/react-dom), so we only fail when
	/// the conflict npm reports actually concerns the toolkit package.
	/// </summary>
	public static void ValidateLibraryPeerDependencies(PortalExtensionDef extension)
	{
		var result = StartProcessUtil.Run(
			"npm",
			"install --install-links --strict-peer-deps --dry-run --no-audit --no-fund",
			useShell: true,
			workingDirectoryPath: extension.AbsolutePath).WaitForResult();

		var output = $"{result.stdout}\n{result.stderr}";

		if (HasToolkitVersionConflict(result.exit, output))
		{
			throw new CliException(
				$"{TOOLKIT_PACKAGE} version conflict detected by npm for Portal Extension [{extension.Name}]. " +
				$"An extension and one of its libraries require incompatible {TOOLKIT_PACKAGE} versions; " +
				$"align them so both use the same version.\n\nnpm reported:\n{output.Trim()}");
		}
	}

	/// <summary>
	/// True when npm's dependency resolution failed (non-zero exit) AND the reported conflict involves the
	/// toolkit package. Kept pure so the scoping logic can be unit-tested without invoking npm.
	/// </summary>
	public static bool HasToolkitVersionConflict(int npmExitCode, string npmOutput)
	{
		if (npmExitCode == 0 || string.IsNullOrEmpty(npmOutput))
		{
			return false;
		}

		return npmOutput.Contains(TOOLKIT_PACKAGE, StringComparison.Ordinal);
	}
}
