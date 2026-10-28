using Beamable.Server;
using cli.Services.LocalStack;
using cli.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spectre.Console;
using System.CommandLine;

namespace cli.Commands.LocalStack;

public class LocalStackCreateCommandArgs : CommandArgs
{
	/// <summary><c>&lt;stack&gt;</c> for a stack, <c>&lt;stack&gt;:&lt;variant&gt;</c> for a variant; null to be asked.</summary>
	public string name;

	public bool shared;
	public string file;
	public string description;

	/// <summary>The backend it runs against: a target's name or an http(s) address; empty to inherit.</summary>
	public string target;

	// A stack.
	public string extends;
	public string include;
	public string exclude;

	/// <summary>This workspace's microservices, portal extensions and service groups the stack runs, by id.</summary>
	public string services;
	public string extensions;
	public string groups;

	/// <summary>A command of the stack's own, described by the <c>--step-*</c> options as for <c>beam local setup --add-step</c>.</summary>
	public LocalStackSetupCommandArgs command = new LocalStackSetupCommandArgs();

	/// <summary>
	/// Every command of the stack's own as a JSON array, replacing the ones it has (an empty array removes them). Each
	/// item takes what the <c>--step-*</c> options do (<c>ReplaceCommands</c> reads it).
	/// </summary>
	public string commands;

	/// <summary>With <c>--add-step</c>: the command runs before every other step.</summary>
	public bool stepFirst;

	// A variant.
	public string before;
	public string after;
	public string remove;
}

public class LocalStackCreateCommandResult
{
	/// <summary>The stack file written; null when nothing was.</summary>
	public string path;

	public string name;

	/// <summary>The steps added because something the stack runs needs them, with why.</summary>
	public List<string> added;
}

/// <summary>Where a question sequence goes after one answer.</summary>
public enum LocalStackWizardMove
{
	Next,
	Back
}

/// <summary>
/// A sequence of questions where any answer can be revisited: each question can step back to the previous one that
/// applies, and a question already answered (or given as a flag) is skipped on the first pass and can be asked again
/// later from a review screen.
/// </summary>
public class LocalStackWizard
{
	public class Question
	{
		/// <summary>The review screen's name for the answer this question (and the ones after it in the same group) give.</summary>
		public string group;

		/// <summary>Whether the question makes sense given the answers so far.</summary>
		public Func<bool> applies = () => true;

		/// <summary>Asks; the argument says whether "back" may be offered. Only an answer moving <c>Next</c> is kept.</summary>
		public Func<bool, LocalStackWizardMove> ask;

		/// <summary>True once answered, or when a flag gave the answer.</summary>
		public bool answered;
	}

	public readonly List<Question> questions = new List<Question>();

	/// <summary>Called before each question is shown, so the screen can be redrawn for it.</summary>
	public Action beforeEach;

	/// <summary>The group of the question being asked, set before <see cref="beforeEach"/> so the screen can name it.</summary>
	public string current;

	/// <summary>
	/// Asks every applicable question from <paramref name="from"/> on that is not answered yet, or every applicable one
	/// of <paramref name="onlyGroup"/> when set. Back goes to the previous applicable question in the same pass; back
	/// from the first returns false, so the caller can go back to where it came from.
	/// </summary>
	public bool Run(int from = 0, string onlyGroup = null)
	{
		bool InPass(int index) =>
			questions[index].applies()
			&& (onlyGroup != null ? questions[index].group == onlyGroup : !questions[index].answered);

		// The questions asked in this pass, so back returns to the one really asked before, even though it now
		// counts as answered.
		var asked = new Stack<int>();
		var i = from;
		while (i < questions.Count)
		{
			if (!InPass(i) && !asked.Contains(i))
			{
				i++;
				continue;
			}

			current = questions[i].group;
			beforeEach?.Invoke();
			var move = questions[i].ask(asked.Count > 0 || onlyGroup != null);
			if (move == LocalStackWizardMove.Back)
			{
				if (asked.Count == 0) return false;
				i = asked.Pop();
				questions[i].answered = false;
				continue;
			}

			questions[i].answered = true;
			asked.Push(i);
			i++;
		}

		return true;
	}

	/// <summary>The groups whose applicable questions are all answered, in order: what the screen shows above a question.</summary>
	public List<string> AnsweredGroups() =>
		questions.Where(q => q.applies() && q.group != null)
			.GroupBy(q => q.group)
			.Where(g => g.All(q => q.answered))
			.Select(g => g.Key)
			.ToList();

	/// <summary>The groups the review screen can revisit, in order, among the questions that apply.</summary>
	public List<string> Groups() =>
		questions.Where(q => q.applies() && q.group != null).Select(q => q.group).Distinct().ToList();

	/// <summary>Asks every applicable question that hasn't been answered yet (after a change made others apply).</summary>
	public void AskUnanswered() => Run();
}

