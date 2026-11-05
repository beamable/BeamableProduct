using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace cli.Services.Bundles;

/// <summary>
/// In-memory model of an authored bundle config file (<c>&lt;bundle-name&gt;.beam.bundle.json</c>).
/// Declares one bundle: its components, the component groups that turn them on, and the bundles it depends on.
/// The bundle's (short) name is the file name itself — it is not stored inside the file. The namespace
/// is never authored either; it is the customer's cid alias, derived at runtime (see <see cref="BundleNamespace"/>).
/// A component's kind and manifest entry come from its project, and an extension's mounts from its
/// package.json; only what the project can't say is authored here.
/// </summary>
public class BundleConfigFile
{
	/// <summary>Short bundle name, derived from the file name (not serialized).</summary>
	[JsonIgnore] public string name;

	/// <summary>Component beamoId to how the bundle carries it.</summary>
	public Dictionary<string, BundleComponentConfig> components = new Dictionary<string, BundleComponentConfig>();

	/// <summary>Component group name to the components it turns on. An installation chooses which groups are on.</summary>
	public Dictionary<string, BundleComponentGroupConfig> componentGroups = new Dictionary<string, BundleComponentGroupConfig>();

	/// <summary>Bundle name (<c>@namespace/name</c>) to the range of its releases this bundle needs.</summary>
	public Dictionary<string, BundleDependencyConfig> dependencies = new Dictionary<string, BundleDependencyConfig>();

	/// <summary>Absolute path this config was loaded from (not serialized).</summary>
	[JsonIgnore] public string filePath;

	/// <summary>The component beamoIds, in file order.</summary>
	[JsonIgnore] public IReadOnlyCollection<string> ComponentNames => components.Keys;
}

public class BundleComponentConfig
{
	/// <summary>
	/// <c>"realm"</c> or <c>"zone"</c>. Optional: it comes from the component's own scope
	/// (<c>BeamServiceScope</c>, or <c>beamable.serviceScope</c> for an extension), and when given it must match it.
	/// </summary>
	public string site;

	/// <summary>Whether orgs that acquire the bundle may get this component's source.</summary>
	public bool source;
}

public class BundleComponentGroupConfig
{
	public string title;
	public string description;
	public List<string> components = new List<string>();
	public bool enabledByDefault;
}

/// <summary>
/// An inclusive range of another bundle's release versions, and whether the bundle needs it (hard) or
/// only works better with it (soft). A null <see cref="max"/> means the range has no upper bound.
/// </summary>
public class BundleDependencyConfig
{
	public string kind = BundleWorkspace.DEPENDENCY_HARD;
	public long min;
	public long? max;

	[JsonIgnore] public bool IsHard => string.Equals(kind, BundleWorkspace.DEPENDENCY_HARD, StringComparison.OrdinalIgnoreCase);

	public string Format() => $"{kind} {FormatRange(min, max)}";

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

	public const string SITE_REALM = "realm";
	public const string SITE_ZONE = "zone";

	public const string DEPENDENCY_HARD = "hard";
	public const string DEPENDENCY_SOFT = "soft";

	/// <summary>
	/// Deserialize strictly: an unknown property (at any level) is an error, which is how a typo'd key
	/// like <c>component</c>, or a file in the old <c>scope</c>/<c>bundleDependencies</c> shape, is rejected
	/// instead of silently ignored.
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
	/// Validate a short bundle name (a file name stem).
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
	/// as a deserialization failure — then validates the values the file alone can check: sites, component
	/// groups covering every component, and dependency kinds and ranges. Throws <see cref="CliException"/> naming
	/// <paramref name="displayPath"/> on any failure. Pure: it does not read the disk; the rules that need the
	/// projects (kinds, sites, storages) are in <see cref="ValidateAgainstProjects"/>.
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

		foreach (var (component, value) in dto.components ?? new Dictionary<string, BundleComponentDto>())
		{
			if (string.IsNullOrWhiteSpace(component))
				throw new CliException($"Bundle file=[{displayPath}] property [components] has an empty component name.");
			var site = value?.site?.Trim();
			if (!string.IsNullOrEmpty(site) && !IsSite(site))
				throw new CliException($"Bundle file=[{displayPath}] component [{component}] property [site] must be \"{SITE_REALM}\" or \"{SITE_ZONE}\", but was [{site}].");
			config.components[component] = new BundleComponentConfig
			{
				site = string.IsNullOrEmpty(site) ? null : site.ToLowerInvariant(),
				source = value?.source ?? false,
			};
		}

