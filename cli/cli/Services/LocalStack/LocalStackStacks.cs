using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace cli.Services.LocalStack;

/// <summary>
/// A stack: what <c>beam local run &lt;stack&gt;[:variant…]</c> starts. It is a named, described selection of steps
/// from the settings' step catalogue, made by tag or exact step name. A stack never holds a path; its steps refer to
/// folders through variables, so the same stack works on every machine.
/// </summary>
public class LocalStackStackDef
{
	/// <summary>Shown by a bare <c>beam local run</c>. Required for a stack first defined in the shared folder.</summary>
	public string description;

	/// <summary>Another stack, in any layer, that this one starts from.</summary>
	public string extends;

	/// <summary>
	/// The backend the stack runs against: <c>local</c>, or a target from the settings (<c>dev</c>, <c>staging</c>,
	/// ...). Inherited through <see cref="extends"/>; unset means <c>local</c>. A variant named after a target
	/// switches it for one run: <c>portal-extensions:local</c>.
	/// </summary>
	public string target;

	/// <summary>Tags or exact step names added to the selection.</summary>
	public List<string> include;

	/// <summary>Tags or exact step names taken out of the selection.</summary>
	public List<string> exclude;

	/// <summary>
	/// Narrows the extension steps (portal extensions, microservices and service groups) to these ids. Replaces an
	/// inherited list.
	/// </summary>
	public List<string> extensions;

	/// <summary>Team variables the stack uses, with a description and an optional default. A value in the settings
	/// file wins over the default.</summary>
	public Dictionary<string, LocalStackStackVariable> variables;

	/// <summary>Steps the catalogue doesn't have, in the catalogue's format. A step with a catalogue step's name
	/// replaces that step for this stack.</summary>
	public Dictionary<string, LocalStackStep> steps;

	/// <summary>Variants by name, run as <c>&lt;stack&gt;:&lt;variant&gt;</c>.</summary>
	public Dictionary<string, LocalStackVariantDef> variants;
}

/// <summary>A variable a stack declares, so the run can ask for it when the settings file has no value.</summary>
public class LocalStackStackVariable
{
	public string description;

	[JsonProperty("default")]
	public string defaultValue;
}

/// <summary>
/// A variant adds steps around its stack's own steps. Overriding a step is a <see cref="remove"/> plus a
/// <see cref="before"/> or <see cref="after"/>.
/// </summary>
public class LocalStackVariantDef
{
	public string description;

	/// <summary>Tags or step names that run before the stack's own steps.</summary>
	public List<string> before;

	/// <summary>Tags or step names that start once the stack's own steps are ready.</summary>
	public List<string> after;

	/// <summary>Tags or step names of the stack's own steps that this variant turns off.</summary>
	public List<string> remove;

	/// <summary>Runs every build step the stack selects, even when its output is already there.</summary>
	public bool forceBuild;

	/// <summary>Switches the backend the stack runs against (see <see cref="LocalStackStackDef.target"/>).</summary>
	public string target;
}

/// <summary>Where a stack definition comes from. Higher layers win: local over shared over built-in.</summary>
public enum LocalStackLayer
{
	BuiltIn,
	Shared,
	Local
}

/// <summary>A stack name that can't be resolved, or a stack file that can't be used.</summary>
public class LocalStackStackException : InvalidOperationException
{
	public LocalStackStackException(string message) : base(message)
	{
	}
}

/// <summary>A resolved <c>&lt;stack&gt;[:variant…]</c>: the steps to run, in order.</summary>
public class LocalStackResolvedStack
{
	public string name;
	public string description;

	/// <summary>The steps to run, in order: every variant's <c>before</c>, the stack's own steps, every <c>after</c>.</summary>
	public List<LocalStackStep> steps = new List<LocalStackStep>();

	/// <summary>
	/// Build steps the name asks for outright: named exactly, selected by the <c>build</c> or <c>web-registry</c>
	/// tag, or added by a variant. They run even when their output is there. Other build steps only run when their
	/// output is missing, or with <see cref="forceBuild"/>.
	/// </summary>
	public HashSet<LocalStackStep> requested = new HashSet<LocalStackStep>();

	/// <summary>Set by a variant such as <c>:build</c>: every selected build step runs.</summary>
	public bool forceBuild;

	/// <summary>The backend it runs against: the last variant naming one, else the stack's, else <c>local</c>.</summary>
	public string target = LocalStackTargets.Local;

	/// <summary>The team variables the stack declares, by name.</summary>
	public Dictionary<string, LocalStackStackVariable> variables = new Dictionary<string, LocalStackStackVariable>(StringComparer.Ordinal);
}

