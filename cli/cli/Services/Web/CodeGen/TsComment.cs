namespace cli.Services.Web.CodeGen;

/// <summary>
/// Comment styles supported in TypeScript output.
/// </summary>
public enum TsCommentStyle
{
	/// <summary>Single-line comments using <c>// ...</c>.</summary>
	SingleLine,

	/// <summary>Multi-line comments using <c>/* ... */</c>.</summary>
	MultiLine,

	/// <summary>JSDoc comments using <c>/** ... */</c>.</summary>
	Doc
}

/// <summary>
/// Represents a comment in generated TypeScript code.
/// </summary>
public class TsComment : TsNode
{
	/// <summary>
	/// The text of the comment. May contain multiple lines separated by '\n'.
	/// </summary>
	public string Text { get; }

	/// <summary>
	/// The style of comment to emit.
	/// </summary>
	public TsCommentStyle Style { get; }

	/// <summary>
	/// Creates a new comment.
	/// </summary>
	/// <param name="text">The comment text (may include '\n').</param>
	/// <param name="style">The comment style.</param>
	public TsComment(string text, TsCommentStyle style = TsCommentStyle.SingleLine)
	{
		Text = text;
		Style = style;
	}

	public override void Write(TsCodeWriter writer)
	{
		// Split into individual lines
		string[] lines = Text.Replace("\r\n", "\n").Split('\n');
		switch (Style)
		{
			case TsCommentStyle.SingleLine:
				// Emit each line as // ...
				foreach (string line in lines)
					writer.WriteLine("// " + line);
				break;
			case TsCommentStyle.MultiLine:
				// Emit /* ... */
				writer.WriteLine("/*");
				foreach (string line in lines)
					writer.WriteLine(" * " + line);
				writer.WriteLine(" */");
				break;
			case TsCommentStyle.Doc:
				// Emit /** ... */
				writer.WriteLine("/**");
				foreach (string line in lines)
					writer.WriteLine(" * " + line);
				writer.WriteLine(" */");
				break;
			default:
				throw new ArgumentOutOfRangeException();
		}
	}
}