		foreach (var (group, value) in dto.componentGroups ?? new Dictionary<string, BundleComponentGroupDto>())
		{
			if (string.IsNullOrWhiteSpace(group))
				throw new CliException($"Bundle file=[{displayPath}] property [componentGroups] has an empty group name.");
			if (value == null)
				throw new CliException($"Bundle file=[{displayPath}] component group [{group}] must be a JSON object.");

			var components = value.components ?? new List<string>();
			if (components.Count == 0)
				throw new CliException($"Bundle file=[{displayPath}] component group [{group}] lists no components.");
			var unknown = components.Where(c => !config.components.ContainsKey(c)).Distinct().ToList();
			if (unknown.Count > 0)
				throw new CliException($"Bundle file=[{displayPath}] component group [{group}] lists components that aren't in [components]: {string.Join(", ", unknown)}.");
			var repeated = components.GroupBy(c => c).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
			if (repeated.Count > 0)
				throw new CliException($"Bundle file=[{displayPath}] component group [{group}] lists [{string.Join(", ", repeated)}] more than once.");

			config.componentGroups[group] = new BundleComponentGroupConfig
			{
				title = string.IsNullOrWhiteSpace(value.title) ? group : value.title,
				description = value.description ?? "",
				components = components,
				enabledByDefault = value.enabledByDefault ?? false,
			};
		}

		if (config.components.Count > 0)
		{
			if (config.componentGroups.Count == 0)
				throw new CliException($"Bundle file=[{displayPath}] has components but no [componentGroups]. Every component must be in at least one group.");
			var orphans = config.components.Keys
				.Where(c => !config.componentGroups.Values.Any(g => g.components.Contains(c)))
				.ToList();
			if (orphans.Count > 0)
				throw new CliException($"Bundle file=[{displayPath}]: every component must be in a component group; these are in none: {string.Join(", ", orphans)}.");
		}

		foreach (var (dependency, value) in dto.dependencies ?? new Dictionary<string, BundleDependencyDto>())
		{
			if (string.IsNullOrWhiteSpace(dependency))
				throw new CliException($"Bundle file=[{displayPath}] property [dependencies] has an empty bundle name.");
			if (!dependency.StartsWith("@") || dependency.IndexOf('/') <= 1 || dependency.EndsWith("/"))
				throw new CliException($"Bundle file=[{displayPath}] dependency [{dependency}] must be a full bundle name, @<namespace>/<bundle-name>.");
			if (value == null)
				throw new CliException($"Bundle file=[{displayPath}] dependency [{dependency}] must be a JSON object with 'kind', 'min' and optional 'max'.");

			var kind = value.kind?.Trim().ToLowerInvariant();
			if (kind != DEPENDENCY_HARD && kind != DEPENDENCY_SOFT)
				throw new CliException($"Bundle file=[{displayPath}] dependency [{dependency}] property [kind] must be \"{DEPENDENCY_HARD}\" or \"{DEPENDENCY_SOFT}\", but was [{value.kind}].");
			var min = value.min ?? 0;
			if (min < 0)
				throw new CliException($"Bundle file=[{displayPath}] dependency [{dependency}] property [min] must be >= 0.");
			if (value.max.HasValue && value.max.Value < min)
				throw new CliException($"Bundle file=[{displayPath}] dependency [{dependency}] range is invalid: max [{value.max.Value}] < min [{min}].");

			config.dependencies[dependency] = new BundleDependencyConfig { kind = kind, min = min, max = value.max };
		}

