using Beamable.Server;
using cli.Services;
using cli.Services.LocalStack;
using cli.Utils;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackInitCommandArgs : CommandArgs
{
	public string configPath;
	public bool force;
	public string host;
	public string portalUrl;
	public string apiDir;
	public string scalaDir;
	public string portalDir;
	public string scalaServices;
	public string services;
	public string extensions;
	public bool updateServices;

	/// <summary>
	/// Answer "no" to the web-registry question without being asked. The steps are still written to the
	/// manifest — what this records is that <c>beam local up</c> should not run them (see
	/// <see cref="LocalStackConfig.webRegistry"/>).
	/// </summary>
	public bool noWebRegistry;

	/// <summary>
	/// Answer "yes" to the web-registry question without being asked. Its own field rather than an inversion
	/// of <see cref="noWebRegistry"/> so "not passed" stays distinguishable from "passed off" — which is what
	/// lets the interactive prompt know it still has a question to ask.
	/// </summary>
	public bool withWebRegistry;
	public string webRegistryDir;
	public string scalaJvmArgs;

	/// <summary>Opt IN to writing the <c>beam-local-stack</c> agent skill. Off by default — the manifest is
	/// what <c>init</c> exists to produce, and the doc is a separate, generated file that not every workspace
	/// wants (it lands outside <c>.beamable</c> and records machine-specific absolute paths).</summary>
	public bool skill;
}

public class LocalStackInitCommandResult
{
	public string manifestPath;
	public int stepCount;
	public bool created;

	/// <summary>Where the generated <c>beam-local-stack</c> skill was written; null when it was skipped.</summary>
	public string skillPath;
}

