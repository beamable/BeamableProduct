using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cli.Services.Bundles;

/// <summary>
/// In-memory model of an authored bundle config file (<c>&lt;bundle-name&gt;.beam.bundle.json</c>).
/// Declares one bundle: the beamoIds of its components and optional bundle dependencies. The bundle's
/// (short) name is the file name itself — it is not stored inside the file. The namespace is never
/// authored either; it is the customer's cid alias, derived at runtime (see <see cref="BundleNamespace"/>).
/// See <c>DesignDocs/infra/beamo-manifest/beamo-manifest-redesign.md</c> (Workspace organization).
/// </summary>
public class BundleConfigFile
{
	/// <summary>Short bundle name, derived from the file name (not serialized).</summary>
	[JsonIgnore] public string name;

	/// <summary>
	/// The bundle's deploy scope: <c>"realm"</c> or <c>"zone"</c>. Written by <c>bundles new</c> from the
	/// components' scope. Null/empty in files authored before this field existed, treated as realm.
	/// </summary>
	public string scope;

	public List<string> components = new List<string>();

	/// <summary>Bundle name to the range of its releases this bundle depends on.</summary>
	public Dictionary<string, BundleDependencyConfig> bundleDependencies = new Dictionary<string, BundleDependencyConfig>();

	/// <summary>Absolute path this config was loaded from (not serialized).</summary>
	[JsonIgnore] public string filePath;