/// <summary>One stack in the listing a bare <c>beam local run</c> prints.</summary>
public class LocalStackStackListing
{
	public string name;
	public string description;

	/// <summary>The lowest layer that defines the stack; the listing groups stacks by it.</summary>
	public LocalStackLayer layer;

	/// <summary>Higher layers that change the stack.</summary>
	public List<LocalStackLayer> changedBy = new List<LocalStackLayer>();

	/// <summary>The backend it runs against unless a variant names another.</summary>
	public string target = LocalStackTargets.Local;

	public List<(string name, string description, LocalStackLayer layer)> variants = new List<(string, string, LocalStackLayer)>();
}

/// <summary>
/// Every stack this workspace can run, from three layers merged by name: <c>.beamable/local/stacks/</c> (personal,
/// not committed), <c>.beamable/shared/stacks/</c> (committed) and the stacks built into the CLI. Fields set in a
/// higher layer win, and variants, variables and steps are merged by name, so a personal variant can be added to a
/// shared or built-in stack without copying it.
/// </summary>
public class LocalStackStacks
{
	/// <summary>The folder name, under <c>.beamable/local</c> or <c>.beamable/shared</c>, that holds stack files.</summary>
	public const string StacksFolder = "stacks";

	/// <summary>The committed folder under <c>.beamable</c>.</summary>
	public const string SharedFolder = "shared";

	/// <summary>The tags a step can carry. A list item that is one of these never fails, even when no step has it yet.</summary>
	public static readonly string[] KnownTags =
	{
		"docker", "csharp", "scala-core", "scala-csharp", "scala", "portal", MicroservicesTag, ExtensionsTag, "web-registry", "observability", "build", "native", "custom"
	};

	public const string MicroservicesTag = "microservices";
	public const string ExtensionsTag = "extensions";

	/// <summary>
	/// The tags of a step running this workspace's services, from its name: a microservice is <c>microservices</c>, a
	/// portal extension <c>extensions</c>, and a service group, which runs both, has both. Empty for other steps.
	/// </summary>
	public static List<string> ServiceTagsFor(string stepName)
	{
		bool Starts(string prefix) => (stepName ?? string.Empty).Trim().StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
		if (Starts(LocalStackTemplate.MicroservicePrefix)) return new List<string> { MicroservicesTag };
		if (Starts(LocalStackTemplate.ExtensionPrefix)) return new List<string> { ExtensionsTag };
		if (Starts(LocalStackTemplate.GroupPrefix)) return new List<string> { MicroservicesTag, ExtensionsTag };
		return new List<string>();
	}

	/// <summary>True for a tag of this workspace's services: <c>microservices</c> or <c>extensions</c>.</summary>
	public static bool IsServiceTag(string tag) =>
		string.Equals(tag, MicroservicesTag, StringComparison.OrdinalIgnoreCase) || string.Equals(tag, ExtensionsTag, StringComparison.OrdinalIgnoreCase);

	/// <summary>What each tag selects, for the pickers in <c>beam local create</c>.</summary>
	public static readonly IReadOnlyDictionary<string, string> TagDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
	{
		["docker"] = "API dependencies and Caddy (Mongo, Redis, ActiveMQ and so on)",
		["csharp"] = "C# gateway and C# runtimes",
		["scala-core"] = "the Scala services needed to log in",
		["scala-csharp"] = "the Scala services the C# hosts call (content, messaging, notification, session, stats)",
		["scala"] = "every Scala service",
		["portal"] = "portal dependencies, Vite cache clear and the portal frontend",
		[MicroservicesTag] = "this workspace's microservices (a service group has this tag and extensions)",
		[ExtensionsTag] = "this workspace's portal extensions (a service group has this tag and microservices)",
		["web-registry"] = "only for changing the Web SDK (@beamable/sdk) or the portal toolkit (@beamable/portal-toolkit) "
		                   + "themselves: builds them from BeamableProduct, publishes them to a local npm registry and installs "
		                   + "them in the extensions, so the portal runs your changes instead of the released packages. Not "
		                   + "needed to work on the portal, extensions or services",
		["observability"] = "log forwarding to Grafana",
		["build"] = "compile steps",
		["native"] = "native libraries and the React Native sample",
		["custom"] = "steps added with beam local setup --add-step",
	};

	/// <summary>True when <paramref name="name"/> can name a stack or a variant.</summary>
	public static bool IsValidName(string name) => NamePattern.IsMatch(name ?? string.Empty);

