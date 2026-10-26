using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;

namespace cli.Services.LocalStack;

/// <summary>
/// The headings every <c>beam local</c> command prints, so a screen reads at a glance, in the portal's colours: H1 for
/// the command or what it shows (a purple rule across the screen), H2 for a section (cyan, underlined), H3 for a group
/// inside a section (amber). The rule is plain ASCII: Windows consoles printed box-drawing characters as '?'.
/// </summary>
public static class LocalStackStyle
{
	public const string H1Colour = "#9580D8";
	public const string H2Colour = "#04C6F1";
	public const string H3Colour = "#EFA42C";

	/// <summary>A rule across the screen with <paramref name="title"/> in it, and a grey <paramref name="note"/>.</summary>
	public static void H1(string title, string note = null) =>
		AnsiConsole.Write(H1Rule(title, note));

	public static Rule H1Rule(string title, string note = null) =>
		new Rule($"[bold {H1Colour}]{Markup.Escape(title)}[/]" + (string.IsNullOrEmpty(note) ? string.Empty : $" [grey]{Markup.Escape(note)}[/]"))
		{
			Justification = Justify.Left,
			Border = BoxBorder.Ascii,
			Style = Style.Parse(H1Colour)
		};

	/// <summary>The H1 colour on a title that has to stay one line of markup (the live board's).</summary>
	public static string H1Text(string title) => $"[bold {H1Colour}]{Markup.Escape(title)}[/]";

	/// <summary>A section heading, after a blank line.</summary>
	public static void H2(string title, string note = null)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine(H2Text(title, note));
	}

	public static string H2Text(string title, string note = null) =>
		$"[bold underline {H2Colour}]{Markup.Escape(title)}[/]" + (string.IsNullOrEmpty(note) ? string.Empty : $" [grey]{Markup.Escape(note)}[/]");

	/// <summary>A group heading inside a section, as markup to start a line with.</summary>
	public static string H3(string title) => $"[bold {H3Colour}]{Markup.Escape(title)}[/]";
}

/// <summary>Where a step is while a stack starts: one value per row of the progress board.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum LocalStackStepState
{
	/// <summary>An earlier step isn't ready yet.</summary>
	Waiting,

	/// <summary>A build step is running.</summary>
	Building,

	/// <summary>A build step that doesn't need to build: what it compiles hasn't changed since its last build.</summary>
	UpToDate,

	/// <summary>The process is up and its readiness gate hasn't passed yet.</summary>
	Starting,

	/// <summary>Relaunched after exiting early or hanging; the detail says which retry.</summary>
	Restarting,

	/// <summary>The readiness gate passed.</summary>
	Ready,

	/// <summary>Up, but there is no readiness gate, or it never signalled within its timeout.</summary>
	Running,

	/// <summary>Up, but something it needs failed (a federation that didn't register).</summary>
	Degraded,

	/// <summary>A step that runs once finished.</summary>
	Done,

	/// <summary>Exited with an error, couldn't start, or didn't finish in time.</summary>
	Failed,

	/// <summary>Was up and isn't any more (what <c>beam local ps</c> finds afterwards).</summary>
	Stopped
}

/// <summary>One row of the board.</summary>
public class LocalStackBoardRow
{
	/// <summary>The step's name: the key stacks and <c>beam local logs</c> use.</summary>
	public string name;

	/// <summary>The readable name shown on the row.</summary>
	public string label;

	public LocalStackStepState state;

	public int port;

	/// <summary>The readiness timeout; a starting row shows its time against it.</summary>
	public int timeoutSeconds;

	/// <summary>A short note: the exit code, the retry, "already running".</summary>
	public string detail;

	/// <summary>When the step left <see cref="LocalStackStepState.Waiting"/>.</summary>
	public DateTime? startedUtc;

	/// <summary>When it reached the state it is in now.</summary>
	public DateTime changedUtc;

	/// <summary>True for a step that runs once (a build, <c>docker compose up</c>, a seed).</summary>
	public bool once;

	/// <summary>True when the state is one the step stays in: its time stops counting.</summary>
	[JsonIgnore]
	public bool Settled => state is LocalStackStepState.Ready or LocalStackStepState.Running or LocalStackStepState.Degraded
		or LocalStackStepState.Done or LocalStackStepState.UpToDate or LocalStackStepState.Failed or LocalStackStepState.Stopped;

	/// <summary>Seconds since the step started, up to when it settled.</summary>
	public int ElapsedSeconds(DateTime nowUtc) =>
		startedUtc == null ? 0 : (int)Math.Max(0, ((Settled ? changedUtc : nowUtc) - startedUtc.Value).TotalSeconds);
}

/// <summary>
/// What the board shows, as written to <c>local-stack.board.json</c> next to the run-state, so
/// <c>beam local ps</c> can print the same board from another terminal.
/// </summary>
public class LocalStackBoardSnapshot
{
	public string stack;

