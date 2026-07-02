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
}

public class LocalStackInitCommandResult
{
	public string manifestPath;
	public int stepCount;
	public bool created;
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
	}

	private static List<string> Split(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

	private static string NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

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

		// Where the manifest lives.
		var defaultPath = LocalStackCommand.ResolveManifestPath(args.ConfigService, args.configPath);
		var path = Path.GetFullPath(Ask("Where is the manifest?",
			args.configPath, defaultPath, quiet, allowEmpty: false));

		// Discover the microservices/portal extensions/service groups in the current .beamable workspace so they
		// can be offered as defaults (empty when run outside a workspace).
		var (discoveredServices, discoveredExtensions, discoveredGroups) = await DiscoverWorkspaceServices(args);

		// Update-only mode: rewrite just the microservice/extension/group steps of an existing manifest.
		if (args.updateServices)
			return UpdateServices(args, path, quiet, discoveredServices, discoveredExtensions, discoveredGroups);

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

		// Endpoints (defaults — Enter accepts).
		var host = Ask("Backend API [green]host[/]:", args.host, defaults.host, quiet, allowEmpty: false);
		var portalUrl = Ask("[green]Portal[/] frontend URL:", args.portalUrl, defaults.portalUrl, quiet, allowEmpty: false);

		// Scala services: auto-discover the tools/* services from the repo (name + main class) and default to
		// the curated set that's actually present; fall back to the static list when nothing is discovered.
		var discovered = LocalStackTemplate.DiscoverScalaTools(NullIfEmpty(scalaDir));
		var defaultScalaNames = ResolveDefaultScalaNames(discovered);
		if (discovered.Count > 0)
			Log.Information($"Discovered {discovered.Count} Scala tools under {scalaDir}.");

		LogDiscovered("microservice", discoveredServices);
		LogDiscovered("portal extension", discoveredExtensions);
		LogDiscovered("group", discoveredGroups.Keys.OrderBy(x => x).ToList());

		// Scala services default to the curated/discovered set (small, known-good). Microservices and extensions
		// are opt-in: discovered ids are offered to pick from, but nothing is selected by default (a workspace can
		// have dozens of extensions — running them all is rarely what you want).
		var scalaServices = Ask("[green]Scala[/] services to run [grey](space separated)[/]:",
			args.scalaServices, string.Join(" ", defaultScalaNames), quiet, allowEmpty: false);
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
		};

		var config = LocalStackTemplate.Create(options);
		LocalStackConfigIO.Save(path, config);
		EnsureGitignore(Path.GetDirectoryName(path));

		Log.Information($"Wrote local-stack manifest to {path} ({config.steps.Count} steps).");
		Log.Information("Edit any <EDIT: ...> paths, then run: beam local up");

		return new LocalStackInitCommandResult
		{
			manifestPath = path,
			stepCount = config.steps.Count,
			created = true
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

		return new LocalStackInitCommandResult
		{
			manifestPath = path,
			stepCount = config.steps.Count,
			created = false
		};
	}
}
