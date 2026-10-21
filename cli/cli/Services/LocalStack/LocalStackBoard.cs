using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Spectre.Console;
using Spectre.Console.Rendering;
using System.Diagnostics;

namespace cli.Services.LocalStack;

/// <summary>Where a step is while a stack starts: one value per row of the progress board.</summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum LocalStackStepState
{
	/// <summary>An earlier step isn't ready yet.</summary>
	Waiting,

	/// <summary>A build step is running.</summary>
	Building,

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
		or LocalStackStepState.Done or LocalStackStepState.Failed or LocalStackStepState.Stopped;

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
	public List<LocalStackBoardRow> rows = new List<LocalStackBoardRow>();
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

	private LocalStackBoardRow Find(string step) =>
		_snapshot.rows.FirstOrDefault(r => string.Equals(r.name, step, StringComparison.OrdinalIgnoreCase));

	/// <summary>
	/// Shows the board full screen, on the terminal's alternate screen, until <see cref="Complete"/> (or
	/// <paramref name="token"/>), then puts the normal screen back. Every row stays reachable: the arrow keys,
	/// PgUp/PgDn and Home/End scroll, and until one of them is pressed the view follows the steps in progress (F
	/// follows again). What else is written while it shows (warnings, a failure's lines) goes to a pane at its
	/// bottom, and is printed again on the normal screen when the board closes, so it stays in the terminal's
	/// history. Ctrl+C puts the normal screen back too. Lines and silent mode return at once.
	/// </summary>
	public async Task ShowLive(CancellationToken token)
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
			var view = new LocalStackBoardView();
			while (!_done.Task.IsCompleted && !token.IsCancellationRequested)
			{
				int width = Math.Max(20, console.Profile.Width), height = Math.Max(8, console.Profile.Height);
				LocalStackBoardSnapshot snapshot;
				lock (_lock) snapshot = new LocalStackBoardSnapshot { stack = _snapshot.stack, rows = _snapshot.rows.Select(Copy).ToList() };

				var recent = LastLines(messages.Text, 3);
				var visible = VisibleCount(height, recent.Count);
				foreach (var key in ReadKeys(console))
					view.Press(key, snapshot.rows, visible);

				output.Write(Draw(Frame(snapshot, width, view.Offset(snapshot.rows, visible), visible, recent, view.Following, _now()),
					console, width, height));
				output.Flush();
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
	/// below the last line cleared, so nothing from the previous frame is left and nothing flickers.
	/// </summary>
	private static string Draw(IRenderable frame, IAnsiConsole console, int width, int height)
	{
		var buffer = new StringWriter();
		var painter = AnsiConsole.Create(new AnsiConsoleSettings
		{
			Ansi = AnsiSupport.Yes,
			ColorSystem = (ColorSystemSupport)console.Profile.Capabilities.ColorSystem,
			Interactive = InteractionSupport.No,
			Out = new AnsiConsoleOutput(buffer)
		});
		painter.Profile.Width = width;
		painter.Write(frame);

		var lines = buffer.ToString().Replace("\r", string.Empty).TrimEnd('\n').Split('\n').Take(height);
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
			or LocalStackStepState.Degraded or LocalStackStepState.Done);
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
	/// summary, the help line and <paramref name="messages"/> message lines.</summary>
	public static int VisibleCount(int height, int messages) => Math.Max(1, height - 3 - messages);

	/// <summary>
	/// One full-screen frame: the title with which rows show, <paramref name="visible"/> rows from
	/// <paramref name="offset"/>, the summary, the latest messages and the keys.
	/// </summary>
	public static IRenderable Frame(LocalStackBoardSnapshot snapshot, int width, int offset, int visible,
		IReadOnlyList<string> messages, bool following, DateTime nowUtc)
	{
		var count = snapshot.rows.Count;
		offset = Math.Clamp(offset, 0, Math.Max(0, count - visible));
		var shown = Math.Min(visible, count - offset);
		var range = count > visible ? $"  [grey]rows {offset + 1}-{offset + shown} of {count}{(following ? ", following" : "")}[/]" : string.Empty;

		var parts = new List<IRenderable>
		{
			new Markup($"[bold]Running stack {Markup.Escape(snapshot.stack ?? string.Empty)}[/]{range}"),
			Grid(snapshot.rows.Skip(offset).Take(shown).ToList(), snapshot.rows, width, nowUtc),
			new Markup($"[grey]{Markup.Escape(Summary(snapshot.rows))}[/]")
		};
		foreach (var message in messages)
			parts.Add(new Markup($"[yellow]{Markup.Escape(Fit(message, width - 1).TrimEnd())}[/]"));

		const string keys = "Up/Down, PgUp/PgDn, Home/End scroll; F follows the steps in progress; Ctrl+C stops the stack";
		parts.Add(new Markup($"[grey]{Markup.Escape(Fit(keys, width - 1).TrimEnd())}[/]"));
		return new Rows(parts);
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
		new Rows(
			new Markup($"[bold]Running stack {Markup.Escape(snapshot.stack ?? string.Empty)}[/]"),
			Grid(snapshot.rows, snapshot.rows, width, nowUtc),
			new Markup($"[grey]{Markup.Escape(Summary(snapshot.rows))}[/]"));

	/// <summary><paramref name="rows"/> as a grid, sized for all of <paramref name="all"/> so scrolling doesn't move the columns.</summary>
	private static Grid Grid(IReadOnlyList<LocalStackBoardRow> rows, IReadOnlyList<LocalStackBoardRow> all, int width, DateTime nowUtc)
	{
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

		var labelWidth = Math.Min(narrow ? Math.Max(10, width - 14) : 34,
			all.Select(r => (r.label ?? r.name ?? string.Empty).Length).DefaultIfEmpty(10).Max());

		foreach (var row in rows)
		{
			var label = new Markup(Markup.Escape(Fit(row.label ?? row.name ?? string.Empty, labelWidth)));
			var state = new Markup($"[{Colour(row.state)}]{Fit(row.state.ToString(), 10)}[/]");
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
				new Markup($"[grey]{Markup.Escape(Fit(row.detail ?? string.Empty, Math.Max(10, width - labelWidth - 54)).TrimEnd())}[/]"));
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
		if (row.startedUtc == null) return string.Empty;
		var elapsed = row.ElapsedSeconds(nowUtc);
		return row.state is LocalStackStepState.Starting or LocalStackStepState.Restarting && row.timeoutSeconds > 0 && !row.once
			? $"{elapsed}s / {row.timeoutSeconds}s"
			: $"{elapsed}s";
	}

	/// <summary>One state change without a terminal: <c>12:01:44  Scala service auth  Starting -> Ready (41s)  detail</c>.</summary>
	public static string Line(LocalStackBoardRow row, LocalStackStepState from, DateTime nowUtc)
	{
		var line = $"{nowUtc.ToLocalTime():HH:mm:ss}  {row.label ?? row.name}  {from} -> {row.state}";
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

/// <summary>Where the full-screen board is scrolled to, and whether it follows the steps in progress.</summary>
public class LocalStackBoardView
{
	private int _offset;

	public bool Following { get; private set; } = true;

	/// <summary>The first row to show: where it follows to, or where it was scrolled to.</summary>
	public int Offset(IReadOnlyList<LocalStackBoardRow> rows, int visible) =>
		Following ? LocalStackBoard.FollowOffset(rows, visible) : Math.Clamp(_offset, 0, Math.Max(0, rows.Count - visible));

	/// <summary>Scrolls for a key. Any scroll stops following, from where the view is; F follows again.</summary>
	public void Press(ConsoleKeyInfo key, IReadOnlyList<LocalStackBoardRow> rows, int visible)
	{
		if (key.Key == ConsoleKey.F)
		{
			Following = true;
			return;
		}

		var current = Offset(rows, visible);
		var last = Math.Max(0, rows.Count - visible);
		int? to = key.Key switch
		{
			ConsoleKey.UpArrow => current - 1,
			ConsoleKey.DownArrow => current + 1,
			ConsoleKey.PageUp => current - visible,
			ConsoleKey.PageDown => current + visible,
			ConsoleKey.Home => 0,
			ConsoleKey.End => last,
			_ => null
		};
		if (to == null) return;

		Following = false;
		_offset = Math.Clamp(to.Value, 0, last);
	}
}

/// <summary>Reads and writes the board file next to the run-state.</summary>
public static class LocalStackBoardIO
{
	public const string FileName = "local-stack.board.json";

	/// <summary>The board file for the run-state at <paramref name="runStatePath"/>.</summary>
	public static string PathFor(string runStatePath) =>
		Path.Combine(Path.GetDirectoryName(runStatePath) ?? ".", FileName);

	public static void Save(string path, LocalStackBoardSnapshot snapshot)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(path)!);
		var temp = path + ".tmp";
		File.WriteAllText(temp, JsonConvert.SerializeObject(snapshot, Formatting.Indented));
		File.Move(temp, path, overwrite: true);
	}

	public static LocalStackBoardSnapshot Load(string path)
	{
		try
		{
			return File.Exists(path) ? JsonConvert.DeserializeObject<LocalStackBoardSnapshot>(File.ReadAllText(path)) : null;
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
