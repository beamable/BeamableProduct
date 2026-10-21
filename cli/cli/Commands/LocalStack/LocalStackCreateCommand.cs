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

	// A stack.
	public string extends;
	public string include;
	public string exclude;
	public string extensions;

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
		AddOption(new Option<string>("--extends", "A stack: another stack to start from"),
			(args, v) => args.extends = v);
		AddOption(new Option<string>("--include", "A stack: comma separated tags or step names to add"),
			(args, v) => args.include = v);
		AddOption(new Option<string>("--exclude", "A stack: comma separated tags or step names to take out"),
			(args, v) => args.exclude = v);
		AddOption(new Option<string>("--extensions", "A stack: comma separated extension, microservice or service group ids to narrow the extension steps to"),
			(args, v) => args.extensions = v);
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
		public string extends;
		public List<string> include;
		public List<string> exclude;
		public List<string> extensions;
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

	public override Task<LocalStackCreateCommandResult> GetResult(LocalStackCreateCommandArgs args)
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
			extends = args.extends,
			include = List(args.include),
			exclude = List(args.exclude),
			extensions = List(args.extensions),
			before = List(args.before),
			after = List(args.after),
			remove = List(args.remove),
		};
		if (draft.before != null) draft.changes.Add(ChangeBefore);
		if (draft.after != null) draft.changes.Add(ChangeAfter);
		if (draft.remove != null) draft.changes.Add(ChangeRemove);

		if (Editing)
		{
			LoadForEdit(draft, args, stacks, quiet);

			if (quiet && args.description == null && args.extends == null && args.include == null && args.exclude == null
			    && args.extensions == null && args.before == null && args.after == null && args.remove == null)
				throw new CliException($"Pass what to change in {draft.FullName}: --description, and --extends, --include, --exclude, --extensions for a stack, or --before, --after, --remove for a variant (an empty value clears it). In an interactive terminal it shows the entry and asks.");
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

		if (quiet)
		{
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
				return Task.FromResult(new LocalStackCreateCommandResult { name = draft.FullName });
			}

			if (!draft.IsEdit) Write(file, draft.stack, draft.variant, entry);
			Log.Information($"{(draft.IsEdit ? "Saved" : "Wrote")} {draft.FullName} {(draft.IsEdit ? "in" : "to")} {file}. Check it with `beam local run {draft.FullName} --dry-run`.");
			return Task.FromResult(new LocalStackCreateCommandResult { path = file, name = draft.FullName });
		}

		var wizard = Questions(draft, stacks, config, args);
		wizard.beforeEach = () => Frame(wizard, draft);

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
			return Task.FromResult(result);
		}

		foreach (var group in wizard.Groups())
			AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(group)}:[/] {Markup.Escape(Summary(group, draft))}");
		Log.Information($"{(draft.IsEdit ? "Saved" : "Wrote")} {draft.FullName} {(draft.IsEdit ? "in" : "to")} {result.path}. Check it with `beam local run {draft.FullName} --dry-run`.");
		return Task.FromResult(result);
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
			draft.extends = Value(args.extends, def.extends);
			draft.include = Items(args.include, def.include);
			draft.exclude = Items(args.exclude, def.exclude);
			draft.extensions = Items(args.extensions, def.extensions);
			return;
		}

		draft.variant = found.def.variants.Keys.First(v => Same(v, variantName));
		var variant = found.def.variants[draft.variant] ?? new LocalStackVariantDef();
		draft.description = Value(args.description, variant.description);
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

	/// <summary>Clears the screen for the next question: the title, then every answer given so far.</summary>
	private static void Frame(LocalStackWizard wizard, Draft draft)
	{
		AnsiConsole.Clear();
		AnsiConsole.MarkupLine($"[bold]beam local {draft.Verb}[/]{(draft.IsEdit ? $" [bold]{Markup.Escape(draft.FullName)}[/]" : "")} [grey](Enter to answer, Shift+Tab to go back)[/]");
		foreach (var group in wizard.AnsweredGroups())
			AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(group)}:[/] {Markup.Escape(Summary(group, draft))}");
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
			if (draft.extends == null && draft.include == null)
				throw new CliException("A stack needs something to run: pass --extends, --include, or both.");

			return (file, new LocalStackStackDef
			{
				description = draft.description,
				extends = draft.extends,
				include = draft.include,
				exclude = draft.extends != null || draft.include != null ? draft.exclude : null,
				extensions = draft.extensions,
			});
		}

		if (!stacks.Contains(draft.stack))
			throw new CliException($"No stack named '{draft.stack}' to add a variant to. Run `beam local run` to list them.");
		if (File.Exists(file) && JObject.Parse(File.ReadAllText(file))[draft.stack]?["variants"]?[draft.variant] != null)
			throw new CliException($"{file} already has '{draft.FullName}'. Change it with `beam local edit {draft.FullName}{(draft.shared ? " --shared" : "")}`, or pick another name.");

		var variant = new LocalStackVariantDef
		{
			description = draft.description,
			before = draft.changes.Contains(ChangeBefore) ? draft.before : null,
			after = draft.changes.Contains(ChangeAfter) ? draft.after : null,
			remove = draft.changes.Contains(ChangeRemove) ? draft.remove : null,
		};
		if (variant.before == null && variant.after == null && variant.remove == null)
			throw new CliException("A variant needs to change something: pass --before, --after, --remove, or a mix.");

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
			if (!draft.inherits && draft.extends == null && draft.include == null)
				throw new CliException("A stack needs something to run: it needs --extends, --include, or both.");

			entry.description = draft.description;
			entry.extends = draft.extends;
			entry.include = draft.include;
			entry.exclude = draft.exclude;
			entry.extensions = draft.extensions;
			return entry;
		}

		var variant = entry.variants[draft.variant] ?? new LocalStackVariantDef();
		variant.description = draft.description;
		variant.before = draft.changes.Contains(ChangeBefore) ? draft.before : null;
		variant.after = draft.changes.Contains(ChangeAfter) ? draft.after : null;
		variant.remove = draft.changes.Contains(ChangeRemove) ? draft.remove : null;
		if (variant.before == null && variant.after == null && variant.remove == null)
			throw new CliException("A variant needs to change something: it needs --before, --after, --remove, or a mix. To drop it, use `beam local delete`.");

		entry.variants[draft.variant] = variant;
		return entry;
	}

	/// <summary>The questions, in order; the ones a flag answered start out answered.</summary>
	private static LocalStackWizard Questions(Draft draft, LocalStackStacks stacks, LocalStackConfig config, LocalStackCreateCommandArgs args)
	{
		var wizard = new LocalStackWizard();
		var named = !string.IsNullOrWhiteSpace(args.name);
		void Redraw() => Frame(wizard, draft);
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

		Add("Narrows to", back =>
		{
			var ids = ExtensionIds(stacks, config, draft);
			var picked = MultiSelect("Run only some of the extensions, microservices and service groups? [grey](select none to run them all)[/]",
				ids, draft.extensions, back, Markup.Escape);
			if (picked == null) return LocalStackWizardMove.Back;
			draft.extensions = picked.Count == 0 ? null : picked;
			return LocalStackWizardMove.Next;
		}, args.extensions != null,
			() => !draft.IsVariant && ExtensionIds(stacks, config, draft).Count > 1);

		// A variant.
		Add("Changes", back =>
		{
			var picked = MultiSelect($"What should [green]{Title()}[/] do?", new[] { ChangeBefore, ChangeAfter, ChangeRemove },
				draft.changes.ToList(), back, Markup.Escape, required: true);
			if (picked == null) return LocalStackWizardMove.Back;
			draft.changes = picked.ToHashSet();
			return LocalStackWizardMove.Next;
		}, draft.changes.Count > 0, () => draft.IsVariant);

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
				(file, entry) = Build(draft, stacks, root);
				resolved = stacks.ResolveDraft(draft.FullName, draft.Layer, file, entry, config, replace: draft.IsEdit);
			}
			catch (Exception e) when (e is CliException or LocalStackStackException)
			{
				problem = e.Message;
			}

			AnsiConsole.Clear();
			AnsiConsole.MarkupLine($"[bold]beam local {draft.Verb}[/] [grey]review[/] [bold]{Markup.Escape(draft.FullName ?? "(no name yet)")}[/]"
			                       + (draft.IsEdit ? $" [grey]({LayerWord(draft.Layer)}, {Markup.Escape(Path.GetFileName(draft.editFile))})[/]" : ""));
			foreach (var group in wizard.Groups())
				AnsiConsole.MarkupLine($"  [grey]{Markup.Escape(group)}:[/] {Markup.Escape(Summary(group, draft))}");
			AnsiConsole.WriteLine();

			if (resolved != null)
			{
				AnsiConsole.MarkupLine($"It runs {resolved.steps.Count} step(s):");
				var index = 0;
				foreach (var step in resolved.steps)
					AnsiConsole.MarkupLine($"  {++index,3}. {Markup.Escape(Label(step))} [grey]({Markup.Escape(step.name)})[/]");
			}
			else
			{
				AnsiConsole.MarkupLine($"[red]Can't be written yet:[/] {Markup.Escape(problem)}");
			}

			var choices = new List<string>();
			if (resolved != null) choices.Add(write);
			choices.AddRange(wizard.Groups().Select(g => changePrefix + g));
			choices.Add(cancel);

			var choice = AnsiConsole.Prompt(new SelectionPrompt<string>()
				.Title(resolved == null ? "What do you want to change?"
					: draft.IsEdit ? $"Save the changes to [green]{Markup.Escape(draft.FullName)}[/] in {Markup.Escape(file)}?"
					: $"Write [green]{Markup.Escape(draft.FullName)}[/] to {Markup.Escape(file)}?")
				.PageSize(15)
				.AddChoices(choices)
				.UseConverter(Markup.Escape)
				.AddBeamHightlight());

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
			"Narrows to" => draft.extensions == null ? "all of them" : Join(draft.extensions),
			"Changes" => draft.changes.Count == 0 ? "nothing" : string.Join("; ", draft.changes.Select(c => c.Split(" (")[0])),
			"Runs before" => Join(draft.before),
			"Starts after" => Join(draft.after),
			"Turns off" => Join(draft.remove),
			"Description" => draft.description ?? "none",
			_ => string.Empty
		};
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
			return running.Where(s => stacks.TagsOf(s).Contains("extensions", StringComparer.OrdinalIgnoreCase))
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
		("web-registry", "Web packages"), ("extensions", "Extensions"), ("custom", "Custom steps"), ("", "Other")
	};

	/// <summary>Marks a picker row as a group header rather than a step; step names never start with it.</summary>
	private const string GroupRow = "\u0001group:";

	/// <summary>
	/// One picker over <paramref name="steps"/>, grouped by tag (Docker, C# hosts, Scala services with the login set
	/// inside, portal, web packages, extensions), each group saying what the tag means and how many steps it has, and
	/// each step shown by its readable label. Ticking a group ticks all of its steps. What was ticked is written as
	/// tags where a whole tag was ticked, and as step names otherwise (see <see cref="CollapseToTags"/>). The current
	/// answer starts ticked; Shift+Tab returns <see cref="LocalStackWizardMove.Back"/>.
	/// </summary>
	private static (LocalStackWizardMove move, List<string> picked) PickSteps(LocalStackStacks stacks, List<LocalStackStep> catalogue,
		List<LocalStackStep> steps, List<string> current, bool back, string title, Action frame)
	{
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
			fields["before"] = variant.before;
			fields["after"] = variant.after;
			fields["remove"] = variant.remove;
		}
		else
		{
			fields["description"] = entry.description;
			fields["extends"] = entry.extends;
			fields["include"] = entry.include;
			fields["exclude"] = entry.exclude;
			fields["extensions"] = entry.extensions;
		}

		foreach (var (field, value) in fields)
		{
			if (value == null) target.Remove(field);
			else target[field] = JToken.FromObject(value);
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

			AnsiConsole.MarkupLine($"[bold]{(layer == LocalStackLayer.Shared ? "shared" : "local")}[/]");
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