	/// <summary>The <c>beam local run</c> process; an attached run's board is stale once it is gone.</summary>
	public int runnerPid;

	public bool attached;
	public string host;
	public string portalUrl;
	public DateTime startedUtc;

	/// <summary>Where the run writes each step's output (<c>&lt;step&gt;.log</c>, <c>&lt;step&gt;.err.log</c>); how
	/// <c>beam local logs</c> finds an attached stack's, which keeps no run-state.</summary>
	public string logsDir;

	/// <summary>True once the stack has come up: from then on its services can be restarted from the board.</summary>
	public bool up;
	public List<LocalStackBoardRow> rows = new List<LocalStackBoardRow>();

	/// <summary>The local builds of the web packages the extensions use, by package; null without them.</summary>
	public Dictionary<string, string> webPackages;
}

/// <summary>How the board reaches the person running the stack.</summary>
public enum LocalStackBoardMode
{
	/// <summary>A terminal: one row per step, full screen, scrollable with the keys.</summary>
	Live,

	/// <summary>No terminal (CI, a log file): one timestamped line per state change.</summary>
	Lines,

	/// <summary>Output is piped as data: the result stream carries the states, and the board prints nothing.</summary>
	Silent
}

/// <summary>
/// The progress board <c>beam local run</c> shows while a stack starts: one row per step in start order with its
/// readable name, a bar, its state, its time and its port, full screen and scrollable (see <see cref="ShowLive"/>).
/// A failed step's row turns red, and its last log lines and where to find the rest are printed. Without a terminal each state change is one line
/// instead. The board is also written to a file on every change, which is what <c>beam local ps</c> prints.
/// </summary>
public class LocalStackBoard
{
	/// <summary>Below this width the board drops the bar, the time and the port.</summary>
	public const int NarrowWidth = 80;

	// The board is plain ASCII. Spectre can report a Windows console as Unicode while the stream underneath still
	// can't encode box-drawing characters, which then print as '?'.

	private const int BarWidth = 20;

	private readonly object _lock = new object();
	private readonly LocalStackBoardSnapshot _snapshot;
	private readonly LocalStackBoardMode _mode;
	private readonly string _file;
	private readonly Func<DateTime> _now;
	private readonly TaskCompletionSource _done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <summary>The lines written in <see cref="LocalStackBoardMode.Lines"/> mode go here (the console by default).</summary>
	public Action<string> WriteLine = line => AnsiConsole.Console.WriteLine(line);

	public LocalStackBoard(LocalStackBoardSnapshot snapshot, LocalStackBoardMode mode, string file, Func<DateTime> now = null)
	{
		_snapshot = snapshot;
		_mode = mode;
		_file = file;
		_now = now ?? (() => DateTime.UtcNow);
		Save();
	}

	public LocalStackBoardMode Mode => _mode;

	/// <summary>The stack the board is for.</summary>
	public string Name => _snapshot.stack;

	/// <summary>A copy of the rows as they are now.</summary>
	public List<LocalStackBoardRow> Rows
	{
		get
		{
			lock (_lock) return _snapshot.rows.Select(Copy).ToList();
		}
	}

	/// <summary>A copy of one step's row, or null when the board has no such step.</summary>
	public LocalStackBoardRow Row(string step)
	{
		lock (_lock)
		{
			var row = Find(step);
			return row == null ? null : Copy(row);
		}
	}

	/// <summary>
	/// Moves a step to <paramref name="state"/>. The same state again only updates the detail. In lines mode a real
	/// change prints one line; a failure also prints <paramref name="tail"/> (its last log lines) and
	/// <paramref name="hint"/> (where the rest is), above the live board or under the line, unless
	/// <paramref name="announce"/> is false.
	/// </summary>
	public void Set(string step, LocalStackStepState state, string detail = null, IReadOnlyList<string> tail = null,
		string hint = null, bool announce = true)
	{
		LocalStackBoardRow row;
		LocalStackStepState from;
		var now = _now();
		lock (_lock)
		{
			var current = Find(step);
			if (current == null) return;

			from = current.state;
			current.detail = detail;
			if (from == state && from != LocalStackStepState.Restarting)
			{
				Save();
				return;
			}

			current.startedUtc ??= now;
			// A retry starts the clock again, so the time shown is against this attempt's timeout.
			if (state == LocalStackStepState.Restarting) current.startedUtc = now;
			current.state = state;
			current.changedUtc = now;
			Save();
			row = Copy(current);
		}

		if (!announce || _mode == LocalStackBoardMode.Silent) return;

		if (_mode == LocalStackBoardMode.Lines)
			WriteLine(Line(row, from, now));

		if (state == LocalStackStepState.Failed)
		{
			foreach (var line in FailureBlock(row, tail, hint))
				WriteLine(line);
		}
	}

