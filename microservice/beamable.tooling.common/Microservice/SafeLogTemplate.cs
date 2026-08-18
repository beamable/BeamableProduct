using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace Beamable.Server
{
	/// <summary>
	/// Guards the structured-logging entry points against malformed message templates.
	/// <para/>
	/// <see cref="Microsoft.Extensions.Logging"/> treats the message given to
	/// <see cref="LoggerExtensions.Log(ILogger,LogLevel,string,object[])"/> as a <i>template</i>: every
	/// <c>{name}</c> is rewritten into a positional hole and the result is handed to
	/// <see cref="string.Format(string,object[])"/>. A template that declares more holes than there are
	/// arguments - or that contains a stray brace - throws a <see cref="FormatException"/> per registered log
	/// provider, wrapped in an <see cref="AggregateException"/>. That is raised while the log is being written,
	/// so it surfaces at the caller rather than at the logger, and it destroys whatever the caller was trying
	/// to report.
	/// <para/>
	/// The usual cause is text that is not under the caller's control - an exception message, a json fragment,
	/// a user-supplied id - being interpolated into the template instead of passed as an argument. Callers
	/// should always pass such text as an argument, but this type keeps a mistake from taking down the code
	/// that is merely trying to log.
	/// </summary>
	public static class SafeLogTemplate
	{
		private static readonly char[] FormatDelimiters = { ',', ':' };

		/// <summary>
		/// Writes a structured log message, degrading to a flat message rather than throwing when the template
		/// and the arguments disagree.
		/// </summary>
		public static void Write(ILogger logger, LogLevel level, string template, object[] args)
		{
			if (logger == null)
			{
				return;
			}

			if (args == null || args.Length == 0)
			{
				// With no arguments the template is never parsed, so any braces in it are already literal.
				logger.Log(level, template);
				return;
			}

			if (!logger.IsEnabled(level))
			{
				return;
			}

			// The template is checked here, up front, so that a broken one is caught while this frame is still
			// on the stack. A try/catch around the write below would not be enough on its own: a queued logger
			// defers formatting until its flush, and an otel exporter until its export. Only the template and the
			// argument count are inspected - never the argument values, which must not be rendered twice and must
			// not be able to throw from inside this guard.
			if (!CanRender(template, args.Length))
			{
				logger.Log(level, Flatten(template, args));
				return;
			}

			try
			{
				logger.Log(level, template, args);
			}
			catch (Exception)
			{
				// Should be unreachable given the check above, but a log call is never worth an exception.
				logger.Log(level, Flatten(template, args));
			}
		}

		/// <summary>
		/// Reports whether <see cref="Microsoft.Extensions.Logging"/> would be able to render the given message
		/// template with the given arguments. Only how many arguments there are matters, never their values.
		/// </summary>
		public static bool CanRender(string template, object[] args)
		{
			return CanRender(template, args?.Length ?? 0);
		}

		/// <summary>
		/// Reports whether <see cref="Microsoft.Extensions.Logging"/> would be able to render the given message
		/// template against this many arguments.
		/// <para/>
		/// The argument values are deliberately not involved. Rendering them here would run application
		/// <c>ToString()</c> code a second time on every successful log call, and a <c>ToString()</c> that throws
		/// would defeat the whole purpose of this class. Placeholder nulls stand in for the real values, which is
		/// also how the BEAM_LOG analyzer checks the same templates at compile time.
		/// </summary>
		public static bool CanRender(string template, int argCount)
		{
			if (string.IsNullOrEmpty(template))
			{
				// A null or empty message is never parsed; the framework renders null as "[null]".
				return true;
			}

			var positionalFormat = ToPositionalFormat(template, out var holeCount);
			if (holeCount > argCount)
			{
				// The usual failure: text interpolated into the template brought its own braces along and so
				// declared more holes than the caller passed arguments. Caught without formatting anything.
				return false;
			}

			try
			{
				// Catches what a hole count cannot: a stray brace, or a bad alignment such as "{0,zz}".
				string.Format(CultureInfo.InvariantCulture, positionalFormat, new object[argCount]);
				return true;
			}
			catch (Exception)
			{
				return false;
			}
		}

		/// <summary>
		/// Rewrites a message template's named holes into the positional holes that
		/// <see cref="string.Format(string,object[])"/> understands, turning <c>"a {name} b"</c> into
		/// <c>"a {0} b"</c>.
		/// <para/>
		/// This mirrors the framework's internal <c>LogValuesFormatter</c>, including its quirks: doubled braces
		/// are escapes, an unclosed brace ends the scan, and a template with no holes at all is passed through
		/// untouched - which is why a stray <c>}</c> can still fail to format.
		/// </summary>
		public static string ToPositionalFormat(string template)
		{
			return ToPositionalFormat(template, out _);
		}

		/// <summary>
		/// The same rewrite as <see cref="ToPositionalFormat(string)"/>, also reporting how many holes the
		/// template declares. That count is what says whether the caller passed enough arguments, and it can be
		/// compared without rendering anything.
		/// </summary>
		public static string ToPositionalFormat(string template, out int holeCount)
		{
			holeCount = 0;
			if (string.IsNullOrEmpty(template))
			{
				return template;
			}

			var builder = new StringBuilder(template.Length);
			var scanIndex = 0;
			var endIndex = template.Length;

			while (scanIndex < endIndex)
			{
				var openBraceIndex = FindBraceIndex(template, '{', scanIndex, endIndex);
				if (scanIndex == 0 && openBraceIndex == endIndex)
				{
					// No holes anywhere; the framework uses the template as-is.
					return template;
				}

				var closeBraceIndex = FindBraceIndex(template, '}', openBraceIndex, endIndex);
				if (closeBraceIndex == endIndex)
				{
					// An unclosed brace; the rest of the template is literal text.
					builder.Append(template, scanIndex, endIndex - scanIndex);
					scanIndex = endIndex;
				}
				else
				{
					// Hole syntax is { name[,alignment][:format] }; only the name becomes an index.
					var formatDelimiterIndex = template.IndexOfAny(FormatDelimiters, openBraceIndex, closeBraceIndex - openBraceIndex);
					if (formatDelimiterIndex < 0)
					{
						formatDelimiterIndex = closeBraceIndex;
					}

					builder.Append(template, scanIndex, openBraceIndex - scanIndex + 1);
					builder.Append(holeCount);
					builder.Append(template, formatDelimiterIndex, closeBraceIndex - formatDelimiterIndex + 1);

					holeCount++;
					scanIndex = closeBraceIndex + 1;
				}
			}

			return builder.ToString();
		}

		/// <summary>
		/// Renders a template and its arguments as a single unstructured message. The template is emitted
		/// verbatim, which is safe because a message logged without arguments is never parsed for holes.
		/// </summary>
		private static string Flatten(string template, object[] args)
		{
			var builder = new StringBuilder(template);
			builder.Append("\n[unformatted log arguments: ");
			for (var i = 0; i < args.Length; i++)
			{
				if (i > 0)
				{
					builder.Append(", ");
				}

				builder.Append(SafeToString(args[i]));
			}

			builder.Append("]");
			return builder.ToString();
		}

		/// <summary>
		/// Renders a single value without ever throwing. An argument's <c>ToString()</c> is application code and
		/// is free to fail; when it does, the template and the remaining arguments still have to reach the log.
		/// </summary>
		internal static string SafeToString(object value)
		{
			if (value == null)
			{
				return "[null]";
			}

			try
			{
				return value.ToString();
			}
			catch (Exception e)
			{
				return $"[unrenderable {value.GetType().Name}: {e.GetType().Name}]";
			}
		}

		/// <summary>
		/// Finds the next unescaped occurrence of <paramref name="brace"/>, or <paramref name="endIndex"/> when
		/// there is none. Doubled braces are escapes and are skipped over.
		/// </summary>
		private static int FindBraceIndex(string template, char brace, int startIndex, int endIndex)
		{
			var braceIndex = endIndex;
			var scanIndex = startIndex;
			var braceOccurrenceCount = 0;

			while (scanIndex < endIndex)
			{
				if (braceOccurrenceCount > 0 && template[scanIndex] != brace)
				{
					if (braceOccurrenceCount % 2 == 0)
					{
						// An even number of braces; they escaped each other, so keep looking.
						braceOccurrenceCount = 0;
						braceIndex = endIndex;
					}
					else
					{
						// An unescaped brace.
						break;
					}
				}
				else if (template[scanIndex] == brace)
				{
					if (brace == '}')
					{
						if (braceOccurrenceCount == 0)
						{
							// For a closing brace, the first occurrence wins.
							braceIndex = scanIndex;
						}
					}
					else
					{
						// For an opening brace, the last occurrence wins.
						braceIndex = scanIndex;
					}

					braceOccurrenceCount++;
				}

				scanIndex++;
			}

			return braceIndex;
		}
	}
}