	/// <summary>Tags that ask for their build steps outright, so those run without <c>:build</c>.</summary>
	private static readonly HashSet<string> RequestingTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "build", "web-registry" };

	private static readonly Regex NamePattern = new Regex("^[A-Za-z0-9][A-Za-z0-9._-]*$", RegexOptions.Compiled);

	private const string BuiltInResourceName = "cli.Services.LocalStack.BuiltInStacks.json";

	private class Entry
	{
		public LocalStackLayer layer;
		public string file;
		public LocalStackStackDef def;
	}

	private class BuiltInFile
	{
		public Dictionary<string, List<string>> tags;
		public Dictionary<string, LocalStackVariantDef> variants;
		public Dictionary<string, LocalStackStackDef> stacks;
	}

	private readonly Dictionary<string, List<Entry>> _stacks = new Dictionary<string, List<Entry>>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, LocalStackVariantDef> _builtInVariants = new Dictionary<string, LocalStackVariantDef>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, List<string>> _tagGroups = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The variants every stack has, by name.</summary>
	public IReadOnlyDictionary<string, LocalStackVariantDef> BuiltInVariants => _builtInVariants;

	/// <summary>The stacks folder of a layer: <c>&lt;root&gt;/.beamable/local/stacks</c> or <c>.../shared/stacks</c>.</summary>
	public static string StacksDir(string workspaceRoot, LocalStackLayer layer) => layer switch
	{
		LocalStackLayer.Local => Path.Combine(workspaceRoot, ConfigService.CFG_FOLDER, LocalStackConfigIO.SettingsFolder, StacksFolder),
		LocalStackLayer.Shared => Path.Combine(workspaceRoot, ConfigService.CFG_FOLDER, SharedFolder, StacksFolder),
		_ => null
	};

	/// <summary>Loads the built-in stacks, then the shared and local folders of <paramref name="workspaceRoot"/>.</summary>
	public static LocalStackStacks Load(string workspaceRoot)
	{
		var set = BuiltIn();
		if (!string.IsNullOrEmpty(workspaceRoot))
		{
			set.LoadFolder(StacksDir(workspaceRoot, LocalStackLayer.Shared), LocalStackLayer.Shared);
			set.LoadFolder(StacksDir(workspaceRoot, LocalStackLayer.Local), LocalStackLayer.Local);
		}

		return set;
	}

	/// <summary>Only the stacks, variants and tags built into the CLI.</summary>
	public static LocalStackStacks BuiltIn()
	{
		using var stream = typeof(LocalStackStacks).Assembly.GetManifestResourceStream(BuiltInResourceName)
		                   ?? throw new LocalStackStackException($"The CLI is missing its built-in stacks ({BuiltInResourceName}).");
		using var reader = new StreamReader(stream);
		var file = JsonConvert.DeserializeObject<BuiltInFile>(reader.ReadToEnd()) ?? new BuiltInFile();

		var set = new LocalStackStacks();
		set._builtInVariants = new Dictionary<string, LocalStackVariantDef>(
			file.variants ?? new Dictionary<string, LocalStackVariantDef>(), StringComparer.OrdinalIgnoreCase);
		set._tagGroups = new Dictionary<string, List<string>>(
			file.tags ?? new Dictionary<string, List<string>>(), StringComparer.OrdinalIgnoreCase);
		foreach (var (name, def) in file.stacks ?? new Dictionary<string, LocalStackStackDef>())
			set.Add(name, LocalStackLayer.BuiltIn, null, def);

		return set;
	}

	/// <summary>
	/// Reads every <c>*.json</c> stack file in <paramref name="dir"/>. A file holds stacks by name; the same name in
	/// two files of one folder is an error that names both.
	/// </summary>
	public void LoadFolder(string dir, LocalStackLayer layer)
	{
		if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

		var seen = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (var file in Directory.GetFiles(dir, "*.json").OrderBy(f => f, StringComparer.OrdinalIgnoreCase))
		{
			Dictionary<string, LocalStackStackDef> defs;
			try
			{
				defs = JsonConvert.DeserializeObject<Dictionary<string, LocalStackStackDef>>(File.ReadAllText(file));
			}
			catch (JsonException e)
			{
				throw new LocalStackStackException($"{file} is not a valid stack file: {e.Message}");
			}

			foreach (var (name, def) in defs ?? new Dictionary<string, LocalStackStackDef>())
			{
				if (seen.TryGetValue(name, out var other))
					throw new LocalStackStackException(
						$"Stack '{name}' is defined twice in {dir}: {Path.GetFileName(other)} and {Path.GetFileName(file)}.");

				seen[name] = file;
				Add(name, layer, file, def ?? new LocalStackStackDef());
			}
		}
	}

	/// <summary>Adds one definition of a stack. Shared definitions must describe every stack and variant they introduce.</summary>
	public void Add(string name, LocalStackLayer layer, string file, LocalStackStackDef def)
	{
		var where = file == null ? "the built-in stacks" : file;
		if (!NamePattern.IsMatch(name ?? string.Empty))
			throw new LocalStackStackException($"'{name}' in {where} is not a valid stack name: use letters, digits, '.', '_' and '-'.");

		foreach (var variant in def.variants?.Keys ?? Enumerable.Empty<string>())
		{
			if (!NamePattern.IsMatch(variant ?? string.Empty))
				throw new LocalStackStackException($"'{name}:{variant}' in {where} is not a valid variant name: use letters, digits, '.', '_' and '-'.");
		}

		if (!_stacks.TryGetValue(name, out var entries))
			_stacks[name] = entries = new List<Entry>();

		if (layer == LocalStackLayer.Shared)
		{
			// Shared stacks are read by the whole team in the listing, so they have to say what they're for.
			if (entries.Count == 0 && string.IsNullOrWhiteSpace(def.description))
				throw new LocalStackStackException($"Stack '{name}' in {where} needs a description: shared stacks are listed for the whole team.");

			var undescribed = (def.variants ?? new Dictionary<string, LocalStackVariantDef>())
				.Where(v => string.IsNullOrWhiteSpace(v.Value?.description)).Select(v => v.Key).FirstOrDefault();
			if (undescribed != null)
				throw new LocalStackStackException($"Variant '{name}:{undescribed}' in {where} needs a description: shared variants are listed for the whole team.");
		}

		entries.Add(new Entry { layer = layer, file = file, def = def });
		entries.Sort((a, b) => a.layer.CompareTo(b.layer));
	}

	/// <summary>
	/// Resolves <paramref name="fullName"/> as if <paramref name="draft"/> were one more definition of its stack, without
	/// adding it to this set: what <c>beam local create</c> previews and checks before it writes anything. With
	/// <paramref name="replace"/>, the draft takes the place of the stack's definition in <paramref name="layer"/>, as
	/// <c>beam local edit</c> writes it.
	/// </summary>
	public LocalStackResolvedStack ResolveDraft(string fullName, LocalStackLayer layer, string file, LocalStackStackDef draft,
		LocalStackConfig config, bool replace = false)
	{
		var copy = new LocalStackStacks { _builtInVariants = _builtInVariants, _tagGroups = _tagGroups };
		foreach (var (name, entries) in _stacks)
			copy._stacks[name] = new List<Entry>(entries);

		var stackName = fullName.Split(':')[0].Trim();
		if (replace && copy._stacks.TryGetValue(stackName, out var defined))
			defined.RemoveAll(e => e.layer == layer);
		copy.Add(stackName, layer, file, draft);
		return copy.Resolve(fullName, config);
	}

	/// <summary>Every definition read from the stack folders and the built-ins, lowest layer first per stack.</summary>
	public IEnumerable<(string name, LocalStackLayer layer, string file, LocalStackStackDef def)> Definitions() =>
		_stacks.SelectMany(kv => kv.Value.Select(e => (kv.Key, e.layer, e.file, e.def)));

	/// <summary>The stacks whose <c>extends</c> names <paramref name="name"/>, in any layer.</summary>
	public List<string> ExtendedBy(string name) =>
		_stacks.Keys
			.Where(other => !string.Equals(other, name, StringComparison.OrdinalIgnoreCase)
			                && string.Equals(Merge(other).extends?.Trim(), name, StringComparison.OrdinalIgnoreCase))
			.OrderBy(other => other, StringComparer.OrdinalIgnoreCase)
			.ToList();

	/// <summary>True when any layer defines <paramref name="name"/>.</summary>
	public bool Contains(string name) => _stacks.ContainsKey(name);

	/// <summary>The files in <paramref name="layer"/> that define <paramref name="name"/>.</summary>
	public IEnumerable<string> FilesDefining(string name, LocalStackLayer layer) =>
		_stacks.TryGetValue(name, out var entries)
			? entries.Where(e => e.layer == layer && e.file != null).Select(e => e.file)
			: Enumerable.Empty<string>();

	/// <summary>Every stack, grouped by the layer that first defines it, for the listing.</summary>
	public List<LocalStackStackListing> List()
	{
		var listing = new List<LocalStackStackListing>();
		foreach (var (name, entries) in _stacks)
		{
			var item = new LocalStackStackListing
			{
				name = name,
				layer = entries[0].layer,
				description = Merge(name).description,
				target = TargetOf(name),
				changedBy = entries.Skip(1).Select(e => e.layer).Where(l => l != entries[0].layer).Distinct().ToList()
			};

			var variants = new Dictionary<string, (string description, LocalStackLayer layer)>(StringComparer.OrdinalIgnoreCase);
			foreach (var entry in entries)
			{
				foreach (var (variant, def) in entry.def.variants ?? new Dictionary<string, LocalStackVariantDef>())
				{
					// The layer that first added the variant is what the listing marks; a later one only changes it.
					var layer = variants.TryGetValue(variant, out var existing) ? existing.layer : entry.layer;
					variants[variant] = (def?.description ?? existing.description, layer);
				}
			}

			item.variants = variants.Select(v => (v.Key, v.Value.description, v.Value.layer)).ToList();
			listing.Add(item);
		}

		// Built-in stacks keep the order they're written in; the others are alphabetical.
		var order = _stacks.Keys.Select((key, index) => (key, index)).ToDictionary(p => p.key, p => p.index, StringComparer.OrdinalIgnoreCase);
		return listing
			.OrderBy(s => s.layer)
			.ThenBy(s => s.layer == LocalStackLayer.BuiltIn ? order[s.name] : 0)
			.ThenBy(s => s.name, StringComparer.OrdinalIgnoreCase)
			.ToList();
	}

	/// <summary>
	/// Resolves <c>&lt;stack&gt;[:variant…]</c> against the settings' step catalogue. The stack's layers are merged,
	/// <c>extends</c> is followed, the selection is made by tag or step name, and the variants apply left to right:
	/// every <c>remove</c> first, then each <c>before</c> ahead of the stack's steps and each <c>after</c> behind them.
	/// A step listed more than once runs once, at its first position.
	/// </summary>
	public LocalStackResolvedStack Resolve(string fullName, LocalStackConfig config)
	{
		var parts = (fullName ?? string.Empty).Split(':');
		if (parts.Any(string.IsNullOrWhiteSpace))
			throw new LocalStackStackException($"'{fullName}' is not a stack name. Use <stack>[:<variant>...], for example full:build.");

		var stackName = parts[0].Trim();
		var effective = Effective(stackName, new List<string>());
		var errors = new List<string>();

		var catalogue = (config?.steps ?? new List<LocalStackStep>()).ToList();
		foreach (var (stepName, step) in effective.steps)
		{
			if (step == null) continue;
			step.name = stepName;
			// A stack's own step carries only the tags it declares, so a tag the stack selects by never pulls in a
			// step written to be asked for by name (a debug build of the gateway is not "csharp" for every stack).
			step.tags ??= new List<string>();
			var index = catalogue.FindIndex(s => string.Equals(s.name, stepName, StringComparison.OrdinalIgnoreCase));
			if (index >= 0) catalogue[index] = step;
			else catalogue.Add(step);
		}

		// A stack's own step runs where it says: first, after a named step, or (by default) last.
		foreach (var step in effective.steps.Values.Where(s => s != null && (s.first == true || !string.IsNullOrWhiteSpace(s.after))).Reverse())
		{
			catalogue.Remove(step);
			var at = step.first == true ? 0
				: catalogue.FindIndex(s => string.Equals(s.name, step.after.Trim(), StringComparison.OrdinalIgnoreCase)) + 1;
			if (at <= 0 && step.first != true)
			{
				errors.Add($"{stackName}: step '{step.name}' runs after '{step.after}', which isn't a step on this machine.");
				at = catalogue.Count;
			}

			catalogue.Insert(at, step);
		}

		List<LocalStackStep> Match(string item, string where, out bool requests)
		{
			requests = false;
			var name = item?.Trim() ?? string.Empty;
			var exact = catalogue.Where(s => string.Equals(s.name, name, StringComparison.OrdinalIgnoreCase)).ToList();
			if (exact.Count > 0)
			{
				requests = true;
				return exact.Where(s => s.enabled).ToList();
			}

			var tagged = catalogue.Where(s => s.enabled && TagsOf(s).Contains(name, StringComparer.OrdinalIgnoreCase)).ToList();
			if (tagged.Count == 0 && !IsKnownTag(name, catalogue))
				errors.Add($"{where}: no step or tag '{name}' in this machine's settings.");

			requests = RequestingTags.Contains(name);
			return tagged;
		}

		var selected = new HashSet<LocalStackStep>();
		var requested = new HashSet<LocalStackStep>();
		var excluded = new HashSet<LocalStackStep>();
		foreach (var (stack, include, exclude) in effective.selections)
		{
			foreach (var item in include ?? new List<string>())
			{
				foreach (var step in Match(item, $"{stack}: include", out var requests))
				{
					selected.Add(step);
					if (requests) requested.Add(step);
				}
			}

			foreach (var item in exclude ?? new List<string>())
			{
				foreach (var step in Match(item, $"{stack}: exclude", out _))
				{
					selected.Remove(step);
					requested.Remove(step);
					excluded.Add(step);
				}
			}
		}

		if (effective.extensions != null)
		{
			foreach (var id in effective.extensions)
			{
				if (!catalogue.Any(s => IsExtensionStep(s) && string.Equals(ExtensionId(s.name), id, StringComparison.OrdinalIgnoreCase)))
					errors.Add($"{stackName}: extensions: no portal extension, microservice or service group '{id}' in this machine's settings.");
			}

			selected.RemoveWhere(s => IsExtensionStep(s)
			                          && !effective.extensions.Contains(ExtensionId(s.name), StringComparer.OrdinalIgnoreCase));
		}

		// A step's build comes with it, so a stack never has to name builds: the C# gateway brings `build: c# gateway`,
		// a Scala service the reactor build, the portal its dependencies. Whether it then builds is the run's call (only
		// when needed; `:build` forces it, `:no-build` removes it below). A build the stack excludes stays out.
		foreach (var step in selected.Where(s => !s.build).ToList())
		{
			foreach (var build in LocalStackControl.BuildsFor(step, catalogue, TagsOf).Where(b => b.enabled && !excluded.Contains(b)))
				selected.Add(build);
		}

		var resolved = new LocalStackResolvedStack
		{
			name = fullName,
			description = effective.description,
			variables = effective.variables,
			target = string.IsNullOrWhiteSpace(effective.target) ? LocalStackTargets.Local : effective.target.Trim()
		};

		var removed = new HashSet<LocalStackStep>();
		var before = new List<LocalStackStep>();
		var after = new List<LocalStackStep>();
		foreach (var variantName in parts.Skip(1).Select(p => p.Trim()))
		{
			// The stack's own variant shadows a built-in one of the same name, and both shadow a target's.
			var targets = LocalStackTargets.Names(config);
			if (!effective.variants.TryGetValue(variantName, out var variant) && !_builtInVariants.TryGetValue(variantName, out variant))
			{
				if (!targets.Contains(variantName, StringComparer.OrdinalIgnoreCase))
				{
					var available = effective.variants.Keys.Concat(_builtInVariants.Keys).Concat(targets).Distinct(StringComparer.OrdinalIgnoreCase);
					throw new LocalStackStackException(
						$"Stack '{stackName}' has no variant '{variantName}'. Its variants: {string.Join(", ", available.Select(v => ":" + v))}.");
				}

				variant = new LocalStackVariantDef { target = targets.First(t => string.Equals(t, variantName, StringComparison.OrdinalIgnoreCase)) };
			}

			variant ??= new LocalStackVariantDef();
			var where = $"{stackName}:{variantName}";
			foreach (var item in variant.remove ?? new List<string>())
				removed.UnionWith(Match(item, $"{where}: remove", out _));
			foreach (var item in variant.before ?? new List<string>())
				before.AddRange(Match(item, $"{where}: before", out _));
			foreach (var item in variant.after ?? new List<string>())
				after.AddRange(Match(item, $"{where}: after", out _));

			resolved.forceBuild |= variant.forceBuild;
			if (!string.IsNullOrWhiteSpace(variant.target)) resolved.target = variant.target.Trim();
		}

		if (!LocalStackTargets.IsKnown(resolved.target, config))
			errors.Add($"its backend '{resolved.target}' isn't a target: use {string.Join(", ", LocalStackTargets.Names(config))}, or an http(s) address");

		if (errors.Count > 0)
			throw new LocalStackStackException($"'{fullName}' can't be resolved:\n  " + string.Join("\n  ", errors.Distinct()));

		var own = catalogue.Where(s => selected.Contains(s) && !removed.Contains(s));
		var seenSteps = new HashSet<LocalStackStep>();
		foreach (var step in before.Concat(own).Concat(after))
		{
			if (seenSteps.Add(step))
				resolved.steps.Add(step);
		}

		// What a variant adds is asked for outright, so its build steps run like a named step's.
		resolved.requested.UnionWith(requested.Where(seenSteps.Contains));
		resolved.requested.UnionWith(before);
		resolved.requested.UnionWith(after);
		return resolved;
	}

	/// <summary>
	/// The tags a step is selected by: its own, or the ones its name implies when it has none written, plus the
	/// built-in tag groups listing it.
	/// </summary>
	public List<string> TagsOf(LocalStackStep step)
	{
		var tags = step.tags != null ? new List<string>(step.tags) : LocalStackConfigIO.InferTags(step.name ?? string.Empty);
		foreach (var (tag, names) in _tagGroups)
		{
			if (names.Contains(step.name, StringComparer.OrdinalIgnoreCase) && !tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
				tags.Add(tag);
		}

		return tags;
	}

	private bool IsKnownTag(string name, List<LocalStackStep> catalogue) =>
		KnownTags.Contains(name, StringComparer.OrdinalIgnoreCase)
		|| _tagGroups.ContainsKey(name)
		|| catalogue.Any(s => s.tags?.Contains(name, StringComparer.OrdinalIgnoreCase) == true);

	private bool IsExtensionStep(LocalStackStep step) => TagsOf(step).Any(IsServiceTag);

	/// <summary>The id a stack's <c>extensions</c> list matches: the step name without its <c>portal extension: </c>,
	/// <c>microservice: </c> or <c>group: </c> prefix.</summary>
	public static string ExtensionId(string stepName)
	{
		foreach (var prefix in new[] { LocalStackTemplate.ExtensionPrefix, LocalStackTemplate.MicroservicePrefix, LocalStackTemplate.GroupPrefix })
		{
			if (stepName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
				return stepName.Substring(prefix.Length).Trim();
		}

		return stepName;
	}

	private class EffectiveStack
	{
		public string description;
		public string target;
		public List<(string stack, List<string> include, List<string> exclude)> selections = new List<(string, List<string>, List<string>)>();
		public List<string> extensions;
		public Dictionary<string, LocalStackStackVariable> variables = new Dictionary<string, LocalStackStackVariable>(StringComparer.Ordinal);
		public Dictionary<string, LocalStackStep> steps = new Dictionary<string, LocalStackStep>(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, LocalStackVariantDef> variants = new Dictionary<string, LocalStackVariantDef>(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>The target a stack runs against, following <c>extends</c>; <c>local</c> when unset or when the chain is broken.</summary>
	public string TargetOf(string name)
	{
		try
		{
			var target = Effective(name, new List<string>()).target;
			return string.IsNullOrWhiteSpace(target) ? LocalStackTargets.Local : target.Trim();
		}
		catch (LocalStackStackException)
		{
			return LocalStackTargets.Local;
		}
	}

	/// <summary>A stack with its layers merged and its <c>extends</c> chain followed; the base comes first.</summary>
	private EffectiveStack Effective(string name, List<string> chain)
	{
		if (!_stacks.ContainsKey(name))
		{
			var known = string.Join(", ", _stacks.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase));
			throw new LocalStackStackException(chain.Count == 0
				? $"No stack named '{name}'. Run `beam local run` to list them. Stacks: {known}."
				: $"Stack '{chain[^1]}' extends '{name}', which doesn't exist.");
		}

		if (chain.Contains(name, StringComparer.OrdinalIgnoreCase))
			throw new LocalStackStackException("Stacks extend each other in a loop: " + string.Join(" -> ", chain.Append(name)) + ".");

		var def = Merge(name);
		chain.Add(name);
		var effective = string.IsNullOrWhiteSpace(def.extends) ? new EffectiveStack() : Effective(def.extends.Trim(), chain);
		chain.RemoveAt(chain.Count - 1);

		effective.description = def.description ?? effective.description;
		effective.target = def.target ?? effective.target;
		effective.selections.Add((name, def.include, def.exclude));
		effective.extensions = def.extensions ?? effective.extensions;
		foreach (var (key, value) in def.variables ?? new Dictionary<string, LocalStackStackVariable>())
			effective.variables[key] = value;
		foreach (var (key, value) in def.steps ?? new Dictionary<string, LocalStackStep>())
			effective.steps[key] = value;
		foreach (var (key, value) in def.variants ?? new Dictionary<string, LocalStackVariantDef>())
			effective.variants[key] = value;
		return effective;
	}

	/// <summary>One stack's layers merged: fields set in a higher layer win; variants, variables and steps merge by name.</summary>
	private LocalStackStackDef Merge(string name)
	{
		var merged = new LocalStackStackDef();
		foreach (var entry in _stacks[name])
		{
			var def = entry.def;
			merged.description = def.description ?? merged.description;
			merged.extends = def.extends ?? merged.extends;
			merged.target = def.target ?? merged.target;
			merged.include = def.include ?? merged.include;
			merged.exclude = def.exclude ?? merged.exclude;
			merged.extensions = def.extensions ?? merged.extensions;
			merged.variables = MergeByName(merged.variables, def.variables, StringComparer.Ordinal);
			merged.steps = MergeByName(merged.steps, def.steps, StringComparer.OrdinalIgnoreCase);
			merged.variants = MergeByName(merged.variants, def.variants, StringComparer.OrdinalIgnoreCase);
		}

		return merged;
	}

	private static Dictionary<string, T> MergeByName<T>(Dictionary<string, T> lower, Dictionary<string, T> higher, StringComparer comparer)
	{
		if (higher == null) return lower;
		var merged = lower == null ? new Dictionary<string, T>(comparer) : new Dictionary<string, T>(lower, comparer);
		foreach (var (key, value) in higher)
			merged[key] = value;
		return merged;
	}
}

/// <summary>A step a stack needs and doesn't run: what to add, and why.</summary>
public class LocalStackMissingStep
{
	/// <summary>The step to add: a catalogue step's name, or a service step to write (<c>microservice: X</c>).</summary>
	public string name;

	/// <summary>The catalogue step, or null when the stack has to define it itself (a microservice an extension calls).</summary>
	public LocalStackStep step;

	/// <summary>The step that needs it, by its readable name.</summary>
	public string neededBy;

	/// <summary>Why, in a few words: "C# gateway needs it".</summary>
	public string reason;
}

/// <summary>
/// What a stack's steps need that it doesn't run. A step names what it needs in <see cref="LocalStackStep.dependsOn"/>
/// (a step name, or a tag for every step that has it). On the local backend, a microservice, an extension or a service
/// group also needs the backend it logs into: the docker dependencies, the C# gateway and the Scala services needed
/// to log in; and an extension or group needs the portal to show it. What a missing step needs is followed too.
/// </summary>
public static class LocalStackDependencies
{
	public const string ApiDepsStep = "docker: api deps + caddy";
	public const string GatewayStep = "c# gateway";
	public const string LoginServicesTag = "scala-core";

	/// <summary>What <paramref name="step"/> needs, by name or tag.</summary>
	public static List<string> Needs(LocalStackStep step, bool localBackend)
	{
		var needs = new List<string>(step.dependsOn ?? new List<string>());
		var name = step.name ?? string.Empty;
		var isService = name.StartsWith(LocalStackTemplate.MicroservicePrefix, StringComparison.OrdinalIgnoreCase);
		var isExtension = name.StartsWith(LocalStackTemplate.ExtensionPrefix, StringComparison.OrdinalIgnoreCase)
		                  || name.StartsWith(LocalStackTemplate.GroupPrefix, StringComparison.OrdinalIgnoreCase);
		if (localBackend && (isService || isExtension))
		{
			needs.Add(ApiDepsStep);
			needs.Add(GatewayStep);
			needs.Add(LoginServicesTag);
			if (isExtension) needs.Add(LocalStackTemplate.PortalFrontendStepName);
		}

		return needs.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	/// <summary>
	/// The steps <paramref name="running"/> needs and doesn't have, each once, in the order they were found. A need
	/// is looked up in <paramref name="catalogue"/> by name, then by tag. A <c>microservice: X</c> the catalogue doesn't
	/// have comes back without a step, for the stack to define.
	/// </summary>
	public static List<LocalStackMissingStep> Missing(IReadOnlyList<LocalStackStep> running, IReadOnlyList<LocalStackStep> catalogue,
		Func<LocalStackStep, IEnumerable<string>> tagsOf, bool localBackend)
	{
		bool Same(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
		var have = running.Select(s => s.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		var missing = new List<LocalStackMissingStep>();
		var queue = new Queue<LocalStackStep>(running);
		while (queue.Count > 0)
		{
			var step = queue.Dequeue();
			var label = string.IsNullOrWhiteSpace(step.label) ? LocalStackConfigIO.InferLabel(step.name ?? string.Empty) : step.label;
			foreach (var need in Needs(step, localBackend))
			{
				// Builds come with the steps that need them (see Resolve), so they are never a missing step.
				var byName = catalogue.Where(s => Same(s.name, need)).ToList();
				var candidates = (byName.Count > 0
					? byName
					: catalogue.Where(s => s.enabled && !Same(s.name, step.name) && tagsOf(s).Contains(need, StringComparer.OrdinalIgnoreCase)).ToList())
					.Where(s => !s.build).ToList();

				if (candidates.Count == 0 && need.StartsWith(LocalStackTemplate.MicroservicePrefix, StringComparison.OrdinalIgnoreCase) && have.Add(need))
				{
					missing.Add(new LocalStackMissingStep { name = need, neededBy = label, reason = $"{label} calls it" });
					continue;
				}

				foreach (var candidate in candidates.Where(c => c.enabled && have.Add(c.name)))
				{
					missing.Add(new LocalStackMissingStep
					{
						name = candidate.name, step = candidate, neededBy = label,
						reason = Needs(step, false).Contains(need, StringComparer.OrdinalIgnoreCase) ? $"{label} needs it"
							: Same(need, LocalStackTemplate.PortalFrontendStepName) ? $"{label} is shown in it"
							: $"{label} logs into the local backend, which needs it"
					});
					queue.Enqueue(candidate);
				}
			}
		}

		return missing;
	}
}
