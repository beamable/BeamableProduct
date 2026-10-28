using System.Text;

namespace cli.Services.CliInvocation;

/// <summary>
/// Captures what a command writes straight to the console (<c>Console.Out</c> / <c>Console.Error</c>) while other
/// commands run in the same process at the same time.
/// <para>
/// Installed once, it puts a router in front of the process's console: a write made from inside a capture (the
/// capture follows the command's async flow, including tasks it starts) goes to that capture; any other write goes
/// where the console pointed before. Swapping <c>Console.Out</c> per command instead would also grab the output of
/// every other command and of the host itself.
/// </para>
/// </summary>
public static class InvocationConsole
{
	private static readonly object InstallLock = new object();
	private static readonly AsyncLocal<Capture> Current = new AsyncLocal<Capture>();
	private static bool _installed;

	/// <summary>One command's console output.</summary>
	public sealed class Capture
	{
		private readonly StringWriter _out = new StringWriter();
		private readonly StringWriter _error = new StringWriter();

		public Capture()
		{
			// A command's own tasks can write at the same time.
			Out = TextWriter.Synchronized(_out);
			Error = TextWriter.Synchronized(_error);
		}

		internal TextWriter Out { get; }
		internal TextWriter Error { get; }

		public string StandardOutput
		{
			get
			{
				lock (Out)
				{
					return _out.ToString();
				}
			}
		}

		public string StandardError
		{
			get
			{
				lock (Error)
				{
					return _error.ToString();
				}
			}
		}
	}

	/// <summary>
	/// Routes the console through the capture, once per process. The console's current writers become the
	/// destination for writes made outside any capture, so call this after the host has set them up.
	/// </summary>
	public static void Install()
	{
		lock (InstallLock)
		{
			// Console.SetOut wraps the writer it is given, so remember the install rather than compare writers.
			if (_installed)
			{
				return;
			}

			Console.SetOut(new RoutingWriter(Console.Out, capture => capture.Out));
			Console.SetError(new RoutingWriter(Console.Error, capture => capture.Error));
			_installed = true;
		}
	}

	/// <summary>
	/// Starts capturing for the current async flow; everything started from it afterwards writes into the returned
	/// capture. Dispose the scope to stop.
	/// </summary>
	public static Scope Begin(out Capture capture)
	{
		Install();
		capture = new Capture();
		var previous = Current.Value;
		Current.Value = capture;
		return new Scope(previous);
	}

	public readonly struct Scope : IDisposable
	{
		private readonly Capture _previous;
		internal Scope(Capture previous) => _previous = previous;
		public void Dispose() => Current.Value = _previous;
	}

	private sealed class RoutingWriter : TextWriter
	{
		private readonly TextWriter _fallback;
		private readonly Func<Capture, TextWriter> _select;

		public RoutingWriter(TextWriter fallback, Func<Capture, TextWriter> select)
		{
			_fallback = fallback;
			_select = select;
		}

		private TextWriter Target => Current.Value is { } capture ? _select(capture) : _fallback;

		public override Encoding Encoding => _fallback.Encoding;
		public override IFormatProvider FormatProvider => _fallback.FormatProvider;

		[System.Diagnostics.CodeAnalysis.AllowNull]
		public override string NewLine
		{
			get => _fallback.NewLine;
			set => _fallback.NewLine = value;
		}

		public override void Write(char value) => Target.Write(value);
		public override void Write(char[] buffer, int index, int count) => Target.Write(buffer, index, count);
		public override void Write(ReadOnlySpan<char> buffer) => Target.Write(buffer);
		public override void Write(string value) => Target.Write(value);
		public override void WriteLine() => Target.WriteLine();
		public override void WriteLine(string value) => Target.WriteLine(value);
		public override void WriteLine(ReadOnlySpan<char> buffer) => Target.WriteLine(buffer);
		public override Task WriteAsync(char value) => Target.WriteAsync(value);
		public override Task WriteAsync(string value) => Target.WriteAsync(value);
		public override Task WriteAsync(char[] buffer, int index, int count) => Target.WriteAsync(buffer, index, count);
		public override Task WriteLineAsync(string value) => Target.WriteLineAsync(value);
		public override Task WriteLineAsync() => Target.WriteLineAsync();
		public override void Flush() => Target.Flush();
		public override Task FlushAsync() => Target.FlushAsync();
	}
}