/// <summary>
/// Interactively builds a reference local-stack manifest (see <see cref="LocalStackTemplate"/>) that
/// <c>beam local up</c> then runs. Each value is prompted for with its default shown — press Enter to
/// accept it. Any value passed as an option is used as-is (and not prompted for); <c>--quiet</c> (or a
/// non-interactive console) skips all prompts and uses the defaults / passed values. Repo paths left
/// empty become <c>&lt;EDIT: ...&gt;</c> placeholders to fill in by hand.
/// </summary>
public class LocalStackInitCommand
	: AtomicCommand<LocalStackInitCommandArgs, LocalStackInitCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	public LocalStackInitCommand() : base("init", "Write a reference local-stack manifest to edit and run")
	{
	}

	public override void Configure()
	{
		AddOption(new Option<string>("--config", "Path to write the manifest to (defaults to .beamable/local-stack.json)"),
			(args, v) => args.configPath = v);
		AddOption(new Option<bool>("--force", "Overwrite an existing manifest without asking"),
			(args, v) => args.force = v);
		AddOption(new Option<string>("--host", "Backend API host baked into the manifest"),
			(args, v) => args.host = v);
		AddOption(new Option<string>("--portal-url", "Portal frontend URL baked into the manifest"),
			(args, v) => args.portalUrl = v);
		AddOption(new Option<string>("--api-dir", "Absolute path to the BeamableAPI (C# gateway) repo"),
			(args, v) => args.apiDir = v);
		AddOption(new Option<string>("--scala-dir", "Absolute path to the BeamableBackend (Scala) repo"),
			(args, v) => args.scalaDir = v);
		AddOption(new Option<string>("--portal-dir", "Absolute path to the portal frontend repo"),
			(args, v) => args.portalDir = v);
		AddOption(new Option<string>("--scala-services", "Comma/space separated Scala tools/* services to run"),
			(args, v) => args.scalaServices = v);
		AddOption(new Option<string>("--services", "Comma/space separated microservice ids to run"),
			(args, v) => args.services = v);
		AddOption(new Option<string>("--extensions", "Comma/space separated portal extension ids to run"),
			(args, v) => args.extensions = v);
		AddOption(new Option<bool>("--update-services", "Only update the microservice/extension steps of an existing manifest, leaving everything else untouched"),
			(args, v) => args.updateServices = v);
		AddOption(new Option<string>("--scala-jvm-args", () => LocalStackTemplate.DefaultScalaJvmArgs,
				"JVM flags each Scala service is launched with; the heap cap keeps ~18 JDK 8 JVMs from each reserving a quarter of physical RAM"),
			(args, v) => args.scalaJvmArgs = v);
		// The web-registry steps are ALWAYS written; what this command records is whether `beam local up` runs
		// them, so nobody has to remember --no-web-registry on every single `up`. The question defaults to NO
		// (the registry is only useful when iterating on @beamable/sdk or @beamable/portal-toolkit), and these
		// two flags answer it without prompting.
		AddOption(new Option<bool>("--no-web-registry", "Record that `beam local up` should not run the local web package registry steps (Verdaccio and local-unpkg); this is the default, and it skips both prompts"),
			(args, v) => args.noWebRegistry = v);
		AddOption(new Option<bool>("--with-web-registry", "Record that `beam local up` should run the local web package registry steps; wins over --no-web-registry when both are passed"),
			(args, v) => args.withWebRegistry = v);
		AddOption(new Option<string>("--web-registry-dir", "Absolute path to the portal-localdev directory holding the web registry compose file; implies --with-web-registry and skips both prompts"),
			(args, v) => args.webRegistryDir = v);
		var skill = new Option<bool>("--skill",
			$"Also write the {LocalStackSkillTemplate.SkillName} agent skill, documenting the repositories this manifest spans and how the stack works, to .claude/skills/ (off by default; regenerated on every init that passes this)");
		skill.AddAlias("--with-skill");
		AddOption(skill, (args, v) => args.skill = v);
	}

	private static List<string> Split(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

	private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

	/// <summary>
	/// The standing web-registry choice to record in the manifest: whether <c>beam local up</c> runs the local
	/// web package registry steps without being asked to.
	///
	/// An explicit flag answers it, as does an explicit <c>--web-registry-dir</c> (naming the directory only
	/// makes sense if you want the registry). Otherwise an interactive run asks, defaulting to NO — the
	/// registry is only useful while iterating on <c>@beamable/sdk</c> or <c>@beamable/portal-toolkit</c>, and
	/// its steps are a slow pnpm rebuild plus a reinstall per extension. A quiet run takes that same default.
	/// </summary>
	private static bool ResolveWebRegistryChoice(LocalStackInitCommandArgs args, bool quiet)
	{
		// --with-web-registry wins over --no-web-registry when both are passed, matching `beam local up`.
		if (args.withWebRegistry || !string.IsNullOrWhiteSpace(args.webRegistryDir))
		{
			return true;
		}

		if (args.noWebRegistry || quiet)
		{
			return false;
		}

		return AnsiConsole.Confirm(
			"Run the local web package registry ([green]Verdaccio[/] + local-unpkg) on `beam local up`? "
			+ "[grey](only needed while iterating on the web SDK; change it by re-running this command)[/]",
			defaultValue: false);
	}

	/// <summary>
	/// Resolves the <c>portal-localdev</c> directory for the web-registry steps, which are always written so
	/// that <c>beam local up --with-web-registry</c> keeps working without regenerating the manifest.
	///
	/// An explicit <c>--web-registry-dir</c> wins. Otherwise the path is only PROMPTED for when the registry
	/// was actually asked for; a "no" answer takes the auto-detected path silently rather than adding a
	/// question about a directory the stack is not going to use. Either way an unresolved path becomes an
	/// <c>&lt;EDIT: ...&gt;</c> placeholder, exactly like the other repo paths.
	/// </summary>
	private static string ResolveWebRegistryDir(
		LocalStackInitCommandArgs args, string startDir, bool quiet, bool webRegistry)
	{
		if (!string.IsNullOrWhiteSpace(args.webRegistryDir))
		{
			return args.webRegistryDir;
		}

		var productDir = FindRepoDir(startDir, "BeamableProduct");
		var detected = productDir == null ? null : Path.Combine(productDir, "portal-localdev");
		if (detected != null && !Directory.Exists(detected))
		{
			detected = null;
		}

		if (!webRegistry)
		{
			return detected;
		}

		// Empty is allowed: the template writes an <EDIT: ...> placeholder, matching the other repo paths.
		return Ask("Absolute path to the [green]portal-localdev[/] web registry directory [grey](empty = placeholder)[/]:",
			null, detected, quiet, allowEmpty: true);
	}

	/// <summary>
	/// Looks for a folder named <paramref name="name"/> in <paramref name="startDir"/> and its ancestors (up to
	/// <paramref name="maxLevels"/> levels up), returning the first match — used to auto-fill the repo paths
	/// (BeamableAPI / BeamableBackend / agentic-portal) that typically sit as siblings a level or two up.
	/// </summary>
	private static string FindRepoDir(string startDir, string name, int maxLevels = 3)
	{
		try
		{
			var dir = new DirectoryInfo(string.IsNullOrEmpty(startDir) ? Directory.GetCurrentDirectory() : startDir);
			for (var i = 0; i <= maxLevels && dir != null; i++)
			{
				var candidate = Path.Combine(dir.FullName, name);
				if (Directory.Exists(candidate)) return candidate;
				dir = dir.Parent;
			}
		}
		catch { /* best-effort discovery */ }

		return null;
	}

	/// <summary>Ensures the manifest directory's <c>.gitignore</c> ignores the generated run-state + logs.</summary>
	private static void EnsureGitignore(string dir)
	{
		try
		{
			if (string.IsNullOrEmpty(dir)) return;
			var gitignorePath = Path.Combine(dir, ".gitignore");
			var wanted = new[] { LocalStackRunStateIO.LogsDirName + "/", LocalStackRunStateIO.RunStateFileName };

			var existing = File.Exists(gitignorePath)
				? File.ReadAllLines(gitignorePath).Select(l => l.Trim()).ToHashSet()
				: new HashSet<string>();

			var toAdd = wanted.Where(e => !existing.Contains(e)).ToList();
			if (toAdd.Count == 0) return;

			var block = new List<string> { "", "# Beamable local-stack generated artifacts" };
			block.AddRange(toAdd);
			File.AppendAllText(gitignorePath, string.Join(Environment.NewLine, block) + Environment.NewLine);
			Log.Information($"Added local-stack artifacts to {gitignorePath}.");
		}
		catch (Exception e)
		{
			Log.Verbose($"could not update .gitignore: {e.Message}");
		}
	}

	/// <summary>
	/// Writes the <c>beam-local-stack</c> agent skill that documents the repositories this manifest spans and
	/// how the stack works (see <see cref="LocalStackSkillTemplate"/>). Opt-in via <c>--skill</c>; when asked
	/// for, it always OVERWRITES, because the file is generated from the manifest and a stale copy describing
	/// a stack you no longer have is worse than none — so it needs neither <c>--force</c> nor a prompt, unlike
	/// the manifest itself.
	///
	/// Best-effort, like <see cref="EnsureGitignore"/>: an init that already produced a valid manifest must
	/// never fail because a documentation file could not be written.
	/// </summary>
	/// <returns>The path written, or null when not asked for or when writing failed.</returns>
	private static string WriteSkill(LocalStackInitCommandArgs args, string manifestPath, LocalStackConfig config)
	{
		if (!args.skill)
		{
			return null;
		}

		try
		{
			var skillPath = LocalStackSkillTemplate.Write(args.ConfigService, manifestPath, config);
			if (skillPath == null)
			{
				Log.Verbose("local init: the embedded local-stack skill template is missing; skipped writing it.");
				return null;
			}

			Log.Information($"Wrote local-stack skill to {skillPath}.");
			return skillPath;
		}
		catch (Exception e)
		{
			Log.Verbose($"local init: could not write the local-stack skill: {e.Message}");
			return null;
		}
	}

	/// <summary>
	/// The default Scala selection: discovered tools whose names are in the curated <see cref="LocalStackTemplate.DefaultScalaServices"/>
	/// (so we keep the known-good set but with resolved main classes); all discovered tools if that intersection
	/// is empty; and the static curated list when nothing was discovered.
	/// </summary>
	private static List<string> ResolveDefaultScalaNames(List<LocalStackTemplate.ScalaToolInfo> discovered)
	{
		if (discovered == null || discovered.Count == 0)
			return LocalStackTemplate.DefaultScalaServices.ToList();

		var curated = new HashSet<string>(LocalStackTemplate.DefaultScalaServices, StringComparer.OrdinalIgnoreCase);
		var inCurated = discovered.Where(t => curated.Contains(t.name)).Select(t => t.name).ToList();
		return inCurated.Count > 0 ? inCurated : discovered.Select(t => t.name).ToList();
	}

	/// <summary>
	/// Discovers the local microservice/portal-extension ids and service groups in the current <c>.beamable</c>
	/// workspace by loading the beamo manifest (no network). Best-effort: returns empty results when run outside
	/// a workspace or if the manifest can't be read, so <c>init</c> stays usable anywhere.
	/// </summary>
	private static async Task<(List<string> services, List<string> extensions, Dictionary<string, string[]> groups)> DiscoverWorkspaceServices(LocalStackInitCommandArgs args)
	{
		var services = new List<string>();
		var extensions = new List<string>();
		var groups = new Dictionary<string, string[]>();
		try
		{
			if (args.ConfigService?.DirectoryExists != true)
				return (services, extensions, groups);

			await args.BeamoLocalSystem.InitManifest(useManifestCache: true, fetchServerManifest: false);
			var manifest = args.BeamoLocalSystem.BeamoManifest;
			var defs = manifest?.ServiceDefinitions ?? new List<BeamoServiceDefinition>();

			services = defs.Where(d => d.Protocol == BeamoProtocolType.HttpMicroservice)
				.Select(d => d.BeamoId).OrderBy(x => x).ToList();
			extensions = defs.Where(d => d.Protocol == BeamoProtocolType.PortalExtension)
				.Select(d => d.BeamoId).OrderBy(x => x).ToList();
			groups = manifest?.ServiceGroupToBeamoIds != null
				? new Dictionary<string, string[]>(manifest.ServiceGroupToBeamoIds, StringComparer.OrdinalIgnoreCase)
				: new Dictionary<string, string[]>();
		}
		catch (Exception e)
		{
			Log.Verbose($"local init: could not discover workspace services/extensions: {e.Message}");
		}

		return (services, extensions, groups);
	}

	/// <summary>
	/// Merges the extensions found by scanning a portal checkout into the ones the beamo manifest reported.
	///
	/// The beamo manifest only covers the workspace the command RUNS in, so running <c>init</c> anywhere but the
	/// portal repo discovered nothing and the picker came up empty — which reads as "this project has no
	/// extensions" rather than "you are standing somewhere else". Scanning the checkout that was actually pointed
	/// at fixes that for both the interactive flow and <c>--update-services</c>.
	/// </summary>
	private static (List<string> extensions, Dictionary<string, string[]> groups) MergePortalExtensions(
		string portalDir, List<string> discoveredExtensions, Dictionary<string, string[]> discoveredGroups)
	{
		var (scannedExtensions, scannedGroups) = LocalStackTemplate.DiscoverPortalExtensions(portalDir);

		var extensions = (discoveredExtensions ?? new List<string>())
			.Concat(scannedExtensions)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
			.ToList();

		var groups = discoveredGroups != null
			? new Dictionary<string, string[]>(discoveredGroups, StringComparer.OrdinalIgnoreCase)
			: new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

		foreach (var (group, members) in scannedGroups)
		{
			// A group the workspace already knows about wins — that membership is authoritative.
			if (!groups.ContainsKey(group)) groups[group] = members;
		}

		return (extensions, groups);
	}

	/// <summary>Maps the selected Scala service names to <see cref="LocalStackTemplate.ScalaToolInfo"/>, attaching
	/// the discovered main class where known (unknown names get a null main class → the launch shell greps pom.xml).</summary>
	private static List<LocalStackTemplate.ScalaToolInfo> ToScalaTools(List<string> names, List<LocalStackTemplate.ScalaToolInfo> discovered)
	{
		if (names == null) return null;
		var byName = (discovered ?? new List<LocalStackTemplate.ScalaToolInfo>())
			.ToDictionary(t => t.name, StringComparer.OrdinalIgnoreCase);
		return names
			.Select(n => byName.TryGetValue(n, out var info)
				? info
				: new LocalStackTemplate.ScalaToolInfo { name = n })
			.ToList();
	}

	/// <summary>
	/// Resolves one value: a passed option wins; otherwise in quiet mode the default is used; otherwise
	/// the user is prompted with the default shown (Enter accepts it).
	/// </summary>
	private static string Ask(string title, string passed, string def, bool quiet, bool allowEmpty)
	{
		// A non-null value was explicitly provided on the command line (even ""), so honor it verbatim —
		// this lets `--extensions ""` clear the list rather than falling back to the default.
		if (passed != null) return passed;
		if (quiet) return def ?? string.Empty;

		var prompt = new TextPrompt<string>(title).PromptStyle("green");
		if (!string.IsNullOrEmpty(def)) prompt.DefaultValue(def);
		if (allowEmpty || string.IsNullOrEmpty(def)) prompt.AllowEmpty();
		return AnsiConsole.Prompt(prompt);
	}

	/// <summary>
	/// Prompts the user to pick from a discovered set of ids (space-separated result). A passed option wins
	/// verbatim (even ""); in quiet / non-interactive mode or when nothing was discovered, returns
	/// <paramref name="quietDefault"/> (empty for a fresh init, the current set for an update). Interactively,
	/// shows a multi-select with <paramref name="preselected"/> ticked.
	/// </summary>
	private static string AskServiceSelection(string title, string passed, List<string> choices,
		IEnumerable<string> preselected, bool quiet, string quietDefault)
	{
		if (passed != null) return passed;
		if (quiet || choices == null || choices.Count == 0) return quietDefault ?? string.Empty;

		var prompt = new MultiSelectionPrompt<string>()
			.Title(title)
			.PageSize(15)
			.MoreChoicesText("[grey](move up/down to reveal more)[/]")
			.InstructionsText("[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept; select none to skip)[/]")
			.AddChoices(choices)
			.AddBeamHightlight()
			.NotRequired();

		var preselectSet = new HashSet<string>(preselected ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
		foreach (var c in choices)
			if (preselectSet.Contains(c))
				prompt.Select(c);

		return string.Join(" ", AnsiConsole.Prompt(prompt));
	}

	/// <summary>Display prefix for service-group entries in the extension multi-select (per the "GROUP name" ask).</summary>
	private const string GroupDisplayPrefix = "GROUP ";

	/// <summary>Meta-choice in the Scala picker that expands to the curated <see cref="LocalStackTemplate.DefaultScalaServices"/> set.</summary>
	private const string ScalaDefaultChoice = "DEFAULT (curated set)";

	/// <summary>Meta-choice in the Scala picker that expands to every discovered <c>tools/*</c> service.</summary>
	private const string ScalaAllChoice = "ALL (every discovered service)";

	/// <summary>
	/// Scala-services picker. Offers two meta-choices — DEFAULT (the curated <see cref="LocalStackTemplate.DefaultScalaServices"/>
	/// set, preselected) and ALL (every discovered tool) — followed by each discovered service so the user can
	/// add services individually on top of DEFAULT. ALL wins over everything; otherwise the result is the union of
	/// DEFAULT (if ticked) and any individually-ticked services, de-duplicated (case-insensitive, order-preserving).
	/// A passed --scala-services value wins verbatim (with the "default"/"all" keywords expanded for scripting parity);
	/// quiet / non-interactive mode returns the default set.
	/// </summary>
	private static string AskScalaSelection(string title, string passed,
		List<string> discoveredNames, List<string> defaultNames, bool quiet)
	{
		if (passed != null) return ExpandScalaKeywords(passed, discoveredNames, defaultNames);
		if (quiet) return string.Join(" ", defaultNames);

		var choices = new List<string> { ScalaDefaultChoice };
		if (discoveredNames.Count > 0) choices.Add(ScalaAllChoice);
		choices.AddRange(discoveredNames);

		var prompt = new MultiSelectionPrompt<string>()
			.Title(title)
			.PageSize(15)
			.MoreChoicesText("[grey](move up/down to reveal more)[/]")
			.InstructionsText("[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept; DEFAULT + any extras are combined)[/]")
			.AddChoices(choices)
			.AddBeamHightlight()
			.NotRequired();
		prompt.Select(ScalaDefaultChoice); // DEFAULT ticked out of the box (today's behavior)

		return ResolveScalaSelection(AnsiConsole.Prompt(prompt), discoveredNames, defaultNames);
	}

	/// <summary>Turns the picked Scala choices into a space-separated, de-duplicated name list: ALL wins over
	/// everything; otherwise DEFAULT (if ticked) is unioned with the individually-ticked services.</summary>
	private static string ResolveScalaSelection(List<string> selected,
		List<string> discoveredNames, List<string> defaultNames)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var ordered = new List<string>();
		void Add(string n)
		{
			if (seen.Add(n))
			{
				ordered.Add(n);
			}
		}

		if (selected.Contains(ScalaAllChoice))
		{
			foreach (var n in discoveredNames)
			{
				Add(n);
			}
		}
		else
		{
			if (selected.Contains(ScalaDefaultChoice))
			{
				foreach (var n in defaultNames)
				{
					Add(n);
				}
			}

			foreach (var s in selected)
			{
				if (s == ScalaDefaultChoice || s == ScalaAllChoice)
				{
					continue;
				}

				Add(s);
			}
		}

		return string.Join(" ", ordered);
	}

	/// <summary>Expands the "default"/"all" keywords in a passed --scala-services value to their name sets and
	/// de-duplicates the result (so e.g. "default auth" = the curated set plus auth, with no repeats).</summary>
	private static string ExpandScalaKeywords(string passed,
		List<string> discoveredNames, List<string> defaultNames)
	{
		var tokens = Split(passed);
		if (tokens == null) return passed; // empty/whitespace → honor verbatim (clears the list)

		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var ordered = new List<string>();
		void Add(string n)
		{
			if (seen.Add(n))
			{
				ordered.Add(n);
			}
		}

		foreach (var t in tokens)
		{
			if (string.Equals(t, "all", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var n in discoveredNames.Count > 0 ? discoveredNames : defaultNames)
				{
					Add(n);
				}
			}
			else if (string.Equals(t, "default", StringComparison.OrdinalIgnoreCase))
			{
				foreach (var n in defaultNames)
				{
					Add(n);
				}
			}
			else
			{
				Add(t);
			}
		}

		return string.Join(" ", ordered);
	}

	/// <summary>
	/// Portal-extension picker that also lists service groups (prefixed "GROUP ") at the top. Returns the
	/// selected group names and individual extension ids separately. A passed <c>--extensions</c> value wins
	/// (extensions only, no groups); in quiet mode / nothing to show, returns the quiet defaults.
	/// </summary>
	private static (List<string> groups, List<string> extensions) AskExtensionSelection(
		string title, string passed, Dictionary<string, string[]> groupsMap, List<string> extensionChoices,
		IEnumerable<string> preselectedGroups, IEnumerable<string> preselectedExtensions,
		bool quiet, List<string> quietGroups, List<string> quietExtensions)
	{
		if (passed != null)
			return (new List<string>(), Split(passed) ?? new List<string>());

		var groupNames = groupsMap?.Keys.OrderBy(x => x).ToList() ?? new List<string>();
		extensionChoices ??= new List<string>();
		if (quiet || (groupNames.Count == 0 && extensionChoices.Count == 0))
			return (quietGroups ?? new List<string>(), quietExtensions ?? new List<string>());

		var choices = groupNames.Select(g => GroupDisplayPrefix + g).Concat(extensionChoices).ToList();
		var prompt = new MultiSelectionPrompt<string>()
			.Title(title)
			.PageSize(15)
			.MoreChoicesText("[grey](move up/down to reveal more)[/]")
			.InstructionsText("[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept; select none to skip)[/]")
			.AddChoices(choices)
			.AddBeamHightlight()
			.NotRequired();

		var pre = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var g in preselectedGroups ?? Enumerable.Empty<string>()) pre.Add(GroupDisplayPrefix + g);
		foreach (var e in preselectedExtensions ?? Enumerable.Empty<string>()) pre.Add(e);
		foreach (var c in choices)
			if (pre.Contains(c))
				prompt.Select(c);

		var selected = AnsiConsole.Prompt(prompt);
		var groups = selected.Where(s => s.StartsWith(GroupDisplayPrefix, StringComparison.Ordinal))
			.Select(s => s.Substring(GroupDisplayPrefix.Length)).ToList();
		var extensions = selected.Where(s => !s.StartsWith(GroupDisplayPrefix, StringComparison.Ordinal)).ToList();
		return (groups, extensions);
	}

	/// <summary>Removes ids that are already covered by a selected group (so we don't run them twice).</summary>
	private static List<string> ExcludeGroupMembers(IEnumerable<string> ids, IEnumerable<string> selectedGroups,
		Dictionary<string, string[]> groupsMap)
	{
		var members = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var g in selectedGroups ?? Enumerable.Empty<string>())
			if (groupsMap != null && groupsMap.TryGetValue(g, out var ids2) && ids2 != null)
				foreach (var id in ids2)
					members.Add(id);

		return (ids ?? Enumerable.Empty<string>()).Where(id => !members.Contains(id)).ToList();
	}

	/// <summary>Logs the discovered ids so scripted (quiet) users can see what's available to pass explicitly.</summary>
	private static void LogDiscovered(string label, List<string> ids)
	{
		if (ids == null || ids.Count == 0) return;
		const int cap = 15;
		var shown = string.Join(", ", ids.Take(cap));
		if (ids.Count > cap) shown += $", … (+{ids.Count - cap} more)";
		Log.Information($"Discovered {ids.Count} {label}(s): {shown}");
	}

	public override async Task<LocalStackInitCommandResult> GetResult(LocalStackInitCommandArgs args)
	{
		// Prompt only when we actually have an interactive console; otherwise fall back to defaults.
		var quiet = args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive;
		var defaults = new LocalStackTemplate.Options();

		// Where the manifest lives. With no workspace this defaults to `<cwd>/.beamable/local-stack.json` — a real
		// workspace folder rather than a loose file the rest of the CLI would not recognise as one.
		var defaultPath = LocalStackCommand.ResolveManifestPathForInit(args.ConfigService, args.configPath);
		var path = Path.GetFullPath(Ask("Where is the manifest?",
			args.configPath, defaultPath, quiet, allowEmpty: false));

		// Create the containing folder if it is not there yet. Adding a `.beamable` directory to someone's project
		// is a visible change, so an interactive run says what it is about to do and asks; a quiet run just does it.
		var created = LocalStackCommand.EnsureManifestDirectory(path,
			confirm: dir => quiet || AnsiConsole.Confirm(
				$"[green]{dir}[/] does not exist yet. Create it?", defaultValue: true),
			out var manifestDir);

		if (!created)
			throw new CliException($"Aborted — {manifestDir} was not created, so there is nowhere to write the manifest.");

		// Discover the microservices/portal extensions/service groups in the current .beamable workspace so they
		// can be offered as defaults (empty when run outside a workspace).
		var (discoveredServices, discoveredExtensions, discoveredGroups) = await DiscoverWorkspaceServices(args);

		// Update-only mode: rewrite just the microservice/extension/group steps of an existing manifest.
		if (args.updateServices)
		{
			// This path never prompts for repo paths, so take the portal checkout from the manifest being updated
			// and scan it too. Without this, `--update-services` — the command you reach for precisely when you
			// want to ADD extensions — could only offer what the workspace it runs in happens to know about.
			var existingPortalDir = File.Exists(path)
				? NullIfEmpty(LocalStackConfigIO.Load(path)?.repos?.portalDir)
				: null;

			var (updateExtensions, updateGroups) = MergePortalExtensions(
				existingPortalDir, discoveredExtensions, discoveredGroups);

			return UpdateServices(args, path, quiet, discoveredServices, updateExtensions, updateGroups);
		}

		if (File.Exists(path) && !args.force)
		{
			if (quiet)
				throw new CliException($"A manifest already exists at {path}. Pass --force to overwrite, or edit it directly.");
			if (!AnsiConsole.Confirm($"[yellow]{path}[/] already exists. Overwrite it?", defaultValue: false))
				throw new CliException("Aborted — a manifest already exists.");
		}

		// Repo paths — auto-detected by searching parent folders (up to 3 levels) for the well-known repo names,
		// shown as the default; leaving empty writes an <EDIT: ...> placeholder.
		var startDir = args.ConfigService?.WorkingDirectory ?? Directory.GetCurrentDirectory();
		var apiDir = Ask("Absolute path to the [green]BeamableAPI[/] (C# gateway) repo [grey](empty = placeholder)[/]:",
			args.apiDir, FindRepoDir(startDir, "BeamableAPI"), quiet, allowEmpty: true);
		var scalaDir = Ask("Absolute path to the [green]BeamableBackend[/] (Scala) repo [grey](empty = placeholder)[/]:",
			args.scalaDir, FindRepoDir(startDir, "BeamableBackend"), quiet, allowEmpty: true);
		var portalDir = Ask("Absolute path to the [green]portal frontend[/] repo [grey](empty = placeholder)[/]:",
			args.portalDir, FindRepoDir(startDir, "agentic-portal"), quiet, allowEmpty: true);

		// Local web package registry — the steps are always written, and this is the standing choice of whether
		// `beam local up` runs them. It lives in the manifest precisely so it survives until the next `init`:
		// before this, the only way to skip the registry was to remember --no-web-registry on every `up`.
		var webRegistry = ResolveWebRegistryChoice(args, quiet);
		var webRegistryDir = ResolveWebRegistryDir(args, startDir, quiet, webRegistry);

		// Endpoints (defaults — Enter accepts).
		var host = Ask("Backend API [green]host[/]:", args.host, defaults.host, quiet, allowEmpty: false);
		var portalUrl = Ask("[green]Portal[/] frontend URL:", args.portalUrl, defaults.portalUrl, quiet, allowEmpty: false);

		// The toolchain is NOT prompted for. `beam local setup` is what installs the JDK (and Maven, the .NET SDK
		// and Node), and it records where it put them — so init simply adopts that. Asking here was the wrong way
		// round: you had to name a Java 8 home at `init` time that only `setup` could produce, which forced people
		// to run the commands in an order that could not work on a fresh machine.
		//
		// Null when setup has not run yet: the manifest then carries no toolchain, and `beam local up` falls back
		// to machine-level Java discovery exactly as before. Re-running `beam local setup` after `init` fills it in.
		var toolchain = ToolchainService.TryReadInstalled();
		if (toolchain != null)
			Log.Information($"Using the toolchain installed by `beam local setup` at {toolchain.dir}.");
		else
			Log.Information("No toolchain found — run `beam local setup` to install one, then re-run this command (or `beam local setup` again) to wire it in.");

		// Scala services: auto-discover the tools/* services from the repo (name + main class) and default to
		// the curated set that's actually present; fall back to the static list when nothing is discovered.
		var discovered = LocalStackTemplate.DiscoverScalaTools(NullIfEmpty(scalaDir));
		var defaultScalaNames = ResolveDefaultScalaNames(discovered);
		if (discovered.Count > 0)
			Log.Information($"Discovered {discovered.Count} Scala tools under {scalaDir}.");

		(discoveredExtensions, discoveredGroups) =
			MergePortalExtensions(NullIfEmpty(portalDir), discoveredExtensions, discoveredGroups);

		LogDiscovered("microservice", discoveredServices);
		LogDiscovered("portal extension", discoveredExtensions);
		LogDiscovered("group", discoveredGroups.Keys.OrderBy(x => x).ToList());

		// Scala services default to the curated/discovered set (small, known-good). Microservices and extensions
		// are opt-in: discovered ids are offered to pick from, but nothing is selected by default (a workspace can
		// have dozens of extensions — running them all is rarely what you want).
		var discoveredNames = discovered.Select(t => t.name).ToList();
		var scalaServices = AskScalaSelection("Select the [green]Scala[/] services to run:",
			args.scalaServices, discoveredNames, defaultScalaNames, quiet);
		var selectedServices = Split(AskServiceSelection("Select the [green]microservices[/] to run:",
			args.services, discoveredServices, preselected: null, quiet, quietDefault: string.Empty)) ?? new List<string>();
		// The extension picker also lists service groups (prefixed "GROUP "); selecting a group runs all its members.
		var (selectedGroups, selectedExtensions) = AskExtensionSelection("Select the [green]portal extensions / groups[/] to run:",
			args.extensions, discoveredGroups, discoveredExtensions,
			preselectedGroups: null, preselectedExtensions: null, quiet, quietGroups: null, quietExtensions: null);

		// Don't run an id individually if a selected group already covers it.
		var options = new LocalStackTemplate.Options
		{
			host = host,
			portalUrl = portalUrl,
			apiDir = NullIfEmpty(apiDir),
			scalaDir = NullIfEmpty(scalaDir),
			portalDir = NullIfEmpty(portalDir),
			scalaTools = ToScalaTools(Split(scalaServices), discovered),
			services = ExcludeGroupMembers(selectedServices, selectedGroups, discoveredGroups),
			extensions = ExcludeGroupMembers(selectedExtensions, selectedGroups, discoveredGroups),
			groups = selectedGroups,
			toolchain = toolchain,
			javaHome = toolchain?.java,
			scalaJvmArgs = args.scalaJvmArgs ?? defaults.scalaJvmArgs,
			// Always emit the steps, so flipping the choice later — `beam local up --with-web-registry`, or a
			// re-run of this command — needs no hand-editing of the manifest.
			includeWebRegistry = true,
			webRegistry = webRegistry,
			webRegistryDir = NullIfEmpty(webRegistryDir),
		};

		var config = LocalStackTemplate.Create(options);
		LocalStackConfigIO.Save(path, config);
		EnsureGitignore(Path.GetDirectoryName(path));

		Log.Information($"Wrote local-stack manifest to {path} ({config.steps.Count} steps).");
		var skillPath = WriteSkill(args, path, config);
		Log.Information("Edit any <EDIT: ...> paths, then run: beam local up");

		return new LocalStackInitCommandResult
		{
			manifestPath = path,
			stepCount = config.steps.Count,
			created = true,
			skillPath = skillPath
		};
	}

	/// <summary>
	/// Rewrites only the microservice/extension/group steps of an existing manifest, leaving every other step
	/// (docker, gateway, Scala, portal) and all edits untouched. The prompts default to the manifest's current
	/// selection, or — when it has none — the ids discovered in the workspace. An empty answer removes all steps
	/// of that kind.
	/// </summary>
	private LocalStackInitCommandResult UpdateServices(LocalStackInitCommandArgs args, string path, bool quiet,
		List<string> discoveredServices, List<string> discoveredExtensions, Dictionary<string, string[]> discoveredGroups)
	{
		if (!File.Exists(path))
			throw new CliException($"No manifest at {path} to update. Run `beam local init` first.");

		var config = LocalStackConfigIO.Load(path);

		bool IsMicroservice(LocalStackStep s) => s.name?.StartsWith(LocalStackTemplate.MicroservicePrefix) == true;
		bool IsExtension(LocalStackStep s) => s.name?.StartsWith(LocalStackTemplate.ExtensionPrefix) == true;
		bool IsGroup(LocalStackStep s) => s.name?.StartsWith(LocalStackTemplate.GroupPrefix) == true;

		var currentServiceList = config.steps.Where(IsMicroservice)
			.Select(s => s.name.Substring(LocalStackTemplate.MicroservicePrefix.Length)).ToList();
		var currentExtensionList = config.steps.Where(IsExtension)
			.Select(s => s.name.Substring(LocalStackTemplate.ExtensionPrefix.Length)).ToList();
		var currentGroupList = config.steps.Where(IsGroup)
			.Select(s => s.name.Substring(LocalStackTemplate.GroupPrefix.Length)).ToList();

		// Offer the manifest's current ids plus anything discovered in the workspace; preselect what's already in
		// the manifest so an empty answer keeps the current set (in quiet mode it is kept verbatim).
		var serviceChoices = currentServiceList.Union(discoveredServices, StringComparer.OrdinalIgnoreCase).ToList();
		var extensionChoices = currentExtensionList.Union(discoveredExtensions, StringComparer.OrdinalIgnoreCase).ToList();
		// Groups offered = discovered ∪ current (so an already-configured group stays selectable even if not discovered).
		var groupChoices = new Dictionary<string, string[]>(discoveredGroups ?? new Dictionary<string, string[]>(), StringComparer.OrdinalIgnoreCase);
		foreach (var g in currentGroupList) groupChoices.TryAdd(g, Array.Empty<string>());

		var services = Split(AskServiceSelection("Select the [green]microservices[/] to run:",
			args.services, serviceChoices, preselected: currentServiceList, quiet,
			quietDefault: string.Join(" ", currentServiceList))) ?? new List<string>();
		var (selectedGroups, selectedExtensions) = AskExtensionSelection("Select the [green]portal extensions / groups[/] to run:",
			args.extensions, groupChoices, extensionChoices,
			preselectedGroups: currentGroupList, preselectedExtensions: currentExtensionList,
			quiet, quietGroups: currentGroupList, quietExtensions: currentExtensionList);

		var finalServices = ExcludeGroupMembers(services, selectedGroups, discoveredGroups);
		var finalExtensions = ExcludeGroupMembers(selectedExtensions, selectedGroups, discoveredGroups);

		// Drop the old beam steps and append the new set (microservices, then extensions, then groups).
		config.steps.RemoveAll(s => IsMicroservice(s) || IsExtension(s) || IsGroup(s));
		foreach (var svc in finalServices)
			config.steps.Add(LocalStackTemplate.MicroserviceStep(svc));
		foreach (var ext in finalExtensions)
			config.steps.Add(LocalStackTemplate.ExtensionStep(ext));
		foreach (var group in selectedGroups)
			config.steps.Add(LocalStackTemplate.GroupStep(group));

		LocalStackConfigIO.Save(path, config);

		Log.Information($"Updated microservice/extension/group steps in {path} ({config.steps.Count} steps total).");
		// Regenerate the skill here too when --skill is passed: the service selection it lists just changed, and
		// the doc is rendered from the manifest, so this path needs nothing the full init has.
		var skillPath = WriteSkill(args, path, config);

		return new LocalStackInitCommandResult
		{
			manifestPath = path,
			stepCount = config.steps.Count,
			created = false,
			skillPath = skillPath
		};
	}
}
