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
	/// The location of a portal extension library discovered in the workspace.
	/// </summary>
	public class PortalExtensionLibraryLocation
	{
		public string Name;
		public string Directory;
		public string PackageJsonPath;
	}

	/// <summary>
	/// Scans the workspace for every "package.json" marked as a portal extension library (via the
	/// "beamable": { "portalExtensionLib": true } property) and returns each one's name, absolute
	/// directory, and absolute package.json path.
	/// </summary>
	public static List<PortalExtensionLibraryLocation> LocateAllLibraries(string workspace)
	{
		var results = new List<PortalExtensionLibraryLocation>();
		if (string.IsNullOrEmpty(workspace))
		{
			return results;
		}

		foreach (var packagePath in EnumeratePackageJsonExcludingNodeModules(workspace))
		{
			try
			{
				var info = JsonConvert.DeserializeObject<BeamoLocalSystem.PortalExtensionPackageInfo>(File.ReadAllText(packagePath));
				if (info?.BeamableProperties?.IsPortalExtensionLib == true)
				{
					results.Add(new PortalExtensionLibraryLocation
					{
						Name = info.Name,
						Directory = Path.GetFullPath(Path.GetDirectoryName(packagePath)),
						PackageJsonPath = Path.GetFullPath(packagePath),
					});
				}
			}
			catch
			{
				// ignore files that aren't valid package.json libraries
			}
		}

		return results;
	}

	/// <summary>
	/// Enumerates every "package.json" under <paramref name="root"/>, skipping "node_modules" (and ".git")
	/// trees.
	///
	/// Skipping node_modules is essential, not just an optimization: a file:-linked library is symlinked into
	/// every consuming extension's node_modules, and .NET's recursive directory enumeration follows those
	/// symlinks. Walking into node_modules would therefore discover the same library once per consumer (under
	/// distinct symlink paths that Path.GetFullPath does not collapse), so callers would process — and rewrite,
	/// and npm-install — the one real library many times over.
	/// </summary>
	private static IEnumerable<string> EnumeratePackageJsonExcludingNodeModules(string root)
	{
		var pending = new Stack<string>();
		pending.Push(root);

		while (pending.Count > 0)
		{
			var directory = pending.Pop();

			var packageJson = Path.Combine(directory, "package.json");
			if (File.Exists(packageJson))
			{
				yield return packageJson;
			}

			string[] subdirectories;
			try
			{
				subdirectories = Directory.GetDirectories(directory);
			}
			catch
			{
				continue; // unreadable directory (permissions, broken link) — nothing to recurse into
			}

			foreach (var subdirectory in subdirectories)
			{
				var name = Path.GetFileName(subdirectory);
				if (string.Equals(name, "node_modules", StringComparison.OrdinalIgnoreCase)
					|| string.Equals(name, ".git", StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				pending.Push(subdirectory);
			}
		}
	}

	/// <summary>
	/// Scans the workspace for any "package.json" marked as a portal extension library (via the
	/// "beamable": { "portalExtensionLib": true } property) whose name matches the given library name.
	/// Returns the library's absolute directory, or null if no matching library exists.
	/// </summary>
	public static string LocateLibrary(string workspace, string libName)
	{
		return LocateAllLibraries(workspace)
			.FirstOrDefault(l => string.Equals(l.Name, libName, StringComparison.Ordinal))
			?.Directory;
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
	private const string REACT_PACKAGE = "react";

	// The packages a library and its host extension must agree on: a library is compiled against these as
	// peerDependencies, so the extension has to supply a version that satisfies the library's declared range.
	private static readonly string[] SHARED_PEER_PACKAGES = { TOOLKIT_PACKAGE, REACT_PACKAGE };

	/// <summary>
	/// The peerDependency ranges a single file:-linked library declares for the packages it shares with its
	/// host extension.
	/// </summary>
	public class LibraryPeerRequirements
	{
		public string LibraryName;
		public Dictionary<string, string> PeerRanges = new(StringComparer.Ordinal);
	}

	/// <summary>
	/// Validates that the extension supplies a version of every shared package (@beamable/portal-toolkit and
	/// react) that satisfies the peerDependency range each of its file:-linked libraries declares.
	///
	/// We compare versions directly rather than delegating to an npm dry-run. npm only evaluates a symlinked
	/// file: library's peerDependencies under `--install-links`, and that flag is broken for this case: it
	/// reports the satisfying package's version as `undefined`, producing a false ERESOLVE even when the
	/// versions match (the regression that motivated rewriting this method). Reading the versions ourselves is
	/// deterministic and immune to that bug. The check fails open — anything the matcher can't parse is treated
	/// as "no conflict" so users are never blocked on a range it doesn't understand.
	/// </summary>
	public static void ValidateLibraryPeerDependencies(PortalExtensionDef extension)
	{
		var packagePath = extension.AbsolutePackageJsonPath;
		if (!File.Exists(packagePath))
		{
			return;
		}

		JObject root = JObject.Parse(File.ReadAllText(packagePath));

		var libraries = ReadFileLibraryPeerRequirements(extension.AbsolutePath, root);
		if (libraries.Count == 0)
		{
			return;
		}

		// The concrete versions the extension actually provides, read from node_modules — i.e. what the build
		// will really use. Dependencies are installed (InstallDeps) before this validation runs.
		var providedVersions = new Dictionary<string, string>(StringComparer.Ordinal);
		foreach (var package in SHARED_PEER_PACKAGES)
		{
			var version = ReadInstalledPackageVersion(extension.AbsolutePath, package);
			if (version != null)
			{
				providedVersions[package] = version;
			}
		}

		var conflicts = DetectPeerVersionConflicts(providedVersions, libraries, SHARED_PEER_PACKAGES);
		if (conflicts.Count > 0)
		{
			throw new CliException(
				$"Dependency version conflict detected for Portal Extension [{extension.Name}]. " +
				$"An extension and one of its libraries require incompatible versions of a shared package; " +
				$"align them so both use the same version.\n\n" +
				string.Join("\n", conflicts.Select(c => $"  - {c}")));
		}
	}

	/// <summary>
	/// Reads the peerDependency ranges every file:-linked library in the extension's "dependencies" declares.
	/// Libraries whose package.json is missing or unparseable are skipped — the path-repair step already
	/// surfaces broken file: links, and an unreadable library can't be checked anyway.
	/// </summary>
	public static List<LibraryPeerRequirements> ReadFileLibraryPeerRequirements(string extensionDir, JObject extensionPackageJson)
	{
		var results = new List<LibraryPeerRequirements>();
		var dependencies = extensionPackageJson[EXTENSION_NPM_DEPENDENCIES_PROPERTY_NAME] as JObject;
		if (dependencies == null)
		{
			return results;
		}

		foreach (var (depName, token) in dependencies)
		{
			var value = token?.ToString();
			if (string.IsNullOrEmpty(value) || !value.StartsWith("file:"))
			{
				continue;
			}

			var libDir = Path.GetFullPath(Path.Combine(extensionDir, value.Substring("file:".Length)));
			var libPackagePath = Path.Combine(libDir, "package.json");
			if (!File.Exists(libPackagePath))
			{
				continue;
			}

			try
			{
				var libRoot = JObject.Parse(File.ReadAllText(libPackagePath));
				var requirement = new LibraryPeerRequirements { LibraryName = depName };
				if (libRoot["peerDependencies"] is JObject peers)
				{
					foreach (var (peerName, peerToken) in peers)
					{
						var peerRange = peerToken?.ToString();
						if (!string.IsNullOrEmpty(peerRange))
						{
							requirement.PeerRanges[peerName] = peerRange;
						}
					}
				}

				results.Add(requirement);
			}
			catch
			{
				// A library whose package.json can't be parsed can't be checked; skip it rather than fail.
			}
		}

		return results;
	}

	/// <summary>
	/// Reads the concrete "version" of a package installed under the extension's node_modules, or null if the
	/// package isn't installed or has no version field.
	/// </summary>
	public static string ReadInstalledPackageVersion(string extensionDir, string packageName)
	{
		try
		{
			var parts = new List<string> { extensionDir, "node_modules" };
			parts.AddRange(packageName.Split('/')); // scoped names like @beamable/portal-toolkit nest two dirs deep
			parts.Add("package.json");

			var packageJsonPath = Path.Combine(parts.ToArray());
			if (!File.Exists(packageJsonPath))
			{
				return null;
			}

			var root = JObject.Parse(File.ReadAllText(packageJsonPath));
			return root["version"]?.ToString();
		}
		catch
		{
			return null;
		}
	}

	/// <summary>
	/// Pure conflict detection: for each library and each shared package, flags a conflict when the version
	/// the extension provides does NOT satisfy the library's declared peer range. Fails open — an unknown
	/// provided version, or a range/version <see cref="NpmSemver"/> can't decide, is treated as "no conflict".
	/// Kept pure so it can be unit-tested without touching the filesystem or npm.
	/// </summary>
	public static List<string> DetectPeerVersionConflicts(
		IReadOnlyDictionary<string, string> providedVersions,
		IReadOnlyList<LibraryPeerRequirements> libraries,
		IReadOnlyList<string> packagesToCheck)
	{
		var conflicts = new List<string>();
		foreach (var library in libraries)
		{
			foreach (var package in packagesToCheck)
			{
				if (library.PeerRanges == null
					|| !library.PeerRanges.TryGetValue(package, out var range)
					|| string.IsNullOrWhiteSpace(range))
				{
					continue;
				}

				if (!providedVersions.TryGetValue(package, out var version) || string.IsNullOrWhiteSpace(version))
				{
					continue; // can't tell what the extension provides -> don't block
				}

				if (NpmSemver.TrySatisfies(version, range, out var satisfied) && !satisfied)
				{
					conflicts.Add(
						$"library [{library.LibraryName}] requires {package}@\"{range}\", " +
						$"but the extension provides {package}@{version}");
				}
			}
		}

		return conflicts;
	}
}