	/// <summary>The stack changed to another name while running (<c>beam local run</c> of another name).</summary>
	public void Rename(string stack)
	{
		lock (_lock)
		{
			_snapshot.stack = stack;
			Save();
		}
	}

	/// <summary>The stack is up: the board says where it is, and its services can be restarted from it.</summary>
	public void MarkUp()
	{
		lock (_lock)
		{
			_snapshot.up = true;
			Save();
		}
	}

	/// <summary>Records which local builds of the web packages the extensions use (see <see cref="WebPackagesLine"/>).</summary>
	public void SetWebPackages(Dictionary<string, string> packages)
	{
		lock (_lock)
		{
			_snapshot.webPackages = packages;
			Save();
		}
	}

	/// <summary>Shows <paramref name="snapshot"/> instead, as <c>beam local ps --watch</c> does when the board file changes.</summary>
	public void Load(LocalStackBoardSnapshot snapshot)
	{
		lock (_lock)
		{
			_snapshot.stack = snapshot.stack;
			_snapshot.attached = snapshot.attached;
			_snapshot.host = snapshot.host;
			_snapshot.portalUrl = snapshot.portalUrl;
			_snapshot.up = snapshot.up;
			_snapshot.webPackages = snapshot.webPackages;
			_snapshot.rows = snapshot.rows.Select(Copy).ToList();
		}
	}

	/// <summary>Adds rows for steps a changed stack runs that weren't on the board, in the order given.</summary>
	public void AddRows(IEnumerable<LocalStackBoardRow> rows)
	{
		lock (_lock)
		{
			foreach (var row in rows.Where(r => Find(r.name) == null))
				_snapshot.rows.Add(row);
			Save();
		}
	}

	/// <summary>Takes the rows of steps a changed stack no longer runs off the board.</summary>
	public void RemoveRows(IEnumerable<string> steps)
	{
		var names = steps.ToHashSet(StringComparer.OrdinalIgnoreCase);
		lock (_lock)
		{
			_snapshot.rows.RemoveAll(r => names.Contains(r.name));
			Save();
		}
	}

	private LocalStackBoardRow Find(string step) =>
		_snapshot.rows.FirstOrDefault(r => string.Equals(r.name, step, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Shows the board full screen, on the terminal's alternate screen, until <see cref="Complete"/> (or
	/// <paramref name="token"/>), then puts the normal screen back. Every row stays reachable: the arrow keys select
	/// a row, PgUp/PgDn and Home/End move by a page or to the ends, and until one of them is pressed the view follows
	/// the steps in progress (F follows again). With <paramref name="onAction"/>, Space on the selected row offers
	/// what can be done with it once the stack is up (restart, stop or start), and Enter does it: the callback gets
	/// the action and the step, and what it answers goes to the message pane. What else is written while it shows
	/// (warnings, a failure's lines) goes to that pane at its bottom, and is printed again on the normal screen when
	/// the board closes, so it stays in the terminal's history. Ctrl+C puts the normal screen back too. Lines and
	/// silent mode return at once.
	/// </summary>
	public async Task ShowLive(CancellationToken token, Func<string, string, Task<string>> onAction = null, string keys = null)
	{
		if (_mode != LocalStackBoardMode.Live) return;

		var console = AnsiConsole.Console;
		var output = console.Profile.Out.Writer;
		var messages = new LocalStackBoardMessages();
		var capture = AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = AnsiSupport.No,
			ColorSystem = ColorSystemSupport.NoColors,
			Interactive = InteractionSupport.No,
			Out = new AnsiConsoleOutput(messages.Writer)
		});
		capture.Profile.Width = Math.Max(40, console.Profile.Width);

		const string enter = "\u001b[?1049h\u001b[?25l", leave = "\u001b[?25h\u001b[?1049l";
		ConsoleCancelEventHandler restore = (_, _) =>
		{
			try { output.Write(leave); output.Flush(); }
			catch { /* the process is going away */ }
		};

		Console.CancelKeyPress += restore;
		AnsiConsole.Console = capture;
		output.Write(enter);
		try
		{
			var view = new LocalStackBoardView { Actions = onAction != null };
			string last = null;
			while (!_done.Task.IsCompleted && !token.IsCancellationRequested)
			{
				int width = Math.Max(20, console.Profile.Width), height = Math.Max(8, console.Profile.Height);
				LocalStackBoardSnapshot snapshot;
				lock (_lock)
					snapshot = new LocalStackBoardSnapshot
					{
						stack = _snapshot.stack, up = _snapshot.up, host = _snapshot.host, portalUrl = _snapshot.portalUrl,
						webPackages = _snapshot.webPackages,
						rows = _snapshot.rows.Select(Copy).ToList()
					};

				var recent = LastLines(messages.Text, 3);
				var extra = recent.Count + (snapshot.webPackages != null ? 1 : 0);
				foreach (var key in ReadKeys(console))
				{
					var action = view.Press(key, snapshot.rows, VisibleCount(height, extra + (view.Bar != null ? 1 : 0)), snapshot.up);
					if (action != null) _ = Act(action.Value.action, action.Value.step);
				}

				// Sized after the keys, which can add or drop the bar line, so the frame never outgrows the screen.
				var visible = VisibleCount(height, extra + (view.Bar != null ? 1 : 0));
				var frame = Draw(Frame(snapshot, width, view.Offset(snapshot.rows, visible), visible, recent, view.Following, _now(),
					view.SelectedName(snapshot.rows), view.Bar, keys, marks: view.Actions), console, width, height);

				// Repainting an unchanged frame is what flickers; the terminal paints a changed one in one go.
				if (frame != last)
				{
					output.Write("\u001b[?2026h" + frame + "\u001b[?2026l");
					output.Flush();
					last = frame;
				}

				await Task.WhenAny(_done.Task, Task.Delay(150, CancellationToken.None));
			}
		}
		finally
		{
			output.Write(leave);
			output.Flush();
			AnsiConsole.Console = console;
			Console.CancelKeyPress -= restore;

			var text = messages.Text;
			if (!string.IsNullOrWhiteSpace(text))
				output.Write(text.EndsWith('\n') ? text : text + Environment.NewLine);
		}

		// Off the frame loop, so the board keeps drawing (Restarting, then Ready) while the action runs.
		async Task Act(string action, string step)
		{
			try
			{
				var answer = await Task.Run(() => onAction(action, step));
				if (!string.IsNullOrWhiteSpace(answer)) messages.Writer.WriteLine(answer);
			}
			catch (Exception e)
			{
				messages.Writer.WriteLine($"Couldn't {action} {step}: {e.Message}");
			}
		}
	}