		return config;
	}

	/// <summary>Strict wire model for a <c>*.beam.bundle.json</c>. Value validation happens in ParseBundleFile.</summary>
	private class BundleFileDto
	{
		public Dictionary<string, BundleComponentDto> components;
		public Dictionary<string, BundleComponentGroupDto> componentGroups;
		public Dictionary<string, BundleDependencyDto> dependencies;
	}

	private class BundleComponentDto
	{
		public string site;
		public bool? source;
	}

	private class BundleComponentGroupDto
	{
		public string title;
		public string description;
		public List<string> components;
		public bool? enabledByDefault;
	}

	private class BundleDependencyDto
	{
		public string kind;
		public long? min;
		public long? max;
	}

	private static bool IsSite(string value) =>
		string.Equals(value, SITE_REALM, StringComparison.OrdinalIgnoreCase)
		|| string.Equals(value, SITE_ZONE, StringComparison.OrdinalIgnoreCase);

	/// <summary>
	/// The rules that need the workspace's projects: every component is a local service, storage or portal
	/// extension (beamoIds are case-sensitive, so a near miss is suggested); a declared site matches the
	/// component's own scope; every component group that has a service has the storages it uses (a hard dependency);
	/// and a service whose source is available makes its storages' source available too, since the service
	/// doesn't build without them. Throws <see cref="CliException"/> listing every problem.
	/// </summary>
	public static void ValidateAgainstProjects(BeamoLocalManifest manifest, BundleConfigFile bundle)
	{
		var definitions = (manifest?.ServiceDefinitions ?? new List<BeamoServiceDefinition>())
			.Where(d => d.IsLocal)
			.ToList();
		var byId = definitions.ToDictionary(d => d.BeamoId);
		var problems = new List<string>();

		foreach (var (component, config) in bundle.components)
		{
			if (!byId.TryGetValue(component, out var definition))
			{
				var nearMiss = definitions.FirstOrDefault(d => string.Equals(d.BeamoId, component, StringComparison.OrdinalIgnoreCase));
				problems.Add(nearMiss != null
					? $"[{component}] does not match any local component exactly; beamoIds are case-sensitive. Did you mean [{nearMiss.BeamoId}]?"
					: $"[{component}] is not a local service, storage or portal extension.");
				continue;
			}

			if (definition.Protocol != BeamoProtocolType.HttpMicroservice
			    && definition.Protocol != BeamoProtocolType.EmbeddedMongoDb
			    && definition.Protocol != BeamoProtocolType.PortalExtension)
			{
				problems.Add($"[{component}] is a {definition.Protocol}; a bundle carries services, storages and portal extensions.");
				continue;
			}

			var actualSite = definition.IsZoneScoped ? SITE_ZONE : SITE_REALM;
			if (config.site != null && config.site != actualSite)
				problems.Add($"[{component}] declares site [{config.site}] but the project is {actualSite}-scoped. Remove [site] or change the project's scope.");
		}

		if (problems.Count == 0)
		{
			foreach (var (component, _) in bundle.components)
			{
				var definition = byId[component];
				if (definition.Protocol != BeamoProtocolType.HttpMicroservice)
					continue;

				var storages = (manifest.HttpMicroserviceLocalProtocols.TryGetValue(component, out var http)
						? http.StorageDependencyBeamIds
						: new List<string>())
					.Where(d => byId.TryGetValue(d, out var dep) && dep.Protocol == BeamoProtocolType.EmbeddedMongoDb)
					.ToList();

				foreach (var storage in storages)
				{
					if (!bundle.components.TryGetValue(storage, out var storageConfig))
					{
						problems.Add($"Service [{component}] uses storage [{storage}], which isn't in the bundle.");
						continue;
					}

					foreach (var (group, groupConfig) in bundle.componentGroups)
					{
						if (groupConfig.components.Contains(component) && !groupConfig.components.Contains(storage))
							problems.Add($"Component group [{group}] has service [{component}] but not the storage it uses, [{storage}].");
					}

					if (byId[storage].IsZoneScoped != definition.IsZoneScoped)
						problems.Add($"Service [{component}] and its storage [{storage}] must run on the same site.");

					if (bundle.components[component].source && !storageConfig.source)
						problems.Add($"Service [{component}] makes its source available, so its storage [{storage}] must too: the service doesn't build without it.");
				}
			}
		}

		if (problems.Count > 0)
		{
			var fileLabel = bundle.filePath != null ? Path.GetFileName(bundle.filePath) : bundle.name;
			throw new CliException($"Bundle=[{bundle.name}] doesn't match its projects. Fix {fileLabel}:\n - " + string.Join("\n - ", problems));
		}
	}

	/// <summary>A component's site: the declared one, else its project's scope.</summary>
	public static string SiteOf(BeamoLocalManifest manifest, BundleConfigFile bundle, string component)
	{
		var declared = bundle.components.TryGetValue(component, out var config) ? config.site : null;
		if (declared != null) return declared;
		var definition = manifest?.ServiceDefinitions?.FirstOrDefault(d => d.BeamoId == component);
		return definition?.IsZoneScoped == true ? SITE_ZONE : SITE_REALM;
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

			foreach (var component in bundle.ComponentNames)
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
