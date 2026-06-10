using Beamable.Server;
using cli.Services;
using cli.Utils;
using Newtonsoft.Json.Linq;
using System.CommandLine;
using static Beamable.Common.Constants.Features.PortalExtension;

namespace cli.Portal;

public class PortalExtensionAddLibraryCommandArgs : CommandArgs
{
	public string ExtensionName;
	public string LibraryName;
}

public class PortalExtensionAddLibraryCommand : AppCommand<PortalExtensionAddLibraryCommandArgs>, IEmptyResult
{
	public override bool IsForInternalUse => true;

	public PortalExtensionAddLibraryCommand() : base("add-library", "Adds a shared TypeScript library as a dependency of a Portal Extension")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("extension", description: "The Portal Extension name that the library will be added to"),
			(args, i) => args.ExtensionName = i);
		AddArgument(new Argument<string>("library", description: "The shared library that will be a new dependency of the specified Portal Extension"),
			(args, i) => args.LibraryName = i);
	}

	public override Task Handle(PortalExtensionAddLibraryCommandArgs args)
	{
		var manifest = args.BeamoLocalSystem.BeamoManifest;

		var extension = manifest.ServiceDefinitions
			.Where(p => p.Protocol == BeamoProtocolType.PortalExtension)
			.Select(s => s.PortalExtensionDefinition)
			.FirstOrDefault(p => p.Name == args.ExtensionName);

		if (extension == null)
		{
			throw new CliException($"Couldn't find a Portal Extension service with the name: [{args.ExtensionName}]");
		}

		var library = manifest.ServiceDefinitions
			.Where(p => p.Protocol == BeamoProtocolType.PortalExtensionLib)
			.Select(s => s.PortalExtensionLibDefinition)
			.FirstOrDefault(p => p.Name == args.LibraryName);

		if (library == null)
		{
			throw new CliException(
				$"Couldn't find a Portal Extension library with the name: [{args.LibraryName}]. " +
				$"Create one with 'beam project new portal-extension-lib {args.LibraryName}'.");
		}

		try
		{
			var packagePath = extension.AbsolutePackageJsonPath;
			string jsonContent = File.ReadAllText(packagePath);

			JObject root = JObject.Parse(jsonContent);

			var specifier = ComputeFileSpecifier(extension.AbsolutePath, library.AbsolutePath);

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
				Log.Warning($"Added library [{args.LibraryName}] to [{args.ExtensionName}], but 'npm install' failed. " +
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
				$"Could not add library [{args.LibraryName}] to extension [{args.ExtensionName}]. Message = [{e.Message}] Stacktrace = [{e.StackTrace}]");
		}

		return Task.CompletedTask;
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
	/// Verifies that every "file:" library dependency in the extension's package.json still resolves to
	/// the library's real location. If a library moved (it is still present in the manifest but the
	/// recorded path is wrong/broken), the path is auto-repaired. If a referenced library can't be found
	/// anywhere, a comprehensive exception is thrown so the user knows exactly what to fix.
	/// </summary>
	public static void ValidateAndRepairLibraryDependencies(PortalExtensionDef extension, BeamoLocalManifest manifest)
	{
		var packagePath = extension.AbsolutePackageJsonPath;
		if (!File.Exists(packagePath))
		{
			return;
		}

		var libsByName = manifest.ServiceDefinitions
			.Where(s => s.Protocol == BeamoProtocolType.PortalExtensionLib && s.PortalExtensionLibDefinition != null)
			.Select(s => s.PortalExtensionLibDefinition)
			.ToDictionary(l => l.Name, l => l, StringComparer.Ordinal);

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

			if (libsByName.TryGetValue(depName, out var lib))
			{
				var expectedAbs = Path.GetFullPath(lib.AbsolutePath);
				var alreadyCorrect = string.Equals(
					resolvedAbs.TrimEnd(Path.DirectorySeparatorChar),
					expectedAbs.TrimEnd(Path.DirectorySeparatorChar),
					StringComparison.Ordinal) && Directory.Exists(resolvedAbs);

				if (!alreadyCorrect)
				{
					var repaired = ComputeFileSpecifier(extension.AbsolutePath, lib.AbsolutePath);
					Log.Warning($"Repairing library dependency [{depName}] in extension [{extension.Name}]: [{value}] -> [{repaired}]");
					dependencies[depName] = repaired;
					changed = true;
				}
			}
			else if (!Directory.Exists(resolvedAbs))
			{
				// References a library that is neither in the manifest nor present on disk at the recorded path.
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
}