	/// <summary>The keys pressed since the last frame; none when the console has no keyboard.</summary>
	private static List<ConsoleKeyInfo> ReadKeys(IAnsiConsole console)
	{
		var keys = new List<ConsoleKeyInfo>();
		try
		{
			while (console.Input.IsKeyAvailable() && console.Input.ReadKey(intercept: true) is { } key)
				keys.Add(key);
		}
		catch
		{
			// Input redirected: nothing to scroll with, the view keeps following.
		}

		return keys;
	}

	/// <summary>
	/// Draws a frame over the previous one: from the top-left corner, each line cleared to its end, and everything
	/// below the last line cleared, so nothing from the previous frame is left. It stays off the last column and the
	/// last row: writing there makes the terminal wrap or scroll the screen, which showed as a blank frame.
	/// </summary>
	public static string Draw(IRenderable frame, IAnsiConsole console, int width, int height)
	{
		var buffer = new StringWriter();
		var painter = AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = AnsiSupport.Yes,
			ColorSystem = (ColorSystemSupport)console.Profile.Capabilities.ColorSystem,
			Interactive = InteractionSupport.No,
			Out = new AnsiConsoleOutput(buffer)
		});
		painter.Profile.Width = width - 1;
		painter.Write(frame);

		var lines = buffer.ToString().Replace("\r", string.Empty).TrimEnd('\n').Split('\n').Take(height - 1);
		return "\u001b[H" + string.Join("\u001b[K\n", lines) + "\u001b[K\u001b[J";
	}

	/// <summary>Ends <see cref="ShowLive"/>; the caller prints the summary after it returns.</summary>
	public void Complete() => _done.TrySetResult();

	/// <summary>"3 of 8 ready, 1 failed": what the board's last line and the summary say.</summary>
	public string Summary()
	{
		lock (_lock) return Summary(_snapshot.rows);
	}

	public static string Summary(IReadOnlyCollection<LocalStackBoardRow> rows)
	{
		var up = rows.Count(r => r.state is LocalStackStepState.Ready or LocalStackStepState.Running
			or LocalStackStepState.Degraded or LocalStackStepState.Done or LocalStackStepState.UpToDate);
		var text = $"{up} of {rows.Count} ready";
		var failed = rows.Count(r => r.state == LocalStackStepState.Failed);
		if (failed > 0) text += $", {failed} failed";
		var stopped = rows.Count(r => r.state == LocalStackStepState.Stopped);
		if (stopped > 0) text += $", {stopped} stopped";
		var degraded = rows.Count(r => r.state == LocalStackStepState.Degraded);
		if (degraded > 0) text += $", {degraded} degraded";
		return text;
	}

	/// <summary>Rows of the board that fit on a screen <paramref name="height"/> lines tall, besides the title, the
	/// summary, the help line, <paramref name="messages"/> message lines and the last row, which is left empty.</summary>
	public static int VisibleCount(int height, int messages) => Math.Max(1, height - 4 - messages);

	/// <summary>
	/// One full-screen frame: the title with which rows show, <paramref name="visible"/> rows from
	/// <paramref name="offset"/> with the <paramref name="selected"/> one marked, the summary (and where the stack is,
	/// once it is up), the latest messages, the <paramref name="bar"/> asking about the selected row, and the keys.
	/// </summary>
	public static IRenderable Frame(LocalStackBoardSnapshot snapshot, int width, int offset, int visible,
		IReadOnlyList<string> messages, bool following, DateTime nowUtc, string selected = null, string bar = null, string keys = null,
		bool marks = false)
	{
		var count = snapshot.rows.Count;
		offset = Math.Clamp(offset, 0, Math.Max(0, count - visible));
		var shown = Math.Min(visible, count - offset);
		var range = count > visible ? $"  [grey]rows {offset + 1}-{offset + shown} of {count}{(following ? ", following" : "")}[/]" : string.Empty;

		var parts = new List<IRenderable>
		{
			new Markup(LocalStackStyle.H1Text($"Running stack {snapshot.stack ?? string.Empty}") + range),
			Grid(snapshot.rows.Skip(offset).Take(shown).ToList(), snapshot.rows, width, nowUtc, selected, marks || selected != null),
			new Markup($"[grey]{Markup.Escape(Fit(Summary(snapshot.rows) + Where(snapshot), width - 1).TrimEnd())}[/]")
		};
		if (WebPackagesLine(snapshot) is { } web)
			parts.Add(new Markup($"[grey]{Markup.Escape(Fit(web, width - 1).TrimEnd())}[/]"));
		foreach (var message in messages)
			parts.Add(new Markup($"[yellow]{Markup.Escape(Fit(message, width - 1).TrimEnd())}[/]"));
		if (bar != null)
			parts.Add(new Markup($"[bold]{Markup.Escape(Fit(bar, width - 1).TrimEnd())}[/]"));

		keys ??= "Up/Down, PgUp/PgDn, Home/End scroll; F follows the steps in progress; Ctrl+C stops the stack";
		parts.Add(new Markup($"[grey]{Markup.Escape(Fit(keys, width - 1).TrimEnd())}[/]"));
		return new Rows(parts);
	}

	/// <summary>Once the stack is up, where it is: what the summary printed after the board used to say.</summary>
	private static string Where(LocalStackBoardSnapshot snapshot)
	{
		if (!snapshot.up) return string.Empty;
		var parts = new List<string>();
		if (!string.IsNullOrWhiteSpace(snapshot.portalUrl)) parts.Add("portal " + snapshot.portalUrl);
		if (!string.IsNullOrWhiteSpace(snapshot.host)) parts.Add("backend " + snapshot.host);
		return parts.Count == 0 ? ". Up." : ". Up: " + string.Join(", ", parts) + ".";
	}

	/// <summary>The last <paramref name="count"/> non-empty lines of <paramref name="text"/>.</summary>
	public static List<string> LastLines(string text, int count) =>
		text.Replace("\r", string.Empty).Split('\n').Where(l => !string.IsNullOrWhiteSpace(l)).TakeLast(count).ToList();

	/// <summary>
	/// Where the view starts when it follows the stack: two rows above the first step still in progress (or, when
	/// none is, the first one waiting), so what is happening now is on screen with a little of what came before.
	/// </summary>
	public static int FollowOffset(IReadOnlyList<LocalStackBoardRow> rows, int visible)
	{
		var list = rows.ToList();
		var active = list.FindIndex(r => r.state is LocalStackStepState.Building or LocalStackStepState.Starting
			or LocalStackStepState.Restarting);
		if (active < 0) active = list.FindIndex(r => r.state == LocalStackStepState.Waiting);
		if (active < 0) active = list.Count;
		return Math.Clamp(active - 2, 0, Math.Max(0, list.Count - visible));
	}

	/// <summary>The whole board for <paramref name="snapshot"/>, every row: what <c>beam local ps</c> prints.</summary>
	public static IRenderable Render(LocalStackBoardSnapshot snapshot, int width, DateTime nowUtc) =>
		new Rows(new IRenderable[]
		{
			LocalStackStyle.H1Rule($"Running stack {snapshot.stack ?? string.Empty}"),
			Grid(snapshot.rows, snapshot.rows, width, nowUtc),
			new Markup($"[grey]{Markup.Escape(Summary(snapshot.rows))}[/]")
		}.Concat(WebPackagesLine(snapshot) is { } web ? new[] { new Markup($"[grey]{Markup.Escape(web)}[/]") } : Array.Empty<IRenderable>()));

	/// <summary>Which local builds the extensions use, on a line of its own: a row's last column is too narrow for
	/// two versions. Null without local web packages.</summary>
	public static string WebPackagesLine(LocalStackBoardSnapshot snapshot) =>
		snapshot.webPackages is { Count: > 0 } packages
			? "Local web packages: " + string.Join(", ", packages.Select(p => $"{p.Key} {p.Value}"))
			: null;

	/// <summary><paramref name="rows"/> as a grid, sized for all of <paramref name="all"/> so scrolling doesn't move the
	/// columns. With <paramref name="marks"/> every row keeps room for the marker, so selecting one doesn't widen the
	/// rows; the <paramref name="selected"/> row is marked with <c>&gt;</c> and shown inverted.</summary>
	private static Grid Grid(IReadOnlyList<LocalStackBoardRow> rows, IReadOnlyList<LocalStackBoardRow> all, int width, DateTime nowUtc,
		string selected = null, bool marks = false)
	{
		var marker = marks ? 2 : 0;
		var narrow = width < NarrowWidth;
		var grid = new Grid();
		grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
		if (!narrow) grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
		grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
		if (!narrow)
		{
			grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
			grid.AddColumn(new GridColumn().NoWrap().PadRight(2));
			grid.AddColumn(new GridColumn().NoWrap());
		}

		var labelWidth = Math.Min(narrow ? Math.Max(10, width - 14 - marker) : 34,
			all.Select(r => (r.label ?? r.name ?? string.Empty).Length).DefaultIfEmpty(10).Max());

		foreach (var row in rows)
		{
			var text = Markup.Escape(Fit(row.label ?? row.name ?? string.Empty, labelWidth));
			var isSelected = selected != null && string.Equals(row.name, selected, StringComparison.OrdinalIgnoreCase);
			var label = new Markup(!marks ? text : isSelected ? $"[invert]> {text}[/]" : $"  {text}");
			var state = new Markup($"[{Colour(row.state)}]{Fit(Text(row.state), 10)}[/]");
			if (narrow)
			{
				grid.AddRow(label, state);
				continue;
			}

			grid.AddRow(label,
				new Markup(Bar(row, nowUtc)),
				state,
				new Markup(Markup.Escape(Fit(Time(row, nowUtc), 10))),
				new Markup(Fit(row.port > 0 ? $":{row.port}" : string.Empty, 6)),
				new Markup($"[grey]{Markup.Escape(Fit(row.detail ?? string.Empty, Math.Max(10, width - labelWidth - marker - 54)).TrimEnd())}[/]"));
		}

		return grid;
	}

	/// <summary>The bar: empty while waiting, the time against the timeout while starting, a moving block while
	/// building (builds don't report a percentage), full once settled.</summary>
	public static string Bar(LocalStackBoardRow row, DateTime nowUtc)
	{
		int filled;
		switch (row.state)
		{
			case LocalStackStepState.Waiting:
				filled = 0;
				break;
			case LocalStackStepState.Building:
			case LocalStackStepState.Starting when row.timeoutSeconds <= 0 || row.once:
			{
				// Indeterminate: a block of a quarter of the bar sliding along it.
				var at = row.ElapsedSeconds(nowUtc) % BarWidth;
				var cells = Enumerable.Range(0, BarWidth).Select(i => (i - at + BarWidth) % BarWidth < BarWidth / 4 ? Full : Empty);
				return $"[blue]{new string(cells.ToArray())}[/]";
			}
			case LocalStackStepState.Starting:
			case LocalStackStepState.Restarting:
				filled = (int)Math.Round(BarWidth * Math.Min(1.0, (double)row.ElapsedSeconds(nowUtc) / row.timeoutSeconds));
				break;
			default:
				filled = BarWidth;
				break;
		}

		return $"[{Colour(row.state)}]{new string(Full, filled)}[/][grey]{new string(Empty, BarWidth - filled)}[/]";
	}

	private const char Full = '#';
	private const char Empty = '-';

	/// <summary>"41s / 180s" while starting against a timeout, "38s" otherwise, nothing while waiting.</summary>
	public static string Time(LocalStackBoardRow row, DateTime nowUtc)
	{
		if (row.startedUtc == null || row.state == LocalStackStepState.UpToDate) return string.Empty;
		var elapsed = row.ElapsedSeconds(nowUtc);
		return row.state is LocalStackStepState.Starting or LocalStackStepState.Restarting && row.timeoutSeconds > 0 && !row.once
			? $"{elapsed}s / {row.timeoutSeconds}s"
			: $"{elapsed}s";
	}

	/// <summary>One state change without a terminal: <c>12:01:44  Scala service auth  Starting -> Ready (41s)  detail</c>.</summary>
	public static string Line(LocalStackBoardRow row, LocalStackStepState from, DateTime nowUtc)
	{
		var line = $"{nowUtc.ToLocalTime():HH:mm:ss}  {row.label ?? row.name}  {Text(from)} -> {Text(row.state)}";
		if (row.Settled) line += $" ({row.ElapsedSeconds(nowUtc)}s)";
		if (!string.IsNullOrWhiteSpace(row.detail)) line += $"  {row.detail}";
		return line;
	}

	/// <summary>What a failure prints: what failed, its last log lines, and where to read the rest.</summary>
	public static List<string> FailureBlock(LocalStackBoardRow row, IReadOnlyList<string> tail, string hint)
	{
		var lines = new List<string> { $"x {row.label ?? row.name} failed{(string.IsNullOrWhiteSpace(row.detail) ? "" : ": " + row.detail)}" };
		foreach (var line in tail ?? Array.Empty<string>())
			lines.Add("    " + line);
		if (!string.IsNullOrWhiteSpace(hint))
			lines.Add("    " + hint);
		return lines;
	}

	/// <summary>How a state reads on the board: its name, with "Up to date" spelled out.</summary>
	public static string Text(LocalStackStepState state) => state == LocalStackStepState.UpToDate ? "Up to date" : state.ToString();

	public static string Colour(LocalStackStepState state) => state switch
	{
		LocalStackStepState.Waiting => "grey",
		LocalStackStepState.Building => "blue",
		LocalStackStepState.Starting => "yellow",
		LocalStackStepState.Restarting => "yellow",
		LocalStackStepState.Degraded => "darkorange",
		LocalStackStepState.Failed => "red",
		LocalStackStepState.Stopped => "red",
		_ => "green"
	};

	private static string Fit(string text, int width) =>
		text.Length <= width ? text.PadRight(width) : text.Substring(0, Math.Max(1, width - 3)) + "...";

	private static LocalStackBoardRow Copy(LocalStackBoardRow r) => new LocalStackBoardRow
	{
		name = r.name, label = r.label, state = r.state, port = r.port, timeoutSeconds = r.timeoutSeconds,
		detail = r.detail, startedUtc = r.startedUtc, changedUtc = r.changedUtc, once = r.once
	};

	private void Save()
	{
		if (string.IsNullOrEmpty(_file)) return;
		try
		{
			LocalStackBoardIO.Save(_file, _snapshot);
		}
		catch
		{
			// The file only serves `beam local ps`; a run never fails over it.
		}
	}
}