/// <summary>
/// Writes a stack, or a variant on an existing stack, without hand-editing JSON. It follows the name grammar of
/// <c>beam local run</c>: <c>&lt;stack&gt;</c> creates a stack, <c>&lt;stack&gt;:&lt;variant&gt;</c> a variant.
///
/// In an interactive terminal it asks for whatever the flags didn't give, the way <c>beam local setup</c> does:
/// what to create, who it is for, and each list, picked from one list of this machine's steps grouped by tag (a
/// whole group is written as its tag). It works like a wizard: each question replaces the last on the screen, with
/// the answers so far above it, and Shift+Tab goes back. The end is a review of all the answers and the steps the
/// result runs, from which any answer can be changed before writing. With <c>--quiet</c>, or without a terminal,
/// only the flags count.
///
/// The entry goes to <c>.beamable/local/stacks/</c>, or <c>.beamable/shared/stacks/</c> when shared. A variant on a
/// stack from another layer (a personal variant on a shared or built-in stack) is written as a merge entry in the
/// target folder; the original file isn't touched. The result is resolved exactly as <c>beam local run --dry-run</c>
/// would before anything is written, and nothing is written when that fails.
///
/// <see cref="LocalStackEditCommand"/> is the same flow on an entry that already exists.
/// </summary>
public class LocalStackCreateCommand
	: AtomicCommand<LocalStackCreateCommandArgs, LocalStackCreateCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	// It prints its own readable output; the result object still reaches tools through the data channel.
	public override bool AutoLogOutput => false;

	public LocalStackCreateCommand() : base("create",
		"Write a stack, or a variant with <stack>:<variant>, to the local stacks folder (or the shared one with --shared), checked against this machine's settings first; asks for what isn't passed")
	{
	}

	protected LocalStackCreateCommand(string name, string description) : base(name, description)
	{
	}

	/// <summary>True for <c>beam local edit</c>: the entry exists, and its answers start from what it says.</summary>
	protected virtual bool Editing => false;

	public override void Configure()
	{
		AddArgument(new Argument<string>("name", () => null, Editing
				? "The stack to edit, or <stack>:<variant> for one variant. Leave it out to pick one"
				: "The stack to create, or <stack>:<variant> for a variant on an existing stack. Leave it out to be asked"),
			(args, v) => args.name = v);
		AddOption(new Option<bool>("--shared", Editing
				? "Edit the entry in .beamable/shared/stacks/, which is committed, instead of .beamable/local/stacks/"
				: "Write to .beamable/shared/stacks/, which is committed, instead of .beamable/local/stacks/; a description is then required"),
			(args, v) => args.shared = v);
		if (!Editing)
			AddOption(new Option<string>("--file", "The file in the stacks folder to write to (default: <stack>.json, or the file that already defines the stack)"),
				(args, v) => args.file = v);
		AddOption(new Option<string>("--description", "What the stack or variant is for; shown by `beam local run`"),
			(args, v) => args.description = v);
		AddOption(new Option<string>("--target", "The backend it runs against: local, lan, dev, staging, prod, a target from the settings, or an http(s) address (default: what it extends or changes runs against)"),
			(args, v) => args.target = v);
		AddOption(new Option<string>("--extends", "A stack: another stack to start from"),
			(args, v) => args.extends = v);
		AddOption(new Option<string>("--include", "A stack: comma separated tags or step names to add"),
			(args, v) => args.include = v);
		AddOption(new Option<string>("--exclude", "A stack: comma separated tags or step names to take out"),
			(args, v) => args.exclude = v);
		AddOption(new Option<string>("--services", "A stack: comma separated microservices of this workspace to run (`beam project list` names them)"),
			(args, v) => args.services = v);
		AddOption(new Option<string>("--extensions", "A stack: comma separated portal extensions of this workspace to run"),
			(args, v) => args.extensions = v);
		AddOption(new Option<string>("--groups", "A stack: comma separated service groups of this workspace to run (every microservice and extension in them)"),
			(args, v) => args.groups = v);

		// A command of the stack's own; several are added one per call, or with the questions.
		AddOption(new Option<string>("--add-step", "A stack: add a command of its own with this name, described by the --step-* options"),
			(args, v) => args.command.addStep = v);
		AddOption(new Option<string>("--commands",
				"A stack: all its own commands as a JSON array, replacing the ones it has ([] removes them). Each item takes what the --step-* options do: " +
				"{\"name\", \"command\" + \"args\" | \"shell\" + \"shellKind\" | \"beam\", \"dir\", \"env\": [\"KEY=VALUE\"], \"once\", \"readyUrl\", \"readyLog\", \"port\", \"timeout\", \"label\", \"tags\", \"after\", \"first\"}"),
			(args, v) => args.commands = v);
		AddOption(new Option<string>("--step-command", "Own command: the program to run (with --step-args)"), (args, v) => args.command.stepCommand = v);
		AddOption(new Option<string>("--step-args", "Own command: the program's arguments"), (args, v) => args.command.stepArgs = v);
		AddOption(new Option<string>("--step-shell", "Own command: a shell script to run instead of a program"), (args, v) => args.command.stepShell = v);
		AddOption(new Option<string>("--step-shell-kind", "Own command: the shell for --step-shell, sh or powershell"), (args, v) => args.command.stepShellKind = v);
		AddOption(new Option<string>("--step-beam", "Own command: a beam command to run with this CLI, such as \"content publish\""), (args, v) => args.command.stepBeam = v);
		AddOption(new Option<string>("--step-dir", "Own command: the folder it runs in; may use variables such as ${PortalPath}"), (args, v) => args.command.stepDir = v);
		AddOption(new Option<string[]>("--step-env", "Own command: an environment value, KEY=VALUE; repeat for more") { AllowMultipleArgumentsPerToken = true },
			(args, v) => args.command.stepEnv = v);
		AddOption(new Option<bool>("--step-once", "Own command: it runs to completion instead of staying up"), (args, v) => args.command.stepOnce = v);
		AddOption(new Option<string>("--step-ready-url", "Own command: it is ready once this URL answers"), (args, v) => args.command.stepReadyUrl = v);
		AddOption(new Option<string>("--step-ready-log", "Own command: it is ready once its output contains this text"), (args, v) => args.command.stepReadyLog = v);
		AddOption(new Option<int>("--step-port", "Own command: the port it binds, checked before it starts"), (args, v) => args.command.stepPort = v);
		AddOption(new Option<int>("--step-timeout", () => 120, "Own command: seconds to wait for it to be ready"), (args, v) => args.command.stepTimeout = v);
		AddOption(new Option<string>("--step-label", "Own command: a readable name for listings"), (args, v) => args.command.stepLabel = v);
		AddOption(new Option<string>("--step-after", "Own command: the step it runs after (default: after every other step)"), (args, v) => args.command.stepAfter = v);
		AddOption(new Option<bool>("--step-first", "Own command: it runs before every other step"), (args, v) => args.stepFirst = v);
		AddOption(new Option<string>("--before", "A variant: comma separated tags or step names to run before the stack's own steps"),
			(args, v) => args.before = v);
		AddOption(new Option<string>("--after", "A variant: comma separated tags or step names to start once the stack's own steps are ready"),
			(args, v) => args.after = v);
		AddOption(new Option<string>("--remove", "A variant: comma separated tags or step names of the stack to turn off"),
			(args, v) => args.remove = v);
	}

	/// <summary>Splits on commas only, since step names contain spaces.</summary>
	private static List<string> List(string value) =>
		string.IsNullOrWhiteSpace(value)
			? null
			: value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

	/// <summary>Everything a stack or variant is made of, as the flags or the questions gave it.</summary>
	private class Draft
	{
		public string stack;
		public string variant;
		public bool shared;
		public string description;
		public string target;
		public string extends;
		public List<string> include;
		public List<string> exclude;

		/// <summary>The narrowing of extension steps that isn't about the stack's own services: kept from the entry.</summary>
		public List<string> extensions;

		// What the stack runs from this workspace, by id, and its own commands.
		public List<string> services = new List<string>();
		public List<string> portalExtensions = new List<string>();
		public List<string> groups = new List<string>();
		public List<LocalStackStep> commands = new List<LocalStackStep>();

		/// <summary>The steps added because something the stack runs needs them, with why.</summary>
		public List<string> added = new List<string>();

		public WorkspaceServices workspace = new WorkspaceServices();
		public HashSet<string> changes = new HashSet<string>();
		public List<string> before;
		public List<string> after;
		public List<string> remove;

		// An edit: the file holding the entry, the entry as it was, and whether a lower layer also defines the stack
		// (then the entry only changes that stack, and needs nothing to run of its own).
		public string editFile;
		public LocalStackStackDef original;
		public bool inherits;

		public bool IsVariant => variant != null;
		public bool HasServices => services.Count + portalExtensions.Count + groups.Count > 0;
		public bool IsEdit => editFile != null;
		public string FullName => IsVariant ? $"{stack}:{variant}" : stack;
		public LocalStackLayer Layer => shared ? LocalStackLayer.Shared : LocalStackLayer.Local;
		public string Verb => IsEdit ? "edit" : "create";
	}

	private const string ChangeBefore = "Run more steps before the stack's own steps (builds, a dependency, a data seed)";
	private const string ChangeAfter = "Start more steps once the stack is up (a watcher, a smoke test, another service)";
	private const string ChangeRemove = "Turn off some of the stack's steps";

	/// <summary>Shown on every question that can go back to the previous one.</summary>
	private const string BackHint = "[grey](Shift+Tab goes back)[/]";

	public override async Task<LocalStackCreateCommandResult> GetResult(LocalStackCreateCommandArgs args)
	{
		var quiet = args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive;
		var settingsPath = LocalStackCommand.ResolveManifestPath(args.ConfigService, null);
		if (!File.Exists(settingsPath))
			throw new CliException($"No settings at {settingsPath} yet. Run `beam local setup` first: a stack is checked against this machine's steps before it is written.");

		var config = LocalStackConfigIO.Load(settingsPath);
		var root = LocalStackConfigIO.FindWorkspaceRoot(settingsPath);
		var stacks = LoadStacks(root);

		var draft = new Draft
		{
			shared = args.shared,
			description = args.description,
			target = string.IsNullOrWhiteSpace(args.target) ? null : args.target.Trim().TrimEnd('/'),
			extends = args.extends,
			include = List(args.include),
			exclude = List(args.exclude),
			services = List(args.services) ?? new List<string>(),
			portalExtensions = List(args.extensions) ?? new List<string>(),
			groups = List(args.groups) ?? new List<string>(),
			before = List(args.before),
			after = List(args.after),
			remove = List(args.remove),
		};
		if (draft.before != null) draft.changes.Add(ChangeBefore);
		if (draft.after != null) draft.changes.Add(ChangeAfter);
		if (draft.remove != null) draft.changes.Add(ChangeRemove);
		draft.workspace = await WorkspaceServices.AddFound(args, config);

		if (Editing)
		{
			LoadForEdit(draft, args, stacks, quiet);

			if (quiet && args.description == null && args.target == null && args.extends == null && args.include == null && args.exclude == null
			    && args.services == null && args.extensions == null && args.groups == null && args.command.addStep == null && args.commands == null
			    && args.before == null && args.after == null && args.remove == null)
				throw new CliException($"Pass what to change in {draft.FullName}: --description, --target, and --extends, --include, --exclude, --services, --extensions, --groups, --add-step for a stack, or --before, --after, --remove for a variant (an empty value clears it). In an interactive terminal it shows the entry and asks.");
		}
		else if (!string.IsNullOrWhiteSpace(args.name))
		{
			var parts = args.name.Split(':');
			if (parts.Length > 2 || parts.Any(string.IsNullOrWhiteSpace))
				throw new CliException($"'{args.name}' is not a name to create. Use <stack> for a stack or <stack>:<variant> for one variant.");

			draft.stack = parts[0].Trim();
			draft.variant = parts.Length == 2 ? parts[1].Trim() : null;
		}
		else if (quiet)
		{
			throw new CliException("Pass what to create: `beam local create <stack>` for a stack, `beam local create <stack>:<variant>` for a variant. In an interactive terminal it is asked.");
		}

		if (args.commands != null)
			ReplaceCommands(draft, config, args.commands);
		if (args.command.addStep != null)
			AddCommand(draft, config, args);
		CheckServices(draft);

		if (quiet)
		{
			foreach (var note in AddDependencies(draft, stacks, config, root))
				Log.Information(note);

			var (file, entry) = Build(draft, stacks, root);
			try
			{
				stacks.ResolveDraft(draft.FullName, draft.Layer, file, entry, config, replace: draft.IsEdit);
			}
			catch (LocalStackStackException e)
			{
				throw new CliException($"Nothing was written. {e.Message}");
			}

			if (draft.IsEdit && !WriteEdit(file, draft, entry))
			{
				Log.Information($"Nothing was changed: {draft.FullName} already says that.");
				return new LocalStackCreateCommandResult { name = draft.FullName };
			}

			if (!draft.IsEdit) Write(file, draft.stack, draft.variant, entry);
			Log.Information($"{(draft.IsEdit ? "Saved" : "Wrote")} {draft.FullName} {(draft.IsEdit ? "in" : "to")} {file}. Check it with `beam local run {draft.FullName} --dry-run`.");
			return new LocalStackCreateCommandResult { path = file, name = draft.FullName, added = draft.added };
		}

		var wizard = Questions(draft, stacks, config, args);
		wizard.beforeEach = () => Frame(draft, wizard);

		LocalStackCreateCommandResult result = null;
		InWizardScreen(() =>
		{
			wizard.Run();
			result = Review(wizard, draft, stacks, config, root);
		});

		// Back on the normal screen: what came of it stays in the terminal's history.
		if (result.path == null)
		{
			Log.Information(draft.IsEdit ? "Nothing was changed." : "Nothing was written.");
			return result;
		}

		foreach (var group in wizard.Groups())
			AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(group)}:[/] {Markup.Escape(Summary(group, draft))}");
		foreach (var note in draft.added)
			AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(note)}[/]");
		Log.Information($"{(draft.IsEdit ? "Saved" : "Wrote")} {draft.FullName} {(draft.IsEdit ? "in" : "to")} {result.path}. Check it with `beam local run {draft.FullName} --dry-run`.");
		result.added = draft.added;
		return result;
	}

	/// <summary>The names of the steps an entry defines that the questions manage: its services and its own commands.</summary>
	private static List<string> OwnStepNames(LocalStackStackDef def) =>
		(def?.steps ?? new Dictionary<string, LocalStackStep>())
		.Where(kv => IsServiceStep(kv.Key) || kv.Value?.custom == true)
		.Select(kv => kv.Key)
		.ToList();

	private static bool IsServiceStep(string name) =>
		new[] { LocalStackTemplate.MicroservicePrefix, LocalStackTemplate.ExtensionPrefix, LocalStackTemplate.GroupPrefix }
			.Any(prefix => name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// The steps the draft's services and commands become, by name. A service runs with <c>beam project run</c>, tagged
	/// <c>microservices</c> or <c>extensions</c> (a group both) so a variant can turn them off by kind; an extension (or a group) depends on the microservices
	/// its package.json says it calls, so they are added when missing.
	/// </summary>
	private static Dictionary<string, LocalStackStep> OwnSteps(Draft draft)
	{
		var steps = new Dictionary<string, LocalStackStep>(StringComparer.OrdinalIgnoreCase);
		void Put(LocalStackStep step, IEnumerable<string> calls)
		{
			var name = step.name;
			step.name = null; // the key names it
			step.tags = LocalStackStacks.ServiceTagsFor(name);
			var needs = calls.Select(c => LocalStackTemplate.MicroservicePrefix + c).ToList();
			step.dependsOn = needs.Count > 0 ? needs : null;
			steps[name] = step;
		}

		var workspace = draft.workspace;
		foreach (var group in draft.groups)
		{
			var members = workspace.groups.TryGetValue(group, out var m) ? m : Array.Empty<string>();
			Put(LocalStackTemplate.GroupStep(group), members.SelectMany(workspace.CallsOf).Where(c => !members.Contains(c, StringComparer.OrdinalIgnoreCase)).Distinct());
		}

		foreach (var service in draft.services) Put(LocalStackTemplate.MicroserviceStep(service), Array.Empty<string>());
		foreach (var extension in draft.portalExtensions) Put(LocalStackTemplate.ExtensionStep(extension), workspace.CallsOf(extension));
		foreach (var command in draft.commands)
		{
			var copy = JsonConvert.DeserializeObject<LocalStackStep>(JsonConvert.SerializeObject(command));
			var name = copy.name;
			copy.name = null;
			steps[name] = copy;
		}

		return steps;
	}

	/// <summary><paramref name="include"/> with the stack's own steps named in it, so they run whatever else it selects.</summary>
	private static List<string> WithOwn(List<string> include, IEnumerable<string> own)
	{
		var all = (include ?? new List<string>()).Concat(own.Where(o => include?.Contains(o, StringComparer.OrdinalIgnoreCase) != true)).ToList();
		return all.Count > 0 ? all : include;
	}

	/// <summary>
	/// The extension narrowing to write: what the entry already narrowed to, and the stack's own services. With
	/// services picked, the catalogue's other extension steps (from <c>setup --services</c>) stay out of the stack, as
	/// the shared stacks do it.
	/// </summary>
	private static List<string> Narrowing(Draft draft)
	{
		var ids = (draft.extensions ?? new List<string>()).Concat(draft.groups).Concat(draft.services).Concat(draft.portalExtensions)
			.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
		return ids.Count > 0 ? ids : null;
	}

	public const string GroupKey = "group:", ServiceKey = "microservice:", ExtensionKey = "extension:";

	/// <summary>The service picker's choices: groups first, then microservices, then extensions; what the draft already
	/// runs is offered even when this workspace doesn't have it.</summary>
	private static List<string> ServiceChoices(Draft draft)
	{
		var w = draft.workspace;
		return w.groups.Keys.Concat(draft.groups).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(g => g, StringComparer.OrdinalIgnoreCase).Select(g => GroupKey + g)
			.Concat(w.services.Concat(draft.services).Distinct(StringComparer.OrdinalIgnoreCase).Select(x => ServiceKey + x))
			.Concat(w.extensions.Concat(draft.portalExtensions).Distinct(StringComparer.OrdinalIgnoreCase).Select(x => ExtensionKey + x))
			.ToList();
	}

	private static List<string> PickedServices(Draft draft) =>
		draft.groups.Select(g => GroupKey + g).Concat(draft.services.Select(x => ServiceKey + x)).Concat(draft.portalExtensions.Select(x => ExtensionKey + x)).ToList();

	private static void SetServices(Draft draft, List<string> picked)
	{
		List<string> Of(string prefix) => picked.Where(p => p.StartsWith(prefix, StringComparison.Ordinal)).Select(p => p.Substring(prefix.Length)).ToList();
		draft.groups = Of(GroupKey);
		draft.services = Of(ServiceKey);
		draft.portalExtensions = Of(ExtensionKey);
	}

	private const string SectionKey = "\u0001section:";

	/// <summary>
	/// The service picker: one section each for service groups, microservices and portal extensions (ticking a section
	/// ticks all of it), one line per row whatever the terminal's width. A group says how many microservices and
	/// extensions it runs rather than naming them, and an extension which microservices it calls, cut to fit.
	/// </summary>
	private static List<string> PickServices(Draft draft, string title, bool back)
	{
		var choices = ServiceChoices(draft);
		var width = Math.Max(40, AnsiConsole.Profile.Width - 8);
		var sections = new[] { ("Service groups", GroupKey), ("Microservices", ServiceKey), ("Portal extensions", ExtensionKey) };
		var prompt = new MultiSelectionPrompt<string>()
			.Title(title)
			.PageSize(20)
			.Mode(SelectionMode.Leaf)
			.MoreChoicesText("[grey](move up/down to reveal more)[/]")
			.InstructionsText(back
				? "[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept, [blue]Shift+Tab[/] to go back)[/]"
				: "[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
			.UseConverter(key => key.StartsWith(SectionKey, StringComparison.Ordinal)
				? $"[bold]{Markup.Escape(key.Substring(SectionKey.Length))}[/]"
				: ServiceRow(draft.workspace, key, width))
			.AddBeamHightlight()
			.NotRequired();

		var current = PickedServices(draft);
		foreach (var (name, prefix) in sections)
		{
			var rows = choices.Where(c => c.StartsWith(prefix, StringComparison.Ordinal)).ToList();
			if (rows.Count == 0) continue;
			prompt.AddChoiceGroup($"{SectionKey}{name} ({rows.Count})", rows);
			foreach (var row in rows.Where(r => current.Contains(r, StringComparer.OrdinalIgnoreCase)))
				prompt.Select(row);
		}

		return Ask(prompt, back, out var picked) ? picked.Where(p => !p.StartsWith(SectionKey, StringComparison.Ordinal)).ToList() : null;
	}

	/// <summary>One picker row (markup), on one line of at most <paramref name="width"/> characters of text.</summary>
	public static string ServiceRow(WorkspaceServices w, string key, int width)
	{
		string Row(string name, string detail)
		{
			var room = width - name.Length - 2;
			if (string.IsNullOrEmpty(detail) || room < 8) return Markup.Escape(Fit(name, width));
			return $"{Markup.Escape(name)}  [grey]{Markup.Escape(Fit(detail, room))}[/]";
		}

		if (key.StartsWith(GroupKey, StringComparison.Ordinal))
		{
			var group = key.Substring(GroupKey.Length);
			if (!w.groups.TryGetValue(group, out var members)) return Row(group, "not in this workspace");
			var services = members.Count(m => w.services.Contains(m, StringComparer.OrdinalIgnoreCase));
			var extensions = members.Length - services;
			return Row(group, string.Join(", ", new[]
			{
				services > 0 ? $"{services} microservice{(services == 1 ? "" : "s")}" : null,
				extensions > 0 ? $"{extensions} extension{(extensions == 1 ? "" : "s")}" : null
			}.Where(x => x != null)));
		}

		if (key.StartsWith(ServiceKey, StringComparison.Ordinal))
			return Row(key.Substring(ServiceKey.Length), null);

		var extension = key.Substring(ExtensionKey.Length);
		var calls = w.CallsOf(extension).ToList();
		return Row(extension, calls.Count > 0 ? "calls " + string.Join(", ", calls) : null);
	}

	/// <summary><paramref name="text"/> cut to <paramref name="width"/> characters, ending in "..." when cut.</summary>
	private static string Fit(string text, int width) =>
		text.Length <= width ? text : text.Substring(0, Math.Max(1, width - 3)) + "...";

	/// <summary>Services named by flags that this workspace doesn't have. Unchecked when nothing was found, outside a
	/// workspace.</summary>
	private static void CheckServices(Draft draft)
	{
		var w = draft.workspace;
		if (!w.Any) return;
		var unknown = draft.services.Where(x => !w.services.Contains(x, StringComparer.OrdinalIgnoreCase)).Select(x => $"microservice '{x}'")
			.Concat(draft.portalExtensions.Where(x => !w.extensions.Contains(x, StringComparer.OrdinalIgnoreCase)).Select(x => $"extension '{x}'"))
			.Concat(draft.groups.Where(x => !w.groups.ContainsKey(x)).Select(x => $"service group '{x}'"))
			.ToList();
		if (unknown.Count > 0)
			throw new CliException($"This workspace has no {string.Join(", ", unknown)}. It has the microservices {string.Join(", ", w.services)}; the extensions {string.Join(", ", w.extensions)}; the groups {string.Join(", ", w.groups.Keys)}.");
	}

	/// <summary>Adds the command the <c>--add-step</c> and <c>--step-*</c> options describe.</summary>
	private static void AddCommand(Draft draft, LocalStackConfig config, LocalStackCreateCommandArgs args)
	{
		if (draft.IsVariant)
			throw new CliException("A variant can't have commands of its own. Add the command to the stack, or to the catalogue with `beam local setup --add-step`.");

		var (step, after) = LocalStackCustomSteps.FromFlags(args.command);
		var problem = CommandNameProblem(config, draft, step.name);
		if (problem != null) throw new CliException(problem);
		step.first = args.stepFirst ? true : null;
		step.after = args.stepFirst ? null : after;
		draft.commands.Add(step);
	}

	/// <summary>
	/// Sets the stack's own commands from a JSON array, each item read as the <c>--step-*</c> options would be (so it
	/// is checked the same way): <c>name</c>, one of <c>command</c> (with <c>args</c>), <c>shell</c> (with
	/// <c>shellKind</c>) or <c>beam</c>, then <c>dir</c>, <c>env</c> (<c>KEY=VALUE</c> strings), <c>once</c>,
	/// <c>readyUrl</c>, <c>readyLog</c>, <c>port</c>, <c>timeout</c>, <c>label</c>, <c>tags</c>, <c>after</c> and
	/// <c>first</c>. The ones it had are replaced, which is how the stacks web app edits them.
	/// </summary>
	private static void ReplaceCommands(Draft draft, LocalStackConfig config, string json)
	{
		if (draft.IsVariant)
			throw new CliException("A variant can't have commands of its own. Add the commands to the stack.");

		JArray items;
		try
		{
			items = JArray.Parse(json);
		}
		catch (JsonException e)
		{
			throw new CliException($"--commands takes a JSON array of commands: {e.Message}");
		}

		draft.commands.Clear();
		foreach (var item in items)
		{
			if (item is not JObject c)
				throw new CliException("--commands takes a JSON array of objects, one per command.");

			string Text(string key) => c[key]?.Type == JTokenType.String ? c[key]!.Value<string>() : null;
			int Number(string key) => c[key]?.Type == JTokenType.Integer ? c[key]!.Value<int>() : 0;
			bool Flag(string key) => c[key]?.Type == JTokenType.Boolean && c[key]!.Value<bool>();

			var flags = new LocalStackSetupCommandArgs
			{
				addStep = Text("name"),
				stepCommand = Text("command"),
				stepArgs = Text("args"),
				stepShell = Text("shell"),
				stepShellKind = Text("shellKind"),
				stepBeam = Text("beam"),
				stepDir = Text("dir"),
				stepEnv = c["env"] is JArray env ? env.Values<string>().Where(v => !string.IsNullOrWhiteSpace(v)).ToArray() : null,
				stepOnce = Flag("once"),
				stepReadyUrl = Text("readyUrl"),
				stepReadyLog = Text("readyLog"),
				stepPort = Number("port"),
				stepTimeout = Number("timeout"),
				stepTags = Text("tags"),
				stepLabel = Text("label"),
				stepAfter = Text("after"),
			};
			var (step, after) = LocalStackCustomSteps.FromFlags(flags);
			var problem = CommandNameProblem(config, draft, step.name);
			if (problem != null) throw new CliException(problem);
			var first = Flag("first");
			step.first = first ? true : null;
			step.after = first ? null : after;
			draft.commands.Add(step);
		}
	}

	/// <summary>Why a command of the stack can't have <paramref name="name"/>, or null.</summary>
	private static string CommandNameProblem(LocalStackConfig config, Draft draft, string name) =>
		LocalStackCustomSteps.NameProblem(config, name)
		?? (draft.commands.Any(c => string.Equals(c.name, name?.Trim(), StringComparison.OrdinalIgnoreCase)) ? $"The stack already has a command named '{name.Trim()}'." : null);

	/// <summary>The steps a new command can be placed after: what the stack runs as answered so far.</summary>
	private static List<LocalStackStep> PlacingAmong(Draft draft, LocalStackStacks stacks, LocalStackConfig config)
	{
		try
		{
			var (file, entry) = Build(draft, stacks, config.workspaceRoot ?? Directory.GetCurrentDirectory());
			return stacks.ResolveDraft(draft.FullName ?? "draft", draft.Layer, file, entry, config, replace: draft.IsEdit).steps;
		}
		catch (Exception e) when (e is CliException or LocalStackStackException)
		{
			return config.steps.ToList();
		}
	}

	/// <summary>
	/// Adds what the stack's steps need and it doesn't run (see <see cref="LocalStackDependencies"/>): a catalogue step
	/// to <c>include</c> (and out of <c>exclude</c>), a microservice an extension calls to its services. Repeats until
	/// nothing is missing, since what is added can need more. Each addition is noted in <see cref="Draft.added"/>, with
	/// why. Variants are left alone. Returns the new notes.
	/// </summary>
	private static List<string> AddDependencies(Draft draft, LocalStackStacks stacks, LocalStackConfig config, string root)
	{
		var notes = new List<string>();
		if (draft.IsVariant || draft.stack == null) return notes;

		for (var round = 0; round < 10; round++)
		{
			LocalStackResolvedStack resolved;
			Dictionary<string, LocalStackStep> own;
			try
			{
				var (file, entry) = Build(draft, stacks, root);
				resolved = stacks.ResolveDraft(draft.FullName, draft.Layer, file, entry, config, replace: draft.IsEdit);
				own = OwnSteps(draft);
			}
			catch (Exception e) when (e is CliException or LocalStackStackException)
			{
				return notes; // the review says what's wrong
			}

			var catalogue = config.steps.Concat(own.Select(kv => { kv.Value.name = kv.Key; return kv.Value; })).ToList();
			var missing = LocalStackDependencies.Missing(resolved.steps, catalogue, stacks.TagsOf, LocalStackTargets.RunsLocal(resolved.target, config));
			if (missing.Count == 0) return notes;

			foreach (var m in missing)
			{
				if (m.step == null)
				{
					var service = LocalStackStacks.ExtensionId(m.name);
					if (!draft.services.Contains(service, StringComparer.OrdinalIgnoreCase)) draft.services.Add(service);
				}
				else
				{
					draft.exclude?.RemoveAll(e => string.Equals(e, m.name, StringComparison.OrdinalIgnoreCase));
					draft.include ??= new List<string>();
					if (!draft.include.Contains(m.name, StringComparer.OrdinalIgnoreCase)) draft.include.Add(m.name);
				}

				var note = $"Added {(m.step != null ? Label(m.step) : m.name)}: {m.reason}.";
				if (!draft.added.Contains(note))
				{
					draft.added.Add(note);
					notes.Add(note);
				}
			}
		}

		return notes;
	}

	/// <summary>
	/// Finds the entry to edit (or, with a terminal and no name, asks which) and fills the draft from it. A flag
	/// replaces what the entry says, and an empty one clears it. Only what the local or shared folder defines can be
	/// edited: a built-in stack, or a stack or variant from a lower layer, is changed by adding one's own. The list has
	/// no "nothing" entry: Ctrl+C leaves it.
	/// </summary>
	private static void LoadForEdit(Draft draft, LocalStackCreateCommandArgs args, LocalStackStacks stacks, bool quiet)
	{
		var name = args.name?.Trim();
		var layer = args.shared ? LocalStackLayer.Shared : LocalStackLayer.Local;
		if (string.IsNullOrWhiteSpace(name))
		{
			var editable = Editable(stacks);
			if (quiet || editable.Count == 0)
				throw new CliException(editable.Count == 0
					? "Nothing to edit: there are no stacks in .beamable/local/stacks or .beamable/shared/stacks. Make one with `beam local create`."
					: $"Pass what to edit: `beam local edit <stack>` or `beam local edit <stack>:<variant>` (add --shared for a shared one). These can be edited: {string.Join(", ", editable.Select(e => $"{e.name} ({LayerWord(e.layer)})"))}.");

			var choice = AnsiConsole.Prompt(new SelectionPrompt<(string name, LocalStackLayer layer, string label)>()
				.Title("What do you want to edit?")
				.PageSize(15)
				.AddChoices(editable)
				.UseConverter(e => $"{Markup.Escape(e.name)} [grey]{Markup.Escape(e.label)}[/]")
				.AddBeamHightlight());
			(name, layer) = (choice.name, choice.layer);
		}

		var parts = name.Split(':');
		if (parts.Length > 2 || parts.Any(string.IsNullOrWhiteSpace))
			throw new CliException($"'{name}' is not a name to edit. Use <stack> for a stack or <stack>:<variant> for one variant.");

		bool Same(string a, string b) => string.Equals(a, b?.Trim(), StringComparison.OrdinalIgnoreCase);
		var definitions = stacks.Definitions().Where(d => Same(d.name, parts[0])).ToList();
		var variantName = parts.Length == 2 ? parts[1].Trim() : null;
		bool Defines(LocalStackStackDef def) =>
			variantName == null ? !OnlyVariants(def) : def.variants?.Keys.Any(v => Same(v, variantName)) == true;

		var folder = layer == LocalStackLayer.Shared ? ".beamable/shared/stacks" : ".beamable/local/stacks";
		var found = definitions.FirstOrDefault(d => d.layer == layer && Defines(d.def));
		if (found.def == null)
		{
			if (definitions.Count == 0)
				throw new CliException($"No stack named '{parts[0]}'. Run `beam local run` to list them.");
			if (layer == LocalStackLayer.Local && definitions.Any(d => d.layer == LocalStackLayer.Shared && Defines(d.def)))
				throw new CliException($"'{name}' is in the shared stacks, which are committed. Pass --shared to edit it there.");
			if (definitions.Any(d => d.layer == layer) && variantName == null)
				throw new CliException($"{definitions.First(d => d.layer == layer).file} only adds variants to '{parts[0]}'. Edit one of them with `beam local edit {parts[0]}:<variant>`.");

			var from = definitions.Where(d => d.layer != layer && Defines(d.def)).Select(d => d.layer).DefaultIfEmpty(layer).First();
			throw new CliException(from == layer
				? $"'{name}' isn't in {folder}, so there is nothing there to edit."
				: $"'{name}' comes from the {(from == LocalStackLayer.BuiltIn ? "built-in" : LayerWord(from))} stacks, which edit doesn't change. Make your own instead: `beam local create {parts[0]}:<name>` for a variant, or a stack with --extends {parts[0]}.");
		}

		draft.stack = found.name;
		draft.shared = layer == LocalStackLayer.Shared;
		draft.editFile = found.file;
		draft.original = found.def;
		draft.inherits = definitions.Any(d => d.layer < layer);

		static string Value(string flag, string current) => flag == null ? current : string.IsNullOrWhiteSpace(flag) ? null : flag.Trim();
		static List<string> Items(string flag, List<string> current) => flag == null ? current : List(flag);

		if (variantName == null)
		{
			var def = found.def;
			draft.description = Value(args.description, def.description);
			draft.target = Value(args.target, def.target)?.TrimEnd('/');
			draft.extends = Value(args.extends, def.extends);

			// The stack's own services and commands come back as answers; the include names they added aren't the
			// person's picks, and neither is the narrowing to their ids.
			var own = OwnStepNames(def);
			var ids = own.Select(LocalStackStacks.ExtensionId).ToHashSet(StringComparer.OrdinalIgnoreCase);
			draft.include = Items(args.include, def.include?.Where(i => !own.Contains(i, StringComparer.OrdinalIgnoreCase)).ToList());
			if (draft.include?.Count == 0 && args.include == null && def.include != null && own.Count > 0) draft.include = null;
			draft.exclude = Items(args.exclude, def.exclude);
			draft.extensions = def.extensions?.Where(id => !ids.Contains(id)).ToList() is { Count: > 0 } kept ? kept : null;
			foreach (var (key, step) in def.steps ?? new Dictionary<string, LocalStackStep>())
			{
				if (key.StartsWith(LocalStackTemplate.MicroservicePrefix, StringComparison.OrdinalIgnoreCase)) draft.services.Add(LocalStackStacks.ExtensionId(key));
				else if (key.StartsWith(LocalStackTemplate.ExtensionPrefix, StringComparison.OrdinalIgnoreCase)) draft.portalExtensions.Add(LocalStackStacks.ExtensionId(key));
				else if (key.StartsWith(LocalStackTemplate.GroupPrefix, StringComparison.OrdinalIgnoreCase)) draft.groups.Add(LocalStackStacks.ExtensionId(key));
				else if (step?.custom == true)
				{
					step.name = key;
					draft.commands.Add(step);
				}
			}

			if (args.services != null) draft.services = List(args.services) ?? new List<string>();
			if (args.extensions != null) draft.portalExtensions = List(args.extensions) ?? new List<string>();
			if (args.groups != null) draft.groups = List(args.groups) ?? new List<string>();
			return;
		}

		draft.variant = found.def.variants.Keys.First(v => Same(v, variantName));
		var variant = found.def.variants[draft.variant] ?? new LocalStackVariantDef();
		draft.description = Value(args.description, variant.description);
		draft.target = Value(args.target, variant.target)?.TrimEnd('/');
		draft.before = Items(args.before, variant.before);
		draft.after = Items(args.after, variant.after);
		draft.remove = Items(args.remove, variant.remove);
		draft.changes.Clear();
		if (draft.before != null) draft.changes.Add(ChangeBefore);
		if (draft.after != null) draft.changes.Add(ChangeAfter);
		if (draft.remove != null) draft.changes.Add(ChangeRemove);
	}

	/// <summary>
	/// What <c>beam local edit</c> can change: each stack the local and shared folders define, and each variant in
	/// them. An entry that only adds variants to a stack from another layer is offered by its variants.
	/// </summary>
	private static List<(string name, LocalStackLayer layer, string label)> Editable(LocalStackStacks stacks)
	{
		var list = new List<(string, LocalStackLayer, string)>();
		foreach (var (name, layer, file, def) in stacks.Definitions().Where(d => d.layer != LocalStackLayer.BuiltIn))
		{
			var where = $"({LayerWord(layer)}, {Path.GetFileName(file)})";
			if (!OnlyVariants(def))
				list.Add((name, layer, $"{def.description} {where}".Trim()));
			foreach (var (variant, variantDef) in def.variants ?? new Dictionary<string, LocalStackVariantDef>())
				list.Add(($"{name}:{variant}", layer, $"{variantDef?.description} {where}".Trim()));
		}

		return list;
	}

	/// <summary>True for an entry that only adds variants: it changes nothing about the stack itself.</summary>
	internal static bool OnlyVariants(LocalStackStackDef def) =>
		def.description == null && def.extends == null && def.include == null && def.exclude == null
		&& def.extensions == null && def.steps == null && def.variables == null;

	private static string LayerWord(LocalStackLayer layer) => layer == LocalStackLayer.Shared ? "shared" : "local";

	/// <summary>
	/// Runs the questions like a wizard: on the terminal's alternate screen, so each step replaces the last and the
	/// terminal's own history comes back untouched afterwards. Without an alternate screen each step just clears it.
	/// Ctrl+C restores the normal screen too, so an interrupted wizard never leaves the terminal on the other one.
	/// </summary>
	internal static void InWizardScreen(Action wizard)
	{
		var console = AnsiConsole.Console;
		if (!console.Profile.Capabilities.Ansi || !console.Profile.Capabilities.AlternateBuffer)
		{
			wizard();
			return;
		}

		ConsoleCancelEventHandler restore = (_, _) => Console.Out.Write("\u001b[?1049l\u001b[?25h");
		Console.CancelKeyPress += restore;
		try
		{
			console.AlternateScreen(wizard);
		}
		finally
		{
			Console.CancelKeyPress -= restore;
		}
	}

	/// <summary>
	/// Clears the screen for the next question, leaving just the title, which step this is and the keys: the answers,
	/// with their lists of steps, are on the review, whose "See every step" is the one place that lists them all.
	/// </summary>
	private static void Frame(Draft draft, LocalStackWizard wizard = null)
	{
		AnsiConsole.Clear();
		LocalStackStyle.H1($"beam local {draft.Verb}{(draft.IsEdit ? " " + draft.FullName : string.Empty)}");
		var groups = wizard?.Groups() ?? new List<string>();
		var at = groups.IndexOf(wizard?.current);
		if (at >= 0)
			AnsiConsole.MarkupLine(LocalStackStyle.H2Text($"Step {at + 1} of {groups.Count}: {wizard.current}"));
		AnsiConsole.MarkupLine(LocalStackStyle.KeysText("Enter answers, Shift+Tab goes back"));
		AnsiConsole.WriteLine();
	}

	private static LocalStackStacks LoadStacks(string root)
	{
		try
		{
			return LocalStackStacks.Load(root);
		}
		catch (LocalStackStackException e)
		{
			throw new CliException(e.Message);
		}
	}

	/// <summary>
	/// Checks the draft against the folder it goes to and turns it into the entry to write. Throws with what to fix
	/// when it can't be written as it stands.
	/// </summary>
	private static (string file, LocalStackStackDef entry) Build(Draft draft, LocalStackStacks stacks, string root)
	{
		if (draft.IsEdit) return (draft.editFile, Edited(draft));

		var layer = draft.Layer;
		var dir = LocalStackStacks.StacksDir(root, layer);

		// One folder must never define a name twice, so an entry for a stack this folder already has goes into the
		// same file.
		var existingFile = stacks.FilesDefining(draft.stack, layer).FirstOrDefault();
		var file = existingFile ?? Path.Combine(dir, draft.stack + ".json");

		if (!draft.IsVariant)
		{
			if (existingFile != null)
				throw new CliException($"Stack '{draft.stack}' is already defined in {existingFile}. Change it with `beam local edit {draft.stack}{(draft.shared ? " --shared" : "")}`, or add a variant with `beam local create {draft.stack}:<variant>`.");
			var own = OwnSteps(draft);
			if (draft.extends == null && draft.include == null && own.Count == 0)
				throw new CliException("A stack needs something to run: pass --extends, --include, --services, --extensions, --groups or --add-step.");

			return (file, new LocalStackStackDef
			{
				description = draft.description,
				target = draft.target,
				extends = draft.extends,
				include = WithOwn(draft.include, own.Keys),
				exclude = draft.extends != null || draft.include != null ? draft.exclude : null,
				extensions = Narrowing(draft),
				steps = own.Count > 0 ? own : null,
			});
		}

		if (!stacks.Contains(draft.stack))
			throw new CliException($"No stack named '{draft.stack}' to add a variant to. Run `beam local run` to list them.");
		if (File.Exists(file) && JObject.Parse(File.ReadAllText(file))[draft.stack]?["variants"]?[draft.variant] != null)
			throw new CliException($"{file} already has '{draft.FullName}'. Change it with `beam local edit {draft.FullName}{(draft.shared ? " --shared" : "")}`, or pick another name.");

		var variant = new LocalStackVariantDef
		{
			description = draft.description,
			target = draft.target,
			before = draft.changes.Contains(ChangeBefore) ? draft.before : null,
			after = draft.changes.Contains(ChangeAfter) ? draft.after : null,
			remove = draft.changes.Contains(ChangeRemove) ? draft.remove : null,
		};
		if (variant.before == null && variant.after == null && variant.remove == null && variant.target == null)
			throw new CliException("A variant needs to change something: pass --before, --after, --remove, --target, or a mix.");

		return (file, new LocalStackStackDef
		{
			variants = new Dictionary<string, LocalStackVariantDef> { [draft.variant] = variant }
		});
	}

	/// <summary>
	/// The entry being edited with the draft's answers in place of its own; its variables, steps and other variants
	/// stay. Throws with what to fix when it can't be written as it stands.
	/// </summary>
	private static LocalStackStackDef Edited(Draft draft)
	{
		var entry = JsonConvert.DeserializeObject<LocalStackStackDef>(JsonConvert.SerializeObject(draft.original));
		if (!draft.IsVariant)
		{
			var own = OwnSteps(draft);
			if (!draft.inherits && draft.extends == null && draft.include == null && own.Count == 0)
				throw new CliException("A stack needs something to run: it needs --extends, --include, --services, --extensions, --groups or --add-step.");

			// The entry's other steps (replacements of catalogue steps) stay; its services and commands are the answers.
			var steps = (entry.steps ?? new Dictionary<string, LocalStackStep>())
				.Where(kv => !OwnStepNames(draft.original).Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
				.ToDictionary(kv => kv.Key, kv => kv.Value, StringComparer.OrdinalIgnoreCase);
			foreach (var (name, step) in own) steps[name] = step;

			entry.description = draft.description;
			entry.target = draft.target;
			entry.extends = draft.extends;
			entry.include = WithOwn(draft.include, own.Keys);
			entry.exclude = draft.exclude;
			entry.extensions = Narrowing(draft);
			entry.steps = steps.Count > 0 ? steps : null;
			return entry;
		}

		var variant = entry.variants[draft.variant] ?? new LocalStackVariantDef();
		variant.description = draft.description;
		variant.target = draft.target;
		variant.before = draft.changes.Contains(ChangeBefore) ? draft.before : null;
		variant.after = draft.changes.Contains(ChangeAfter) ? draft.after : null;
		variant.remove = draft.changes.Contains(ChangeRemove) ? draft.remove : null;
		if (variant.before == null && variant.after == null && variant.remove == null && variant.target == null)
			throw new CliException("A variant needs to change something: it needs --before, --after, --remove, --target, or a mix. To drop it, use `beam local delete`.");

		entry.variants[draft.variant] = variant;
		return entry;
	}

	/// <summary>The questions, in order; the ones a flag answered start out answered.</summary>
	private static LocalStackWizard Questions(Draft draft, LocalStackStacks stacks, LocalStackConfig config, LocalStackCreateCommandArgs args)
	{
		var wizard = new LocalStackWizard();
		var named = !string.IsNullOrWhiteSpace(args.name);
		void Redraw() => Frame(draft, wizard);
		string Title() => Markup.Escape(draft.FullName ?? draft.stack ?? "it");

		// An edit starts with every answer given, from the entry, so it opens on the review; what it is and where it
		// lives aren't asked, as they name the entry.
		void Add(string group, Func<bool, LocalStackWizardMove> ask, bool answered, Func<bool> applies = null)
		{
			if (draft.IsEdit && group is "What" or "Who") return;
			wizard.questions.Add(new LocalStackWizard.Question
			{
				group = group, ask = ask, answered = answered || draft.IsEdit, applies = applies ?? (() => true)
			});
		}

		// What: a stack or a variant, and its name.
		bool? wantsVariant = named ? draft.IsVariant : null;
		Add("What", back =>
		{
			const string newStack = "A new stack";
			const string newVariant = "A variant of an existing stack (it adds to or takes from that stack)";
			var choice = Select("What do you want to create?", new[] { newStack, newVariant }, back,
				wantsVariant == null ? null : wantsVariant.Value ? newVariant : newStack);
			if (choice == null) return LocalStackWizardMove.Back;
			wantsVariant = choice == newVariant;
			return LocalStackWizardMove.Next;
		}, named);

		Add("What", back =>
		{
			var listing = stacks.List();
			var choice = Select("Which stack is the variant for?", listing.Select(s => s.name), back, draft.IsVariant ? draft.stack : null,
				name => $"{Markup.Escape(name)} [grey]{Markup.Escape(listing.First(s => s.name == name).description ?? string.Empty)}[/]");
			if (choice == null) return LocalStackWizardMove.Back;
			draft.stack = choice;
			draft.variant ??= string.Empty;
			return LocalStackWizardMove.Next;
		}, named, () => wantsVariant == true);

		Add("What", back =>
		{
			var isVariant = wantsVariant == true;
			var current = isVariant ? draft.variant : draft.variant == null ? draft.stack : null;
			var answer = Text(isVariant ? $"Name of the variant ([green]{Markup.Escape(draft.stack)}:<name>[/]):" : "Name of the stack:",
				back, string.IsNullOrEmpty(current) ? null : current, allowEmpty: false,
				validate: v => !LocalStackStacks.IsValidName(v) ? "Use letters, digits, '.', '_' and '-'."
					: !isVariant && stacks.Contains(v) ? $"'{v}' already exists. Pick another name, or create a variant of it (`beam local edit {v}` changes it)."
					: null);
			if (answer == null) return LocalStackWizardMove.Back;

			if (isVariant) draft.variant = answer;
			else
			{
				draft.stack = answer;
				draft.variant = null;
			}

			return LocalStackWizardMove.Next;
		}, named);

		// Who it is for.
		Add("Who", back =>
		{
			const string local = "Just for me (.beamable/local/stacks, not committed)";
			const string team = "For the team (.beamable/shared/stacks, committed)";
			var choice = Select("Who is it for?", new[] { local, team }, back, draft.shared ? team : local);
			if (choice == null) return LocalStackWizardMove.Back;
			draft.shared = choice == team;
			return LocalStackWizardMove.Next;
		}, args.shared);

		// A stack.
		Add("Starts from", back =>
		{
			const string none = "(start from nothing)";
			var listing = stacks.List().Where(s => !string.Equals(s.name, draft.stack, StringComparison.OrdinalIgnoreCase)).ToList();
			var choice = Select($"Which stack should [green]{Title()}[/] start from? Its steps come along, and you add or take out from there.",
				new[] { none }.Concat(listing.Select(s => s.name)), back, draft.extends ?? none,
				name => name == none
					? none
					: $"{Markup.Escape(name)} [grey]{Markup.Escape(listing.First(s => s.name == name).description ?? string.Empty)}[/]");
			if (choice == null) return LocalStackWizardMove.Back;

			var extends = choice == none ? null : choice;
			if (!string.Equals(extends, draft.extends, StringComparison.OrdinalIgnoreCase))
				draft.exclude = null; // what to leave out depends on what it starts from
			draft.extends = extends;
			return LocalStackWizardMove.Next;
		}, args.extends != null, () => !draft.IsVariant);

		Add("Adds", back =>
		{
			var (move, picked) = PickSteps(stacks, config.steps, AddableSteps(config, Inherited(stacks, config, draft)), draft.include, back,
				$"What should [green]{Title()}[/] add?", Redraw);
			if (move == LocalStackWizardMove.Next) draft.include = picked;
			return move;
		}, args.include != null, () => !draft.IsVariant);

		// Leaving out only makes sense for what the stack inherits: a step it adds itself is simply not added.
		Add("Leaves out", back =>
		{
			var (move, picked) = PickSteps(stacks, config.steps, Inherited(stacks, config, draft), draft.exclude, back,
				$"What should [green]{Title()}[/] leave out of [green]{Markup.Escape(draft.extends ?? $"the {(draft.shared ? "built-in" : "shared or built-in")} {draft.stack}")}[/]?", Redraw);
			if (move == LocalStackWizardMove.Next) draft.exclude = picked;
			return move;
		}, args.exclude != null, () => !draft.IsVariant && (draft.extends != null || draft.inherits));

		// This workspace's microservices, extensions and service groups, as steps of the stack's own.
		Add("Services", back =>
		{
			var picked = PickServices(draft, $"Which of this workspace's service groups, microservices and portal extensions should [green]{Title()}[/] run? [grey](select none for none)[/]", back);
			if (picked == null) return LocalStackWizardMove.Back;
			SetServices(draft, picked);
			return LocalStackWizardMove.Next;
		}, args.services != null || args.extensions != null || args.groups != null,
			() => !draft.IsVariant && (draft.workspace.Any || draft.HasServices));

		// Commands of the stack's own: a seed, a watcher, a script. Several, each asked like `setup --add-step` asks.
		// The screen shows only these commands: after a big service pick the answers so far no longer fit, and the
		// wizard's screen can't scroll.
		Add("Own commands", back =>
		{
			const string done = "No, go on";
			const string add = "Add a command";
			const string removePrefix = "Remove ";
			while (true)
			{
				Frame(draft, wizard);
				LocalStackStyle.H2($"Commands of {draft.FullName ?? draft.stack}");
				if (draft.commands.Count == 0) AnsiConsole.MarkupLine("  [grey]None yet.[/]");
				foreach (var command in draft.commands)
					AnsiConsole.MarkupLine($"  {LocalStackStyle.H3(command.name)}  {Markup.Escape(LocalStackCustomSteps.Describe(command))}");
				AnsiConsole.WriteLine();

				var choices = new List<string> { done, add }.Concat(draft.commands.Select(c => removePrefix + c.name)).ToList();
				var choice = Select($"Add a command of its own to [green]{Title()}[/]? [grey](a data seed, a watcher, a script)[/]",
					choices, back, done, Markup.Escape);
				if (choice == null) return LocalStackWizardMove.Back;
				if (choice == done) return LocalStackWizardMove.Next;
				if (choice.StartsWith(removePrefix, StringComparison.Ordinal))
				{
					draft.commands.RemoveAll(c => c.name == choice.Substring(removePrefix.Length));
					continue;
				}

				var (step, after) = LocalStackCustomSteps.AskForStep(config, null, $"beam local {draft.Verb} {draft.FullName ?? draft.stack}: new command",
					"the stack", PlacingAmong(draft, stacks, config), v => CommandNameProblem(config, draft, v), ownScreen: false);
				if (step == null) continue;
				if (after == LocalStackCustomSteps.AtTheStart) step.first = true;
				else step.after = after;
				draft.commands.Add(step);
			}
		}, args.command.addStep != null, () => !draft.IsVariant);

		// A variant.
		Add("Changes", back =>
		{
			var picked = MultiSelect($"What should [green]{Title()}[/] do? [grey](pick none when it only changes the backend)[/]", new[] { ChangeBefore, ChangeAfter, ChangeRemove },
				draft.changes.ToList(), back, Markup.Escape, required: false);
			if (picked == null) return LocalStackWizardMove.Back;
			draft.changes = picked.ToHashSet();
			return LocalStackWizardMove.Next;
		}, draft.changes.Count > 0 || draft.target != null, () => draft.IsVariant);

		Add("Runs before", back =>
		{
			var (move, picked) = PickSteps(stacks, config.steps, AddableSteps(config, StepsOf(stacks, draft.stack, config)), draft.before, back,
				$"What should [green]{Title()}[/] run before the stack?", Redraw);
			if (move == LocalStackWizardMove.Next) draft.before = picked;
			return move;
		}, draft.before != null, () => draft.IsVariant && draft.changes.Contains(ChangeBefore));

		Add("Starts after", back =>
		{
			var (move, picked) = PickSteps(stacks, config.steps, AddableSteps(config, StepsOf(stacks, draft.stack, config)), draft.after, back,
				$"What should [green]{Title()}[/] start after the stack?", Redraw);
			if (move == LocalStackWizardMove.Next) draft.after = picked;
			return move;
		}, draft.after != null, () => draft.IsVariant && draft.changes.Contains(ChangeAfter));

		Add("Turns off", back =>
		{
			var (move, picked) = PickSteps(stacks, config.steps, RemovableSteps(StepsOf(stacks, draft.stack, config)), draft.remove, back,
				$"Which of the stack's steps should [green]{Title()}[/] turn off?", Redraw);
			if (move == LocalStackWizardMove.Next) draft.remove = picked;
			return move;
		}, draft.remove != null, () => draft.IsVariant && draft.changes.Contains(ChangeRemove));

		// The backend: every target with where it is, the stack's or the base's as the default, or an address.
		Add("Backend", back =>
		{
			const string another = "Another address...";
			var inherited = Inherits(stacks, draft);
			var same = inherited.from == null ? null : $"Same as {inherited.from}";
			var choices = new List<string>();
			if (same != null) choices.Add(same);
			choices.AddRange(LocalStackTargets.Names(config));
			if (draft.target != null && LocalStackTargets.IsAddress(draft.target)) choices.Add(draft.target);
			choices.Add(another);

			var current = draft.target == null ? same ?? LocalStackTargets.Local
				: choices.FirstOrDefault(c => string.Equals(c, draft.target, StringComparison.OrdinalIgnoreCase)) ?? another;
			var choice = Select($"Which backend does [green]{Title()}[/] run against?", choices, back, current,
				c => c == same ? $"{Markup.Escape(c)} [grey]({Markup.Escape(inherited.target)})[/]"
					: c == another ? Markup.Escape(c) + " [grey](a gateway's http(s) address)[/]"
					: $"{Markup.Escape(c)} [grey]{Markup.Escape(TargetHint(c, config))}[/]");
			if (choice == null) return LocalStackWizardMove.Back;

			if (choice == another)
			{
				var address = Text("Backend address [grey](such as https://my-gateway.example.com)[/]:", back,
					LocalStackTargets.IsAddress(draft.target) ? draft.target : null, allowEmpty: false,
					validate: v => LocalStackTargets.IsAddress(v) ? null : "Give an http:// or https:// address.");
				if (address == null) return LocalStackWizardMove.Back;
				draft.target = address.Trim().TrimEnd('/');
			}
			else
			{
				// The inherited backend is written as nothing, so the entry keeps following its base.
				draft.target = choice == same || (same == null && choice == LocalStackTargets.Local && draft.target == null) ? null : choice;
			}

			return LocalStackWizardMove.Next;
		}, args.target != null);

		Add("Description", back =>
		{
			var answer = Text(draft.shared
					? "Description [grey](shown to the team by `beam local run`)[/]:"
					: "Description [grey](optional, shown by `beam local run`)[/]:",
				back, draft.description, allowEmpty: !draft.shared, validate: _ => null);
			if (answer == null) return LocalStackWizardMove.Back;
			draft.description = string.IsNullOrWhiteSpace(answer) ? null : answer.Trim();
			return LocalStackWizardMove.Next;
		}, args.description != null);

		return wizard;
	}

	/// <summary>
	/// The last screen: every answer and the steps the result runs (or why it can't be written yet). From here the
	/// person writes it, changes any answer, or cancels.
	/// </summary>
	private static LocalStackCreateCommandResult Review(LocalStackWizard wizard, Draft draft, LocalStackStacks stacks,
		LocalStackConfig config, string root)
	{
		var write = draft.IsEdit ? "Save the changes" : "Write it";
		var cancel = draft.IsEdit ? "Cancel, change nothing" : "Cancel, write nothing";
		const string changePrefix = "Change: ";

		while (true)
		{
			string file = null;
			LocalStackStackDef entry = null;
			LocalStackResolvedStack resolved = null;
			string problem = null;
			try
			{
				AddDependencies(draft, stacks, config, root);
				(file, entry) = Build(draft, stacks, root);
				resolved = stacks.ResolveDraft(draft.FullName, draft.Layer, file, entry, config, replace: draft.IsEdit);
			}
			catch (Exception e) when (e is CliException or LocalStackStackException)
			{
				problem = e.Message;
			}

			AnsiConsole.Clear();
			LocalStackStyle.H1($"beam local {draft.Verb}: review {draft.FullName ?? "(no name yet)"}",
				draft.IsEdit ? $"{LayerWord(draft.Layer)}, {Path.GetFileName(draft.editFile)}" : null);
			// One line per answer, per kind of step and at most three notes; the full lists are in "See every step".
			var width = Math.Max(40, AnsiConsole.Profile.Width - 2);
			var lines = new List<string> { LocalStackStyle.H2Text("Your answers") };
			foreach (var group in wizard.Groups())
				lines.Add($"  {LocalStackStyle.H3(group)}  {Markup.Escape(Fit(Summary(group, draft), width - group.Length - 4))}");
			if (draft.added.Count > 0)
			{
				lines.AddRange(new[] { string.Empty, LocalStackStyle.H2Text("Added because something needs it") });
				lines.AddRange(draft.added.Take(3).Select(note => $"  [yellow]{Markup.Escape(Fit(note, width - 2))}[/]"));
				if (draft.added.Count > 3)
					lines.Add($"  [yellow]...and {draft.added.Count - 3} more.[/]");
			}

			lines.Add(string.Empty);
			if (resolved != null)
			{
				var summary = StepSummary(stacks, resolved.steps, width);
				lines.Add(LocalStackStyle.H2Text(Markup.Remove(summary[0]).TrimEnd(':')));
				lines.AddRange(summary.Skip(1));
			}
			else
			{
				lines.Add(LocalStackStyle.H2Text("Can't be written yet"));
				lines.Add($"  [red]{Markup.Escape(Fit(problem, width - 2))}[/]");
			}

			var seeAll = resolved == null ? null : $"See every step, in the order they run ({resolved.steps.Count})";
			var choices = new List<string>();
			if (resolved != null) choices.Add(write);
			if (seeAll != null) choices.Add(seeAll);
			choices.AddRange(wizard.Groups().Select(g => changePrefix + g));
			choices.Add(cancel);

			// The screen can't scroll back, so on a short one the choices page and what doesn't fit is said, not lost.
			var (shown, pageSize) = FitReview(lines, choices.Count, AnsiConsole.Profile.Height);
			foreach (var line in shown) AnsiConsole.MarkupLine(line);
			AnsiConsole.WriteLine();

			var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
				.Title(resolved == null ? "What do you want to change?"
					: draft.IsEdit ? $"Save the changes to [green]{Markup.Escape(draft.FullName)}[/] in {Markup.Escape(file)}?"
					: $"Write [green]{Markup.Escape(draft.FullName)}[/] to {Markup.Escape(file)}?")
				.PageSize(pageSize)
				.MoreChoicesText("[grey](move up/down for more)[/]")
				.AddChoices(choices)
				.UseConverter(Markup.Escape)
				.AddBeamHightlight());

			if (choice == seeAll)
			{
				ShowEveryStep(draft, resolved.steps);
				continue;
			}

			// The caller reports either outcome, back on the normal screen; an edit that changed nothing writes nothing.
			if (choice == write)
			{
				if (draft.IsEdit)
					return new LocalStackCreateCommandResult { path = WriteEdit(file, draft, entry) ? file : null, name = draft.FullName };

				Write(file, draft.stack, draft.variant, entry);
				return new LocalStackCreateCommandResult { path = file, name = draft.FullName };
			}

			if (choice == cancel)
				return new LocalStackCreateCommandResult { name = draft.FullName };

			// Ask that group again (back returns here), then anything the change made newly relevant.
			var start = wizard.questions.FindIndex(q => q.group == choice.Substring(changePrefix.Length));
			if (wizard.Run(start, choice.Substring(changePrefix.Length)))
				wizard.AskUnanswered();
		}
	}

	/// <summary>
	/// What of the review fits a screen <paramref name="height"/> lines tall, with its choices below: the lines to show
	/// (when some don't fit, the last one says how many were left out) and the choices' page size, at least three rows.
	/// </summary>
	public static (List<string> lines, int pageSize) FitReview(IReadOnlyList<string> lines, int choices, int height)
	{
		// The title rule, a blank line, the question, the "more" line and the line the cursor waits on.
		const int around = 5;
		var pageSize = Math.Clamp(height - around - lines.Count, 3, Math.Max(3, choices));
		var room = Math.Max(1, height - around - pageSize);
		if (lines.Count <= room) return (lines.ToList(), pageSize);

		var shown = lines.Take(room - 1).ToList();
		shown.Add($"[grey]...{lines.Count - shown.Count} more lines; a taller window shows them[/]");
		return (shown, pageSize);
	}

	/// <summary>
	/// What a stack runs, one line per kind (Docker, C# hosts, Scala services...) with how many and their names cut to
	/// <paramref name="width"/>, so the review fits the screen whatever the stack's size. Builds are counted apart:
	/// they come with their steps and only build when needed.
	/// </summary>
	public static List<string> StepSummary(LocalStackStacks stacks, IReadOnlyList<LocalStackStep> steps, int width)
	{
		var runs = steps.Where(s => !s.build).ToList();
		var builds = steps.Count - runs.Count;
		var lines = new List<string>
		{
			$"It runs {runs.Count} step{(runs.Count == 1 ? "" : "s")}" + (builds > 0 ? $" (+{builds} build{(builds == 1 ? "" : "s")} when needed):" : ":")
		};
		foreach (var (tag, name) in Domains)
		{
			var inKind = runs.Where(s => DomainOf(stacks, s) == tag).ToList();
			if (inKind.Count == 0) continue;
			var head = $"  {name} ({inKind.Count})  ";
			// The kind is on the line already, so "Scala service auth" reads as "auth" there.
			var names = string.Join(", ", inKind.Select(s => Label(s) is var label && label.StartsWith("Scala service ", StringComparison.Ordinal)
				? label.Substring("Scala service ".Length) : Label(s)));
			var room = width - head.Length;
			if (names.Length > room) names = names.Substring(0, Math.Max(1, room - 3)) + "...";
			lines.Add($"  {LocalStackStyle.H3(name)} [grey]({inKind.Count})[/]  {Markup.Escape(names)}");
		}

		return lines;
	}

	/// <summary>Every step, in the order it runs, as a list that scrolls; any key that picks returns to the review.</summary>
	private static void ShowEveryStep(Draft draft, IReadOnlyList<LocalStackStep> steps)
	{
		const string back = "Back to the review";
		var rows = new List<string> { back };
		rows.AddRange(steps.Select((s, i) => $"{i + 1,3}. {Label(s)} ({s.name}){(s.build ? "  build, only when needed" : string.Empty)}"));
		AnsiConsole.Clear();
		LocalStackStyle.H1($"Every step of {draft.FullName ?? draft.stack ?? "the stack"}", "in the order they run");
		AnsiConsole.Prompt(new SelectionPrompt<string>()
			.Title("[grey](Enter goes back)[/]")
			.PageSize(Math.Max(5, AnsiConsole.Profile.Height - 4))
			.MoreChoicesText("[grey](move up/down to see more)[/]")
			.AddChoices(rows)
			.UseConverter(Markup.Escape)
			.AddBeamHightlight());
	}

	/// <summary>The review screen's one-line summary of a group's answer.</summary>
	private static string Summary(string group, Draft draft)
	{
		static string Join(List<string> list) => list == null || list.Count == 0 ? "nothing" : string.Join(", ", list);

		return group switch
		{
			"What" => draft.IsVariant ? $"a variant of {draft.stack}, named {draft.variant}" : $"a stack named {draft.stack}",
			"Who" => draft.shared ? "the team (.beamable/shared/stacks)" : "just me (.beamable/local/stacks)",
			"Starts from" => draft.extends ?? "nothing",
			"Adds" => Join(draft.include),
			"Leaves out" => Join(draft.exclude),
			"Services" => draft.HasServices
				? string.Join(", ", draft.groups.Select(g => $"group {g}").Concat(draft.services).Concat(draft.portalExtensions))
				: "none",
			"Own commands" => draft.commands.Count == 0 ? "none" : string.Join(", ", draft.commands.Select(c => c.name)),
			"Changes" => draft.changes.Count == 0 ? "nothing" : string.Join("; ", draft.changes.Select(c => c.Split(" (")[0])),
			"Runs before" => Join(draft.before),
			"Starts after" => Join(draft.after),
			"Turns off" => Join(draft.remove),
			"Backend" => draft.target ?? "the same as what it builds on",
			"Description" => draft.description ?? "none",
			_ => string.Empty
		};
	}

	/// <summary>What the entry runs against when it names no backend, and what it gets it from (null: nothing, so local).</summary>
	private static (string from, string target) Inherits(LocalStackStacks stacks, Draft draft)
	{
		if (draft.IsVariant) return (draft.stack, stacks.TargetOf(draft.stack));
		if (draft.extends != null) return (draft.extends, stacks.TargetOf(draft.extends));
		return (null, LocalStackTargets.Local);
	}

	/// <summary>Where a target is, for the backend question.</summary>
	private static string TargetHint(string name, LocalStackConfig config)
	{
		if (string.Equals(name, LocalStackTargets.Local, StringComparison.OrdinalIgnoreCase))
			return $"this machine ({config.host})";
		try
		{
			var target = LocalStackTargets.Get(name, config);
			return target.local == true ? $"this machine at {target.host}, for phones and other machines"
				: target.confirm == true ? $"{target.host}, asks before every run"
				: target.host;
		}
		catch (LocalStackStackException e)
		{
			return e.Message;
		}
	}

	/// <summary>The steps an existing stack runs, or none when it doesn't resolve on this machine (the review says why).</summary>
	private static List<LocalStackStep> StepsOf(LocalStackStacks stacks, string name, LocalStackConfig config)
	{
		try
		{
			return stacks.Resolve(name, config).steps;
		}
		catch (LocalStackStackException)
		{
			return new List<LocalStackStep>();
		}
	}

	/// <summary>
	/// The steps the stack starts from before its own include and exclude: the stack it extends, and for an entry
	/// that changes a stack from a lower layer, what that layer selects.
	/// </summary>
	private static List<LocalStackStep> Inherited(LocalStackStacks stacks, LocalStackConfig config, Draft draft)
	{
		if (!draft.inherits)
			return draft.extends == null ? new List<LocalStackStep>() : StepsOf(stacks, draft.extends, config);

		try
		{
			return stacks.ResolveDraft(draft.stack, draft.Layer, draft.editFile, new LocalStackStackDef { description = draft.stack, extends = draft.extends }, config, replace: true).steps;
		}
		catch (LocalStackStackException)
		{
			return new List<LocalStackStep>();
		}
	}

	/// <summary>The extension ids the stack as answered so far would run.</summary>
	private static List<string> ExtensionIds(LocalStackStacks stacks, LocalStackConfig config, Draft draft)
	{
		if (draft.extends == null && draft.include == null && !draft.inherits) return new List<string>();

		try
		{
			// A description, so a shared draft passes the shared folder's check; it doesn't change what runs.
			var running = stacks.ResolveDraft(draft.stack ?? "draft", draft.IsEdit ? draft.Layer : LocalStackLayer.Local, draft.editFile,
				new LocalStackStackDef
				{
					description = draft.stack, extends = draft.extends, include = draft.include ?? (draft.inherits ? null : new List<string>()), exclude = draft.exclude
				}, config, replace: draft.IsEdit).steps;
			return running.Where(s => stacks.TagsOf(s).Any(LocalStackStacks.IsServiceTag))
				.Select(s => LocalStackStacks.ExtensionId(s.name))
				.Distinct(StringComparer.OrdinalIgnoreCase)
				.ToList();
		}
		catch (LocalStackStackException)
		{
			return new List<string>();
		}
	}

	/// <summary>Thrown by <see cref="BackKeyInput"/> on Shift+Tab, to leave the prompt being shown.</summary>
	private sealed class GoBackException : Exception
	{
	}

	/// <summary>Console input that turns Shift+Tab into <see cref="GoBackException"/>; every other key passes through.</summary>
	private sealed class BackKeyInput : IAnsiConsoleInput
	{
		private readonly IAnsiConsoleInput _inner;

		public BackKeyInput(IAnsiConsoleInput inner) => _inner = inner;

		public bool IsKeyAvailable() => _inner.IsKeyAvailable();

		public ConsoleKeyInfo? ReadKey(bool intercept) => Check(_inner.ReadKey(intercept));

		public async Task<ConsoleKeyInfo?> ReadKeyAsync(bool intercept, CancellationToken cancellationToken) =>
			Check(await _inner.ReadKeyAsync(intercept, cancellationToken));

		private static ConsoleKeyInfo? Check(ConsoleKeyInfo? key)
		{
			if (key is { Key: ConsoleKey.Tab } pressed && pressed.Modifiers.HasFlag(ConsoleModifiers.Shift))
				throw new GoBackException();
			return key;
		}
	}

	/// <summary>
	/// The current console with Shift+Tab going back. Spectre's prompts have no key bindings of their own, but they
	/// all read their keys through the console's input, so wrapping it covers lists, multi-selects and text alike.
	/// </summary>
	private sealed class BackKeyConsole : IAnsiConsole
	{
		private readonly IAnsiConsole _inner;

		public BackKeyConsole(IAnsiConsole inner)
		{
			_inner = inner;
			Input = new BackKeyInput(inner.Input);
		}

		public Profile Profile => _inner.Profile;
		public IAnsiConsoleCursor Cursor => _inner.Cursor;
		public IAnsiConsoleInput Input { get; }
		public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;
		public Spectre.Console.Rendering.RenderPipeline Pipeline => _inner.Pipeline;
		public void Clear(bool home) => _inner.Clear(home);
		public void Write(Spectre.Console.Rendering.IRenderable renderable) => _inner.Write(renderable);
	}

	/// <summary>
	/// Shows <paramref name="prompt"/>. When <paramref name="back"/> allows it, Shift+Tab leaves the prompt and this
	/// returns false.
	/// </summary>
	internal static bool Ask<T>(IPrompt<T> prompt, bool back, out T answer)
	{
		if (!back)
		{
			answer = AnsiConsole.Prompt(prompt);
			return true;
		}

		try
		{
			answer = prompt.Show(new BackKeyConsole(AnsiConsole.Console));
			return true;
		}
		catch (GoBackException)
		{
			// The prompt was left half-drawn: show the cursor it hid. The previous question redraws the screen.
			AnsiConsole.Cursor.Show();
			answer = default;
			return false;
		}
	}

	/// <summary>A selection; null when Shift+Tab went back. The current answer is marked.</summary>
	internal static string Select(string title, IEnumerable<string> choices, bool back, string current, Func<string, string> show = null)
	{
		show ??= Markup.Escape;
		var prompt = new SelectionPrompt<string>()
			.Title(back ? $"{title} {BackHint}" : title)
			.PageSize(15)
			.AddChoices(choices)
			.UseConverter(c => c == current ? $"{show(c)} [green](current)[/]" : show(c))
			.AddBeamHightlight();
		return Ask(prompt, back, out var choice) ? choice : null;
	}

	/// <summary>A multi-selection; null when Shift+Tab went back. The current answers start ticked.</summary>
	internal static List<string> MultiSelect(string title, IEnumerable<string> choices, List<string> current, bool back,
		Func<string, string> show, bool required = false)
	{
		var prompt = new MultiSelectionPrompt<string>()
			.Title(title)
			.PageSize(15)
			.InstructionsText(back
				? "[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept, [blue]Shift+Tab[/] to go back)[/]"
				: "[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
			.UseConverter(show)
			.AddBeamHightlight();
		if (!required) prompt.NotRequired();
		foreach (var choice in choices)
		{
			prompt.AddChoice(choice);
			if (current?.Contains(choice, StringComparer.OrdinalIgnoreCase) == true) prompt.Select(choice);
		}

		return Ask(prompt, back, out var picked) ? picked : null;
	}

	/// <summary>A text answer; null when Shift+Tab went back. <paramref name="validate"/> returns an error, or null when fine.</summary>
	internal static string Text(string title, bool back, string current, bool allowEmpty, Func<string, string> validate)
	{
		var prompt = new TextPrompt<string>(back ? $"{title} {BackHint}" : title)
			.Validate(v =>
			{
				var error = validate(v);
				return error == null ? ValidationResult.Success() : ValidationResult.Error(error);
			});
		if (!string.IsNullOrEmpty(current)) prompt.DefaultValue(current);
		if (allowEmpty) prompt.AllowEmpty();

		return Ask(prompt, back, out var answer) ? answer : null;
	}

	/// <summary>The enabled catalogue steps a stack that runs <paramref name="running"/> doesn't run yet.</summary>
	public static List<LocalStackStep> AddableSteps(LocalStackConfig config, IEnumerable<LocalStackStep> running)
	{
		var names = new HashSet<string>(running.Select(s => s.name), StringComparer.OrdinalIgnoreCase);
		return config.steps.Where(s => s.enabled && !names.Contains(s.name)).ToList();
	}

	/// <summary>The steps a variant can turn off: the ones the stack runs.</summary>
	public static List<LocalStackStep> RemovableSteps(IEnumerable<LocalStackStep> running) => running.ToList();

	/// <summary>The picker's groups, one per tag a step is chosen by, in the order shown, with a readable name.</summary>
	private static readonly (string tag, string name)[] Domains =
	{
		("docker", "Docker"), ("csharp", "C# hosts"), ("scala", "Scala services"), ("portal", "Portal"),
		("web-registry", "Web packages"), ("microservices", "Microservices"), ("extensions", "Extensions"), ("custom", "Custom steps"), ("", "Other")
	};

	/// <summary>Marks a picker row as a group header rather than a step; step names never start with it.</summary>
	private const string GroupRow = "\u0001group:";

	/// <summary>
	/// One picker over <paramref name="steps"/> (build steps left out), grouped by tag (Docker, C# hosts, Scala services with the login set
	/// inside, portal, web packages, extensions), each group saying what the tag means and how many steps it has, and
	/// each step shown by its readable label. Ticking a group ticks all of its steps. What was ticked is written as
	/// tags where a whole tag was ticked, and as step names otherwise (see <see cref="CollapseToTags"/>). The current
	/// answer starts ticked; Shift+Tab returns <see cref="LocalStackWizardMove.Back"/>.
	/// </summary>
	private static (LocalStackWizardMove move, List<string> picked) PickSteps(LocalStackStacks stacks, List<LocalStackStep> catalogue,
		List<LocalStackStep> steps, List<string> current, bool back, string title, Action frame)
	{
		// Builds aren't picked: a stack builds what its steps need when it's needed (`:build` forces it, `:no-build`
		// skips it), so a build in the list would only be a way to get that wrong.
		steps = steps.Where(s => !s.build).ToList();
		if (steps.Count == 0) return (LocalStackWizardMove.Next, null);

		var byName = steps.ToDictionary(s => s.name, StringComparer.OrdinalIgnoreCase);
		bool Tagged(LocalStackStep step, string tag) => stacks.TagsOf(step).Contains(tag, StringComparer.OrdinalIgnoreCase);

		var prompt = new MultiSelectionPrompt<string>()
			.Title(title + " [grey](ticking a group ticks all of its steps)[/]")
			.PageSize(20)
			.Mode(SelectionMode.Leaf)
			.MoreChoicesText("[grey](move up/down to reveal more)[/]")
			.InstructionsText(back
				? "[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept, [blue]Shift+Tab[/] to go back)[/]"
				: "[grey](press [blue]<space>[/] to toggle, [green]<enter>[/] to accept)[/]")
			.UseConverter(choice =>
			{
				if (byName.TryGetValue(choice, out var step))
					return $"{Markup.Escape(Label(step))} [grey]({Markup.Escape(step.name)})[/]";

				var tag = choice.Substring(GroupRow.Length);
				var name = tag == "scala-core" ? "Login set" : Domains.First(d => d.tag == tag).name;
				if (tag.Length == 0) return $"[bold]{Markup.Escape(name)}[/]";

				var meaning = LocalStackStacks.TagDescriptions.TryGetValue(tag, out var text) ? text : string.Empty;
				var count = steps.Count(s => Tagged(s, tag));
				return $"[bold]{Markup.Escape(name)}[/] [grey]{Markup.Escape(tag)}: {Markup.Escape(meaning)} ({count} step{(count == 1 ? "" : "s")})[/]";
			})
			.AddBeamHightlight()
			.NotRequired();

		foreach (var (tag, _) in Domains)
		{
			var inGroup = steps.Where(s => DomainOf(stacks, s) == tag).ToList();
			if (inGroup.Count == 0) continue;

			var header = prompt.AddChoice(GroupRow + tag);
			if (tag == "scala" && inGroup.Any(s => Tagged(s, "scala-core")))
			{
				var core = header.AddChild(GroupRow + "scala-core");
				foreach (var step in inGroup.Where(s => Tagged(s, "scala-core")))
					core.AddChild(step.name);
				inGroup = inGroup.Where(s => !Tagged(s, "scala-core")).ToList();
			}

			foreach (var step in inGroup)
				header.AddChild(step.name);
		}

		// The current answer: a tag ticks every one of its steps here, a name ticks that step.
		foreach (var item in current ?? new List<string>())
		{
			foreach (var step in steps.Where(s => string.Equals(s.name, item, StringComparison.OrdinalIgnoreCase) || Tagged(s, item)))
				prompt.Select(step.name);
		}

		frame();
		if (!Ask(prompt, back, out var chosen))
			return (LocalStackWizardMove.Back, current);

		var picked = CollapseToTags(stacks, catalogue, steps, chosen.Where(byName.ContainsKey).ToHashSet(StringComparer.OrdinalIgnoreCase));
		return (LocalStackWizardMove.Next, picked.Count == 0 ? null : picked);
	}

	/// <summary>
	/// What the ticked steps come to as a stack list. A group's tag when every step in the catalogue with that tag is
	/// ticked, so steps that get the tag later come along; <c>scala-core</c> likewise for the login set; otherwise the
	/// ticked step names. Comparing with the whole catalogue matters: a tag that also covers steps the picker didn't
	/// offer (ones the stack already runs) would pull those in too.
	/// </summary>
	public static List<string> CollapseToTags(LocalStackStacks stacks, List<LocalStackStep> catalogue, List<LocalStackStep> steps,
		ICollection<string> ticked)
	{
		bool Tagged(LocalStackStep step, string tag) => stacks.TagsOf(step).Contains(tag, StringComparer.OrdinalIgnoreCase);
		bool WholeTag(string tag) =>
			catalogue.Where(s => s.enabled && Tagged(s, tag)).All(s => ticked.Contains(s.name))
			&& steps.Any(s => Tagged(s, tag));

		var result = new List<string>();
		foreach (var (tag, _) in Domains)
		{
			var inGroup = steps.Where(s => DomainOf(stacks, s) == tag).ToList();
			if (inGroup.Count == 0) continue;

			if (tag.Length > 0 && WholeTag(tag))
			{
				result.Add(tag);
				continue;
			}

			if (tag == "scala" && WholeTag("scala-core"))
			{
				result.Add("scala-core");
				inGroup = inGroup.Where(s => !Tagged(s, "scala-core")).ToList();
			}

			result.AddRange(inGroup.Where(s => ticked.Contains(s.name)).Select(s => s.name));
		}

		return result;
	}

	/// <summary>The tag a step is grouped under in the picker, or empty for a step none of the group tags covers.</summary>
	public static string DomainOf(LocalStackStacks stacks, LocalStackStep step)
	{
		var tags = stacks.TagsOf(step);

		// A step someone added is shown with the others they added; the web package builds are also `build` steps,
		// and their kind is the web registry.
		if (tags.Contains("custom", StringComparer.OrdinalIgnoreCase)) return "custom";
		if (tags.Contains("web-registry", StringComparer.OrdinalIgnoreCase)) return "web-registry";
		foreach (var (tag, _) in Domains)
		{
			if (tag.Length > 0 && tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
				return tag;
		}

		return string.Empty;
	}

	internal static string Label(LocalStackStep step) =>
		string.IsNullOrWhiteSpace(step.label) ? LocalStackConfigIO.InferLabel(step.name ?? string.Empty) : step.label;

	/// <summary>Adds the entry to the file, keeping everything else in it as it was.</summary>
	private static void Write(string file, string stackName, string variantName, LocalStackStackDef entry)
	{
		var serializer = JsonSerializer.Create(new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore
		});

		var root = File.Exists(file) ? JObject.Parse(File.ReadAllText(file)) : new JObject();
		if (variantName == null)
		{
			root[stackName] = JObject.FromObject(entry, serializer);
		}
		else
		{
			if (root[stackName] is not JObject stack)
				root[stackName] = stack = new JObject();
			if (stack["variants"] is not JObject variants)
				stack["variants"] = variants = new JObject();
			variants[variantName] = JObject.FromObject(entry.variants[variantName], serializer);
		}

		Directory.CreateDirectory(Path.GetDirectoryName(file)!);
		File.WriteAllText(file, root.ToString(Formatting.Indented) + Environment.NewLine);
	}

	/// <summary>
	/// Writes the fields an edit asks about over the entry in the file, in place: its other fields (variables, steps,
	/// other variants, a variant's forceBuild) and the file's other entries stay as they were. False, and nothing
	/// written, when the entry already says that.
	/// </summary>
	private static bool WriteEdit(string file, Draft draft, LocalStackStackDef entry)
	{
		static JObject Child(JObject parent, string name)
		{
			var property = parent.Properties().First(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
			if (property.Value is not JObject child)
				property.Value = child = new JObject();
			return child;
		}

		var root = JObject.Parse(File.ReadAllText(file));
		var before = root.DeepClone();
		var target = Child(root, draft.stack);
		var fields = new Dictionary<string, object>();
		if (draft.IsVariant)
		{
			target = Child(Child(target, "variants"), draft.variant);
			var variant = entry.variants[draft.variant];
			fields["description"] = variant.description;
			fields["target"] = variant.target;
			fields["before"] = variant.before;
			fields["after"] = variant.after;
			fields["remove"] = variant.remove;
		}
		else
		{
			fields["description"] = entry.description;
			fields["target"] = entry.target;
			fields["steps"] = entry.steps;
			fields["extends"] = entry.extends;
			fields["include"] = entry.include;
			fields["exclude"] = entry.exclude;
			fields["extensions"] = entry.extensions;
		}

		var serializer = JsonSerializer.Create(new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			DefaultValueHandling = DefaultValueHandling.Ignore
		});
		foreach (var (field, value) in fields)
		{
			if (value == null) target.Remove(field);
			else target[field] = JToken.FromObject(value, serializer);
		}

		if (JToken.DeepEquals(before, root)) return false;

		File.WriteAllText(file, root.ToString(Formatting.Indented) + Environment.NewLine);
		return true;
	}
}

/// <summary>
/// Changes a stack, or one variant, that the local stacks folder (or the shared one with <c>--shared</c>) defines:
/// <c>beam local create</c>'s questions, starting from what the entry says. In a terminal it opens on the review, with
/// the steps the entry runs, and any answer can be changed from there; flags change those answers before it opens.
/// With <c>--quiet</c>, or without a terminal, the flags are the change: each replaces its field, and an empty value
/// clears it. The entry's other fields and the file's other entries are kept. A built-in stack, or a stack or
/// variant from a lower layer, isn't changed here: a variant or a stack that extends it is.
/// </summary>
public class LocalStackEditCommand : LocalStackCreateCommand
{
	public LocalStackEditCommand() : base("edit",
		"Change a stack, or a variant with <stack>:<variant>, in the local stacks folder (or the shared one with --shared), checked against this machine's settings first; shows the entry and asks what to change, or takes the same flags as create")
	{
	}

	protected override bool Editing => true;
}

public class LocalStackDeleteCommandArgs : CommandArgs
{
	/// <summary><c>&lt;stack&gt;</c> or <c>&lt;stack&gt;:&lt;variant&gt;</c>; null to pick one.</summary>
	public string name;

	public bool shared;
}

public class LocalStackDeleteCommandResult
{
	/// <summary>The stack file changed or removed; null when nothing was.</summary>
	public string path;

	public string name;

	/// <summary>True when the file was left empty and removed.</summary>
	public bool fileRemoved;
}

/// <summary>
/// Removes a stack, or one variant, from the local stacks folder (or the shared one with <c>--shared</c>). Removing a
/// stack removes its entry from that folder only: what a lower layer defines (a shared or built-in stack the local
/// entry changed) stays. Built-in stacks and variants can't be deleted, and a stack another stack extends is kept
/// until that one stops extending it. Emptied entries and files are removed. Asks before deleting, unless
/// <c>--quiet</c>; without a name it lists what can be deleted.
/// </summary>
public class LocalStackDeleteCommand
	: AtomicCommand<LocalStackDeleteCommandArgs, LocalStackDeleteCommandResult>
	, IStandaloneCommand, ISkipManifest
{
	// It prints its own readable output; the result object still reaches tools through the data channel.
	public override bool AutoLogOutput => false;

	public LocalStackDeleteCommand() : base("delete",
		"Delete a stack, or a variant with <stack>:<variant>, from the local stacks folder (or the shared one with --shared)")
	{
	}

	public override void Configure()
	{
		AddArgument(new Argument<string>("name", () => null, "The stack to delete, or <stack>:<variant> for one variant. Leave it out to pick one"),
			(args, v) => args.name = v);
		AddOption(new Option<bool>("--shared", "Delete from .beamable/shared/stacks/, which is committed, instead of .beamable/local/stacks/"),
			(args, v) => args.shared = v);
	}

	public override Task<LocalStackDeleteCommandResult> GetResult(LocalStackDeleteCommandArgs args)
	{
		var quiet = args.Quiet || !AnsiConsole.Profile.Capabilities.Interactive;
		var root = LocalStackConfigIO.FindWorkspaceRoot(LocalStackCommand.ResolveManifestPath(args.ConfigService, null));

		LocalStackStacks stacks;
		try
		{
			stacks = LocalStackStacks.Load(root);
		}
		catch (LocalStackStackException e)
		{
			throw new CliException(e.Message);
		}

		var deletable = Deletable(stacks);
		string name;
		LocalStackLayer layer;
		if (string.IsNullOrWhiteSpace(args.name))
		{
			// Like a bare `beam local run`: show everything that can be deleted, then (with a terminal) ask.
			PrintDeletable(stacks);
			if (deletable.Count == 0 || quiet)
				return Task.FromResult(new LocalStackDeleteCommandResult());

			const string nothing = "(delete nothing)";
			var choice = AnsiConsole.Prompt(new SelectionPrompt<(string name, LocalStackLayer layer, string label)>()
				.Title("What do you want to delete?")
				.PageSize(15)
				.AddChoices(deletable.Prepend<(string name, LocalStackLayer layer, string label)>((nothing, LocalStackLayer.Local, string.Empty)))
				.UseConverter(d => d.name == nothing ? nothing : $"{Markup.Escape(d.name)} [grey]{Markup.Escape(d.label)}[/]")
				.AddBeamHightlight());
			if (choice.name == nothing)
			{
				Log.Information("Nothing was deleted.");
				return Task.FromResult(new LocalStackDeleteCommandResult());
			}

			(name, layer) = (choice.name, choice.layer);
		}
		else
		{
			name = args.name.Trim();
			var inLocal = deletable.Any(d => d.layer == LocalStackLayer.Local && string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
			var inShared = deletable.Any(d => d.layer == LocalStackLayer.Shared && string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase));
			layer = args.shared ? LocalStackLayer.Shared : LocalStackLayer.Local;

			if (!args.shared && !inLocal && inShared)
				throw new CliException($"'{name}' is only in the shared stacks, which are committed. Pass --shared to delete it there.");
			if (!(layer == LocalStackLayer.Shared ? inShared : inLocal))
				throw new CliException(NotDeletableReason(stacks, name, layer));
		}

		var parts = name.Split(':');
		var stackName = parts[0];
		var variantName = parts.Length == 2 ? parts[1] : null;
		var file = stacks.FilesDefining(stackName, layer).First();
		var json = JObject.Parse(File.ReadAllText(file));
		var entry = (JObject)json[stackName]!;

		// Deleting the stack's only definition takes the stack away; anything extending it would stop resolving.
		var definitions = stacks.Definitions().Count(d => string.Equals(d.name, stackName, StringComparison.OrdinalIgnoreCase));
		if (variantName == null && definitions == 1)
		{
			var dependents = stacks.ExtendedBy(stackName);
			if (dependents.Count > 0)
				throw new CliException($"'{stackName}' is extended by {string.Join(", ", dependents)}. Delete or change those first.");
		}

		var what = variantName != null
			? $"variant {name}"
			: definitions == 1
				? $"stack {stackName}{((entry["variants"] as JObject)?.Count > 0 ? $" and its {((JObject)entry["variants"]).Count} variant(s)" : "")}"
				: $"your changes to {stackName} (the {LayerName(stacks, stackName, layer)} stack stays)";

		if (!quiet && !AnsiConsole.Confirm($"Delete {Markup.Escape(what)} from {Markup.Escape(file)}?", defaultValue: false))
		{
			Log.Information("Nothing was deleted.");
			return Task.FromResult(new LocalStackDeleteCommandResult { name = name });
		}

		if (variantName == null)
			json.Remove(stackName);
		else
		{
			var variants = (JObject)entry["variants"]!;
			variants.Remove(variantName);
			if (variants.Count == 0) entry.Remove("variants");
			if (entry.Count == 0) json.Remove(stackName);
		}

		var removed = json.Count == 0;
		if (removed) File.Delete(file);
		else File.WriteAllText(file, json.ToString(Formatting.Indented) + Environment.NewLine);

		Log.Information(removed ? $"Deleted {what}; {file} was left empty and removed." : $"Deleted {what} from {file}.");
		return Task.FromResult(new LocalStackDeleteCommandResult { path = file, name = name, fileRemoved = removed });
	}

	/// <summary>
	/// The listing a bare <c>beam local delete</c> prints, laid out like a bare <c>beam local run</c>: the local and the
	/// shared folder, each stack with its description and file, and its variants under it. An entry that only adds
	/// variants to a stack from another layer says whose stack it is.
	/// </summary>
	private static void PrintDeletable(LocalStackStacks stacks)
	{
		var entries = stacks.Definitions().Where(d => d.layer != LocalStackLayer.BuiltIn).ToList();
		if (entries.Count == 0)
		{
			AnsiConsole.MarkupLine("[grey]Nothing to delete: there are no stacks in .beamable/local/stacks or .beamable/shared/stacks. Built-in stacks can't be deleted.[/]");
			return;
		}

		var width = entries.SelectMany(e => (e.def.variants?.Keys ?? Enumerable.Empty<string>()).Select(v => v.Length + 3).Append(e.name.Length + 1))
			.Max() + 2;

		foreach (var layer in new[] { LocalStackLayer.Local, LocalStackLayer.Shared })
		{
			var inLayer = entries.Where(e => e.layer == layer).OrderBy(e => e.name, StringComparer.OrdinalIgnoreCase).ToList();
			if (inLayer.Count == 0) continue;

			LocalStackStyle.H2(layer == LocalStackLayer.Shared ? "Shared stacks" : "Your stacks",
				layer == LocalStackLayer.Shared ? ".beamable/shared/stacks, committed" : ".beamable/local/stacks");
			foreach (var (name, _, file, def) in inLayer)
			{
				var owner = stacks.Definitions().Where(d => string.Equals(d.name, name, StringComparison.OrdinalIgnoreCase) && d.layer < layer)
					.Select(d => d.layer).DefaultIfEmpty(layer).First();
				var description = def.description
				                  ?? (owner != layer
					                  ? $"your changes to the {(owner == LocalStackLayer.BuiltIn ? "built-in" : "shared")} stack"
					                  : string.Empty);
				AnsiConsole.MarkupLine($"  [green]{Markup.Escape(name.PadRight(width))}[/]{Markup.Escape(description)} [grey]({Markup.Escape(Path.GetFileName(file))})[/]");

				foreach (var (variant, variantDef) in def.variants ?? new Dictionary<string, LocalStackVariantDef>())
					AnsiConsole.MarkupLine($"    {Markup.Escape((":" + variant).PadRight(width - 2))}{Markup.Escape(variantDef?.description ?? string.Empty)}");
			}
		}

		AnsiConsole.MarkupLine("[grey]delete one with `beam local delete <stack>[[:variant]]`; add --shared for a shared one. Built-in stacks can't be deleted.[/]");
	}

	/// <summary>Everything in the local and shared folders that can be deleted: each entry, and each variant in it.</summary>
	private static List<(string name, LocalStackLayer layer, string label)> Deletable(LocalStackStacks stacks)
	{
		var list = new List<(string, LocalStackLayer, string)>();
		foreach (var (name, layer, file, def) in stacks.Definitions().Where(d => d.layer != LocalStackLayer.BuiltIn))
		{
			var where = $"({(layer == LocalStackLayer.Shared ? "shared" : "local")}, {Path.GetFileName(file)})";
			var onlyVariants = LocalStackCreateCommand.OnlyVariants(def);
			var count = def.variants?.Count ?? 0;
			list.Add((name, layer, onlyVariants ? $"{where}: your {count} variant(s) on it" : where));
			foreach (var variant in def.variants?.Keys ?? Enumerable.Empty<string>())
				list.Add(($"{name}:{variant}", layer, where));
		}

		return list;
	}

	private static string NotDeletableReason(LocalStackStacks stacks, string name, LocalStackLayer layer)
	{
		var stackName = name.Split(':')[0];
		var folder = layer == LocalStackLayer.Shared ? ".beamable/shared/stacks" : ".beamable/local/stacks";
		if (!stacks.Contains(stackName))
			return $"No stack named '{stackName}'. Run `beam local run` to list them.";
		if (stacks.Definitions().All(d => !string.Equals(d.name, stackName, StringComparison.OrdinalIgnoreCase) || d.layer == LocalStackLayer.BuiltIn))
			return $"'{name}' is built into the CLI and can't be deleted.";
		return $"'{name}' isn't in {folder}, so there is nothing there to delete.";
	}

	private static string LayerName(LocalStackStacks stacks, string stackName, LocalStackLayer except)
	{
		var lower = stacks.Definitions()
			.Where(d => string.Equals(d.name, stackName, StringComparison.OrdinalIgnoreCase) && d.layer != except)
			.Select(d => d.layer)
			.DefaultIfEmpty(LocalStackLayer.BuiltIn)
			.First();
		return lower == LocalStackLayer.BuiltIn ? "built-in" : lower == LocalStackLayer.Shared ? "shared" : "local";
	}
}

/// <summary>
/// The microservices, portal extensions and service groups of the workspace <c>beam local create</c> runs in (its
/// beamo manifest, plus the extensions under the portal checkout), and the microservices each extension calls.
/// </summary>
public class WorkspaceServices
{
	public List<string> services = new List<string>();
	public List<string> extensions = new List<string>();
	public Dictionary<string, string[]> groups = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
	public Dictionary<string, List<string>> calls = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

	/// <summary>Each microservice's .csproj, by id, from the beamo manifest.</summary>
	public Dictionary<string, string> projects = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	/// <summary>The workspace folder (the one holding .beamable); null when not in one.</summary>
	public string workspace;

	public bool Any => services.Count + extensions.Count + groups.Count > 0;

	/// <summary>
	/// The microservices <paramref name="extension"/> calls that this workspace has. A package.json can name one it
	/// doesn't (campaigns names CampaignServiceZone), and adding a step for it would only fail.
	/// </summary>
	public IEnumerable<string> CallsOf(string extension) =>
		calls.TryGetValue(extension, out var c) ? c.Where(s => services.Contains(s, StringComparer.OrdinalIgnoreCase)) : Enumerable.Empty<string>();

	public static async Task<WorkspaceServices> Find(CommandArgs args, LocalStackConfig config)
	{
		var found = new WorkspaceServices();
		var (services, extensions, groups) = await LocalStackSetupCommand.DiscoverWorkspaceServices(args);
		(extensions, groups) = LocalStackSetupCommand.MergePortalExtensions(config.repos?.portalDir, extensions, groups);
		found.services = services;
		found.extensions = extensions;
		found.groups = new Dictionary<string, string[]>(groups, StringComparer.OrdinalIgnoreCase);
		found.calls = LocalStackTemplate.ExtensionMicroservices(config.repos?.portalDir);
		try
		{
			if (args.ConfigService?.DirectoryExists == true)
			{
				found.workspace = args.ConfigService.BeamableWorkspace;
				var protocols = args.BeamoLocalSystem.BeamoManifest?.HttpMicroserviceLocalProtocols;
				foreach (var id in found.services)
					if (protocols != null && protocols.TryGetValue(id, out var local) && !string.IsNullOrEmpty(local?.Metadata?.absolutePath))
						found.projects[id] = local.Metadata.absolutePath;
			}
		}
		catch (Exception e)
		{
			Log.Verbose($"Couldn't read the microservices' projects: {e.Message}");
		}

		return found;
	}

	/// <summary>
	/// Adds a step to <paramref name="steps"/> for each microservice and portal extension found that isn't there yet,
	/// so stacks run what the workspace has now, without setup: a step of that name already in the settings wins. An
	/// extension's step depends on the microservices it calls. No group steps: a group runs members that are already
	/// here, so a group stays something a stack picks. The microservices come after <c>build: microservices</c>,
	/// which compiles them together (see <see cref="LocalStackTemplate.MicroservicesBuildStep"/>).
	/// </summary>
	public void AddTo(List<LocalStackStep> steps)
	{
		var have = steps.Select(s => s.name).ToHashSet(StringComparer.OrdinalIgnoreCase);
		if (MicroservicesBuild() is { } build && have.Add(build.name)) steps.Add(build);
		foreach (var service in services)
		{
			var step = LocalStackTemplate.MicroserviceStep(service);
			if (have.Add(step.name)) steps.Add(step);
		}

		foreach (var extension in extensions)
		{
			var step = LocalStackTemplate.ExtensionStep(extension);
			var calls = CallsOf(extension).Select(c => LocalStackTemplate.MicroservicePrefix + c).ToList();
			step.dependsOn = calls.Count > 0 ? calls : null;
			if (have.Add(step.name)) steps.Add(step);
		}
	}

	/// <summary>
	/// The build of every microservice found: a solution listing their projects, written under .beamable/temp (the
	/// workspace's own may not list them all), and a step that builds it. Null without microservice projects.
	/// </summary>
	private LocalStackStep MicroservicesBuild()
	{
		var paths = services.Where(projects.ContainsKey).Select(s => Path.GetFullPath(projects[s])).ToList();
		if (string.IsNullOrEmpty(workspace) || paths.Count == 0) return null;

		var solution = Path.Combine(workspace, ".beamable", "temp", "local-stack", "microservices.slnx");
		var content = "<Solution>\n" + string.Concat(paths.Select(p => $"  <Project Path=\"{System.Security.SecurityElement.Escape(p)}\" />\n")) + "</Solution>\n";
		if (!File.Exists(solution) || File.ReadAllText(solution) != content)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(solution)!);
			File.WriteAllText(solution, content);
		}

		return LocalStackTemplate.MicroservicesBuildStep(solution, ProjectFolders(paths));
	}

	/// <summary>The folders of <paramref name="csprojs"/> and of every project they reference, transitively.</summary>
	public static List<string> ProjectFolders(IEnumerable<string> csprojs)
	{
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var queue = new Queue<string>(csprojs.Select(p => Path.GetFullPath(p)));
		while (queue.Count > 0)
		{
			var project = queue.Dequeue();
			if (!File.Exists(project) || !seen.Add(project)) continue;
			try
			{
				foreach (var reference in System.Xml.Linq.XDocument.Load(project).Descendants()
					         .Where(e => e.Name.LocalName == "ProjectReference")
					         .Select(e => (string)e.Attribute("Include"))
					         .Where(i => !string.IsNullOrWhiteSpace(i)))
					queue.Enqueue(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, reference.Replace('\\', Path.DirectorySeparatorChar))));
			}
			catch (System.Xml.XmlException e)
			{
				Log.Verbose($"Couldn't read the project references of {project}: {e.Message}");
			}
		}

		return seen.Select(p => Path.GetDirectoryName(p)!).Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToList();
	}

	/// <summary>Finds the workspace's services and adds them to <paramref name="config"/>'s steps (see <see cref="AddTo"/>).</summary>
	public static async Task<WorkspaceServices> AddFound(CommandArgs args, LocalStackConfig config)
	{
		var found = await Find(args, config);
		found.AddTo(config.steps);
		return found;
	}
}
