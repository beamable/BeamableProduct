using System.Text;

namespace cli.Services.Web.CodeGen;

/// <summary>
/// Utility for building indented, multi-line text (ideal for source-code generation).
/// </summary>
public sealed class TsCodeWriter
{
	// Buffer accumulating all written content
	private readonly StringBuilder _buffer = new();

	private const string INDENT_UNIT = "  ";

	// How many times to repeat IndentUnit at the start of each new line
	private int _indentLevel;

	// Indicates the next write is at the start of a line
	private bool _atLineStart = true;

	/// <summary>
	/// Appends <paramref name="text"/> preserving any '\n' characters in <paramref name="text"/>
	/// with no trailing line break.
	/// Applies indentation based on the indent depth.
	/// </summary>
	/// <param name="text">The text to append.</param>
	/// <returns>The same <see cref="TsCodeWriter"/> instance (for chaining).</returns>
	public TsCodeWriter Write(string text)
	{
		// Normalize Windows CRLF into LF only
		string normalized = text.Replace("\r\n", "\n");
		string[] chunks = normalized.Split('\n');

		for (int i = 0; i < chunks.Length; i++)
		{
			// Apply indentation based on the indent level when starting a new line
			WriteIndentIfNeeded();

			// Write the chunk
			_buffer.Append(chunks[i]);

			if (i < chunks.Length - 1)
			{
				// There was a newline in the original text here
				_buffer.Append('\n');
				_atLineStart = true;
			}
			else
			{
				// Last chunk, do not append a newline
				_atLineStart = false;
			}
		}

		return this;
	}

	/// <summary>
	/// Appends <paramref name="line"/> and then adds a line break.
	/// Applies indentation based on the indent depth.
	/// </summary>
	/// <param name="line">The line to append (without its own newline).</param>
	/// <returns>The same <see cref="TsCodeWriter"/> instance (for chaining).</returns>
	public TsCodeWriter WriteLine(string line = "")
	{
		// Apply indentation based on the indent level when starting a new line
		WriteIndentIfNeeded();
		_buffer.Append(line);
		_buffer.Append('\n');
		_atLineStart = true;
		return this;
	}

	/// <summary>
	/// Increases the current indentation depth by one level.
	/// </summary>
	/// <returns>The same <see cref="TsCodeWriter"/> instance (for chaining).</returns>
	public TsCodeWriter Indent()
	{
		_indentLevel++;
		return this;
	}

	/// /// <summary>
	/// Decreases the current indentation depth by one level (to a minimum of zero).
	/// </summary>
	/// <returns>The same <see cref="TsCodeWriter"/> instance (for chaining).</returns>
	public TsCodeWriter Outdent()
	{
		if (_indentLevel > 0)
			_indentLevel--;

		return this;
	}

	/// <summary>
	/// Returns all text written so far, including line breaks and indentation.
	/// </summary>
	/// <returns>The accumulated string.</returns>
	public override string ToString() => _buffer.ToString();

	/// <summary>
	/// If we are at the start of a line, prepend the proper number of indent tokens.
	/// </summary>
	private void WriteIndentIfNeeded()
	{
		if (!_atLineStart)
			return;

		// Write the indentation based on the current indent level
		_buffer.Append(string.Concat(Enumerable.Repeat(INDENT_UNIT, _indentLevel)));
		_atLineStart = false;
	}
}