/// <summary>What is written to the console while the full-screen board shows: its message pane, and what is
/// printed on the normal screen when it closes.</summary>
public sealed class LocalStackBoardMessages
{
	private readonly StringWriter _buffer = new StringWriter();

	/// <summary>Takes writes from any thread, one at a time.</summary>
	public TextWriter Writer { get; }

	public LocalStackBoardMessages() => Writer = TextWriter.Synchronized(_buffer);

	/// <summary>Everything written so far. A synchronized writer locks itself, so reading under the same lock never
	/// sees half a line.</summary>
	public string Text
	{
		get
		{
			lock (Writer) return _buffer.ToString();
		}
	}
}

/// <summary>
/// Where the full-screen board is scrolled to, whether it follows the steps in progress, which row is selected, and
/// what the bar above the keys asks about it.
/// </summary>
public class LocalStackBoardView
{
	private int _offset;
	private int? _selected;
	private (string action, string step)? _pending;

	public bool Following { get; private set; } = true;

	/// <summary>True when the rows can be acted on (Space, Enter): the board was given something to do it with.</summary>
	public bool Actions { get; set; }

	/// <summary>The line above the keys: what Enter would do to the selected row, or why it can't be acted on.</summary>
	public string Bar { get; private set; }

	/// <summary>The keys line of a board whose rows can be acted on.</summary>
	public const string ActionKeys = "Up/Down select, Space acts on it, PgUp/PgDn/Home/End, F follows, Ctrl+C stops the stack";