	/// <summary>True when the bundle declares zone scope. A missing/empty scope is treated as realm.</summary>
	[JsonIgnore] public bool IsZoneScoped =>
		string.Equals(scope?.Trim(), BundleWorkspace.SCOPE_ZONE, StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// An inclusive range of another bundle's release versions. A null <see cref="max"/> means the
/// range has no upper bound.
/// </summary>
public class BundleDependencyConfig
{
	public long min;
	public long? max;

	public string Format() => FormatRange(min, max);

	/// <summary>"1+" for an open range, "1-5" for a closed one.</summary>
	public static string FormatRange(long min, long? max) =>
		max.HasValue ? $"{min}-{max.Value}" : $"{min}+";
}

/// <summary>
/// Discovers and validates authored bundle config files across the workspace.
/// </summary>
public static class BundleWorkspace
{
	public const string BUNDLE_FILE_SUFFIX = ".beam.bundle.json";

	public const string SCOPE_REALM = "realm";
	public const string SCOPE_ZONE = "zone";

	/// <summary>
	/// True when a scope string (e.g. a catalog bundle's <c>scope</c> field) is zone. A null, empty, or
	/// any non-"zone" value is treated as realm.
	/// </summary>
	public static bool IsZoneScope(string scope) =>
		string.Equals(scope?.Trim(), SCOPE_ZONE, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// Deserialize strictly: an unknown property (at any level) is an error, which is how a typo'd key
	/// like <c>component</c> is rejected instead of silently ignored.
	/// </summary>
	private static readonly JsonSerializerSettings StrictBundleSettings = new JsonSerializerSettings
	{
		MissingMemberHandling = MissingMemberHandling.Error,
	};

	private static readonly string[] IgnoredDirectorySegments =
	{
		"bin", "obj", "node_modules", ".git", ".beamable"
	};

	/// <summary>
	/// Validate a short bundle name (a file name stem, and a path segment in catalog routes).
	/// </summary>
	public static void ValidateName(string name)
	{
		if (string.IsNullOrWhiteSpace(name))
			throw new CliException("Bundle name is required.");
		if (name.Contains('/') || name.Contains('\\') || name.Contains('@'))
			throw new CliException($"Bundle name=[{name}] must be a plain name without '/', '\\' or '@'. To target another customer's namespace, qualify the whole reference as @<namespace>/<bundle-name>; otherwise the namespace is derived from your customer alias.");
		if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
			throw new CliException($"Bundle name=[{name}] contains characters that are not valid in a file name.");
	}

	/// <summary>
	/// Split a fully-qualified bundle name (<c>&lt;namespace&gt;/&lt;bundle-name&gt;</c>, the form stored in
	/// <c>manifest.beam.json</c> reference keys) into its namespace and short name.
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
			var config = ParseBundleFile(SafeRelative(root, file), ConfigService.LockedRead(file));
			config.filePath = file;
			config.name = Path.GetFileName(file).Substring(0, Path.GetFileName(file).Length - BUNDLE_FILE_SUFFIX.Length);
			results.Add(config);
		}

		return results;
	}

	/// <summary>
	/// Parse and validate one bundle file's text. First deserializes into the strict <see cref="BundleFileDto"/>
	/// wire model — a malformed JSON, a non-object root, a wrong-typed field, or an unknown property surfaces
	/// as a deserialization failure — then validates the resulting field <em>values</em> (scope value, empty
	/// or duplicate components, dependency ranges). Throws <see cref="CliException"/> naming
	/// <paramref name="displayPath"/> on any failure. Pure: it does not read the disk and does not resolve
	/// component scope (that needs the manifest — see <see cref="ValidateComponentScope"/>).
	/// </summary>
	public static BundleConfigFile ParseBundleFile(string displayPath, string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			throw new CliException($"Bundle file=[{displayPath}] is empty; it must contain a JSON object.");

		BundleFileDto dto;
		try
		{
			dto = JsonConvert.DeserializeObject<BundleFileDto>(text, StrictBundleSettings);
		}
		catch (JsonException e)
		{
			throw new CliException($"Bundle file=[{displayPath}] is not a valid bundle file: {e.Message}");
		}

		if (dto == null)
			throw new CliException($"Bundle file=[{displayPath}] must contain a JSON object at its root.");

		var config = new BundleConfigFile();

		// scope: must be "realm" or "zone" when present.
		if (!string.IsNullOrEmpty(dto.scope))
		{
			var scopeValue = dto.scope.Trim();
			if (!string.Equals(scopeValue, SCOPE_REALM, StringComparison.OrdinalIgnoreCase)
			    && !string.Equals(scopeValue, SCOPE_ZONE, StringComparison.OrdinalIgnoreCase))
				throw new CliException($"Bundle file=[{displayPath}] property [scope] must be \"{SCOPE_REALM}\" or \"{SCOPE_ZONE}\", but was [{scopeValue}].");
			config.scope = scopeValue.ToLowerInvariant();
		}

		// components: each a non-empty string, no duplicates.
		if (dto.components != null)
		{
			var seen = new HashSet<string>();
			for (var i = 0; i < dto.components.Count; i++)
			{
				var value = dto.components[i];
				if (string.IsNullOrWhiteSpace(value))
					throw new CliException($"Bundle file=[{displayPath}] property [components][{i}] must be a non-empty string.");
				if (!seen.Add(value))
					throw new CliException($"Bundle file=[{displayPath}] lists component [{value}] more than once.");
				config.components.Add(value);
			}
		}

		// bundleDependencies: each range has min >= 0 and (when present) max >= min.
		if (dto.bundleDependencies != null)
		{
			foreach (var kvp in dto.bundleDependencies)
			{
				if (string.IsNullOrWhiteSpace(kvp.Key))
					throw new CliException($"Bundle file=[{displayPath}] property [bundleDependencies] has an empty dependency name.");
				if (kvp.Value == null)
					throw new CliException($"Bundle file=[{displayPath}] dependency [{kvp.Key}] must be a JSON object with 'min' and optional 'max'.");

				var min = kvp.Value.min ?? 0;
				if (min < 0)
					throw new CliException($"Bundle file=[{displayPath}] dependency [{kvp.Key}] property [min] must be >= 0.");
				if (kvp.Value.max.HasValue && kvp.Value.max.Value < min)
					throw new CliException($"Bundle file=[{displayPath}] dependency [{kvp.Key}] range is invalid: max [{kvp.Value.max.Value}] < min [{min}].");

				config.bundleDependencies[kvp.Key] = new BundleDependencyConfig { min = min, max = kvp.Value.max };
			}
		}

		return config;
	}

	/// <summary>Strict wire model for a <c>*.beam.bundle.json</c>. Value validation happens in ParseBundleFile.</summary>
	private class BundleFileDto
	{
		public string scope;
		public List<string> components;
		public Dictionary<string, BundleDepRangeDto> bundleDependencies;
	}

	private class BundleDepRangeDto
	{
		public long? min;
		public long? max;
	}

	/// <summary>
	/// Validate that a bundle is single-scope (all components realm, or all zone — never mixed) and, when
	/// the bundle declares a <see cref="BundleConfigFile.scope"/>, that the declaration matches the
	/// components' actual scope. Requires the loaded <see cref="BeamoLocalManifest"/> because scope lives
	/// on each component's project, not in the bundle file. Components absent from the manifest are left to
	/// existence validation (<c>BundleBuild.ValidateComponentsExist</c>). Throws <see cref="CliException"/>.
	/// </summary>
	public static void ValidateComponentScope(BeamoLocalManifest manifest, BundleConfigFile bundle)
	{
		if (manifest?.ServiceDefinitions == null)
			return;

		var scopeById = new Dictionary<string, bool>();
		foreach (var def in manifest.ServiceDefinitions)
			scopeById[def.BeamoId] = def.IsZoneScoped;

		var realm = new List<string>();
		var zone = new List<string>();
		foreach (var component in bundle.components)
		{
			if (!scopeById.TryGetValue(component, out var isZone))
				continue;
			if (isZone)
				zone.Add(component);
			else
				realm.Add(component);
		}

		var fileLabel = bundle.filePath != null ? Path.GetFileName(bundle.filePath) : bundle.name;

		if (realm.Count > 0 && zone.Count > 0)
			throw new CliException(
				$"Bundle=[{bundle.name}] mixes realm-scoped and zone-scoped components; a bundle must be a single scope.\n" +
				$" - realm: {string.Join(", ", realm)}\n" +
				$" - zone: {string.Join(", ", zone)}\n" +
				$"Fix {fileLabel}: split into separate bundles, or make the components' scope consistent (<BeamServiceScope> for services/storages, package.json beamable.serviceScope for portal extensions).");

		if (string.IsNullOrEmpty(bundle.scope))
			return;

		if (bundle.IsZoneScoped && realm.Count > 0)
			throw new CliException(
				$"Bundle=[{bundle.name}] declares scope=[{SCOPE_ZONE}] but includes realm-scoped components: {string.Join(", ", realm)}. Fix {fileLabel} or the components' scope.");
		if (!bundle.IsZoneScoped && zone.Count > 0)
			throw new CliException(
				$"Bundle=[{bundle.name}] declares scope=[{SCOPE_REALM}] but includes zone-scoped components: {string.Join(", ", zone)}. Fix {fileLabel} or the components' scope.");
	}

	private static string SafeRelative(string root, string file)
	{
		try
		{
			return Path.GetRelativePath(root, file);
		}
		catch
		{
			return Path.GetFileName(file);
		}
	}

	/// <summary>
	/// Discover bundles and validate the partitioning rules: valid unique bundle names, and each
	/// beamoId belongs to at most one bundle. Throws <see cref="CliException"/> on violation.
	/// </summary>
	public static List<BundleConfigFile> DiscoverAndValidate(ConfigService configService)
	{
		var bundles = Discover(configService);

		var seenNames = new HashSet<string>();
		var componentToBundle = new Dictionary<string, string>();
		foreach (var bundle in bundles)
		{
			ValidateName(bundle.name);
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

	/// <summary>Find a single authored bundle by short name, or throw if it isn't declared in the workspace.</summary>
	public static BundleConfigFile Require(ConfigService configService, string bundleName)
	{
		ValidateName(bundleName);
		var match = DiscoverAndValidate(configService).FirstOrDefault(b => b.name == bundleName);
		if (match == null)
			throw new CliException($"No {bundleName}{BUNDLE_FILE_SUFFIX} file exists in this workspace.");
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
