using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cli.Services.Bundles;

/// <summary>
/// In-memory model of an authored bundle config file (<c>*.beam.bundle.json</c>). Declares one
/// bundle: its namespaced name, the beamoIds of its components, and optional peer dependencies.
/// See <c>DesignDocs/infra/beamo-manifest/beamo-manifest-redesign.md</c> (Workspace organization).
/// </summary>
public class BundleConfigFile
{
	public string name;
	public List<string> components = new List<string>();
	public Dictionary<string, BundlePeerDependencyConfig> peerDependencies = new Dictionary<string, BundlePeerDependencyConfig>();

	/// <summary>Absolute path this config was loaded from (not serialized).</summary>
	[JsonIgnore] public string filePath;
}

public class BundlePeerDependencyConfig
{
	public string type;
}

/// <summary>
/// Discovers and validates authored bundle config files across the workspace, and provides the
/// bundle-name ↔ (namespace, name) split the generated API requires.
/// </summary>
public static class BundleWorkspace
{
	public const string BUNDLE_FILE_SUFFIX = ".beam.bundle.json";

	private static readonly string[] IgnoredDirectorySegments =
	{
		"bin", "obj", "node_modules", ".git", ".beamable"
	};

	/// <summary>
	/// Split a namespaced bundle name (e.g. <c>&lt;namespace&gt;/&lt;bundle-name&gt;</c>) into its namespace
	/// (the <c>&lt;namespace&gt;</c> segment) and short name (the <c>&lt;bundle-name&gt;</c> segment) for the
	/// generated <c>(bundleName, ns)</c> API parameters.
	/// </summary>
	public static (string ns, string name) SplitBundleName(string fullName)
	{
		if (string.IsNullOrWhiteSpace(fullName))
			throw new CliException($"Bundle name is required and must be namespaced, e.g. <namespace>/<bundle-name>.");

		var slash = fullName.IndexOf('/');
		if (slash <= 0 || slash == fullName.Length - 1)
			throw new CliException($"Bundle name=[{fullName}] must be namespaced as <namespace>/<bundle-name>.");

		return (fullName.Substring(0, slash), fullName.Substring(slash + 1));
	}

	/// <summary>Discover every <c>*.beam.bundle.json</c> under the workspace root.</summary>
	public static List<BundleConfigFile> Discover(ConfigService configService)
	{
		var root = configService.BeamableWorkspace;
		if (string.IsNullOrEmpty(root) || !Directory.Exists(root))
			return new List<BundleConfigFile>();

		var results = new List<BundleConfigFile>();
		foreach (var file in EnumerateBundleFiles(root))
		{
			var obj = JsonConvert.DeserializeObject<JObject>(ConfigService.LockedRead(file)) ?? new JObject();
			var config = new BundleConfigFile
			{
				filePath = file,
				name = obj.Value<string>("name"),
				components = (obj["components"] as JArray)?.Select(t => t.Value<string>()).ToList() ?? new List<string>(),
				peerDependencies = new Dictionary<string, BundlePeerDependencyConfig>()
			};
			if (obj["peerDependencies"] is JObject peers)
			{
				foreach (var kvp in peers)
				{
					config.peerDependencies[kvp.Key] = new BundlePeerDependencyConfig
					{
						type = (kvp.Value as JObject)?.Value<string>("type")
					};
				}
			}

			results.Add(config);
		}

		return results;
	}

	/// <summary>
	/// Discover bundles and validate the partitioning rules: unique bundle names, and each beamoId
	/// belongs to at most one bundle. Throws <see cref="CliException"/> on violation.
	/// </summary>
	public static List<BundleConfigFile> DiscoverAndValidate(ConfigService configService)
	{
		var bundles = Discover(configService);

		var seenNames = new HashSet<string>();
		var componentToBundle = new Dictionary<string, string>();
		foreach (var bundle in bundles)
		{
			if (string.IsNullOrWhiteSpace(bundle.name))
				throw new CliException($"Bundle config file=[{bundle.filePath}] is missing a 'name'.");
			if (!seenNames.Add(bundle.name))
				throw new CliException($"Bundle name=[{bundle.name}] is declared by more than one *{BUNDLE_FILE_SUFFIX} file.");

			foreach (var component in bundle.components)
			{
				if (componentToBundle.TryGetValue(component, out var otherBundle))
					throw new CliException($"Component=[{component}] belongs to more than one bundle ([{otherBundle}] and [{bundle.name}]). A beamoId can be in at most one bundle.");
				componentToBundle[component] = bundle.name;
			}
		}

		return bundles;
	}

	/// <summary>Find a single authored bundle by name, or throw if it isn't declared in the workspace.</summary>
	public static BundleConfigFile Require(ConfigService configService, string bundleName)
	{
		var match = DiscoverAndValidate(configService).FirstOrDefault(b => b.name == bundleName);
		if (match == null)
			throw new CliException($"No {BUNDLE_FILE_SUFFIX} file declares a bundle named [{bundleName}] in this workspace.");
		return match;
	}

	private static IEnumerable<string> EnumerateBundleFiles(string root)
	{
		var pending = new Stack<string>();
		pending.Push(root);
		while (pending.Count > 0)
		{
			var dir = pending.Pop();
			string[] entries;
			try
			{
				entries = Directory.GetFiles(dir, "*" + BUNDLE_FILE_SUFFIX);
			}
			catch (UnauthorizedAccessException)
			{
				continue;
			}

			foreach (var file in entries)
				yield return file;

			foreach (var sub in Directory.GetDirectories(dir))
			{
				var name = Path.GetFileName(sub);
				if (IgnoredDirectorySegments.Contains(name, StringComparer.OrdinalIgnoreCase))
					continue;
				pending.Push(sub);
			}
		}
	}
}