	/// <summary>The first row to show: where it follows to, or where it was scrolled to.</summary>
	public int Offset(IReadOnlyList<LocalStackBoardRow> rows, int visible) =>
		Following ? LocalStackBoard.FollowOffset(rows, visible) : Math.Clamp(_offset, 0, Math.Max(0, rows.Count - visible));

	/// <summary>The selected row's step, or null when none is.</summary>
	public string SelectedName(IReadOnlyList<LocalStackBoardRow> rows) =>
		_selected is { } index && rows.Count > 0 ? rows[Math.Clamp(index, 0, rows.Count - 1)].name : null;

	/// <summary>
	/// Handles a key. The arrows select a row (on a board without actions they only scroll) and PgUp/PgDn/Home/End
	/// move by a page or to the ends, keeping the selection on screen; any of them stops following, from where the
	/// view is, and F follows again. Space asks about the selected row, Enter does what was asked (S stops instead of
	/// restarting), anything else drops the question. Returns the action to run, or null.
	/// </summary>
	public (string action, string step)? Press(ConsoleKeyInfo key, IReadOnlyList<LocalStackBoardRow> rows, int visible, bool stackUp = false)
	{
		if (_pending is { } pending)
		{
			_pending = null;
			Bar = null;
			if (key.Key == ConsoleKey.Enter) return pending;
			if (key.Key == ConsoleKey.S && pending.action == "restart") return ("stop", pending.step);
			return null;
		}

		Bar = null;
		if (key.Key == ConsoleKey.F)
		{
			Following = true;
			_selected = null;
			return null;
		}

		if (key.Key == ConsoleKey.Spacebar && Actions)
		{
			var name = SelectedName(rows);
			var row = rows.FirstOrDefault(r => r.name == name);
			if (row == null)
			{
				Bar = "Select a step with Up/Down first.";
				return null;
			}

			var (action, text) = ActionFor(row, stackUp);
			Bar = text;
			if (action != null) _pending = (action, row.name);
			return null;
		}

		var top = Offset(rows, visible);
		var last = Math.Max(0, rows.Count - visible);
		if (!Actions)
		{
			int? to = key.Key switch
			{
				ConsoleKey.UpArrow => top - 1,
				ConsoleKey.DownArrow => top + 1,
				ConsoleKey.PageUp => top - visible,
				ConsoleKey.PageDown => top + visible,
				ConsoleKey.Home => 0,
				ConsoleKey.End => last,
				_ => null
			};
			if (to == null) return null;

			Following = false;
			_offset = Math.Clamp(to.Value, 0, last);
			return null;
		}

		if (rows.Count == 0) return null;
		var current = _selected ?? top;
		int? next = key.Key switch
		{
			// The first press selects the top row on screen rather than moving past it.
			ConsoleKey.UpArrow => _selected == null ? current : current - 1,
			ConsoleKey.DownArrow => _selected == null ? current : current + 1,
			ConsoleKey.PageUp => current - visible,
			ConsoleKey.PageDown => current + visible,
			ConsoleKey.Home => 0,
			ConsoleKey.End => rows.Count - 1,
			_ => null
		};
		if (next == null) return null;

		Following = false;
		_offset = top;
		_selected = Math.Clamp(next.Value, 0, rows.Count - 1);
		if (_selected < _offset) _offset = _selected.Value;
		if (_selected >= _offset + visible) _offset = _selected.Value - visible + 1;
		_offset = Math.Clamp(_offset, 0, last);
		return null;
	}

	/// <summary>
	/// What Space offers for <paramref name="row"/>: a service that is up can be restarted (or stopped), one that is
	/// down can be started. A build or a step that runs once isn't restarted on its own, and nothing is while the
	/// stack or the step is still on its way up. The action is null when there is nothing to do; the text says why.
	/// </summary>
	public static (string action, string text) ActionFor(LocalStackBoardRow row, bool stackUp)
	{
		var label = row.label ?? row.name;
		if (row.once)
			return (null, $"{label} runs once. Restart the service that uses it.");
		if (!stackUp)
			return (null, "Wait until the stack is up.");
		return row.state switch
		{
			LocalStackStepState.Ready or LocalStackStepState.Running or LocalStackStepState.Degraded
				=> ("restart", $"Restart {label}?  Enter restart, S stop, Esc cancel"),
			LocalStackStepState.Stopped or LocalStackStepState.Failed
				=> ("start", $"Start {label}?  Enter start, Esc cancel"),
			_ => (null, $"Wait until {label} is up.")
		};
	}
}

/// <summary>Reads and writes the board file next to the run-state.</summary>
public static class LocalStackBoardIO
{
	public const string FileName = "local-stack.board.json";

	/// <summary>The board file for the run-state at <paramref name="runStatePath"/>.</summary>
	public static string PathFor(string runStatePath) =>
		Path.Combine(Path.GetDirectoryName(runStatePath) ?? ".", FileName);

	public static void Save(string path, LocalStackBoardSnapshot snapshot) =>
		LocalStackRunStateIO.WriteShared(path, JsonConvert.SerializeObject(snapshot, Formatting.Indented));

	public static LocalStackBoardSnapshot Load(string path)
	{
		try
		{
			return File.Exists(path) ? JsonConvert.DeserializeObject<LocalStackBoardSnapshot>(LocalStackRunStateIO.ReadShared(path)) : null;
		}
		catch
		{
			return null;
		}
	}

	public static void Clear(string path)
	{
		try
		{
			if (File.Exists(path)) File.Delete(path);
		}
		catch
		{
			// best-effort
		}
	}

	/// <summary>
	/// True when the board is left over from a run that no longer exists: an attached run's board dies with its
	/// process (a crash can leave the file behind). A detached run's board lasts until <c>beam local stop</c>.
	/// </summary>
	public static bool IsStale(LocalStackBoardSnapshot snapshot, Func<int, bool> isAlive = null)
	{
		if (snapshot == null) return true;
		if (!snapshot.attached) return false;
		isAlive ??= pid =>
		{
			try
			{
				using var process = Process.GetProcessById(pid);
				return !process.HasExited;
			}
			catch
			{
				return false;
			}
		};
		return snapshot.runnerPid <= 0 || !isAlive(snapshot.runnerPid);
	}
}
