namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>for ... in</c> loop.
/// </summary>
public class TsForInStatement : TsNode
{
	/// <summary>
	/// The left-hand side of the <c>for ... in</c> loop.
	/// </summary>
	public TsNode Left { get; }

	/// <summary>
	/// The right-hand side of the <c>for ... in</c> loop.
	/// </summary>
	public TsExpression Right { get; }

	/// <summary>
	/// The statements in the body of the <c>for ... in</c> loop.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// Creates a new <c>for ... in</c> loop.
	/// </summary>
	/// <param name="left">The left-hand side of the <c>for ... in</c> loop.</param>
	/// <param name="right">The right-hand side of the <c>for ... in</c> loop.</param>
	public TsForInStatement(TsNode left, TsExpression right)
	{
		Left = left;
		Right = right;
	}

	/// <summary>
	/// Adds a statement to the body of the <c>for ... in</c> loop.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsForInStatement"/> instance for chaining.</returns>
	public TsForInStatement AddBody(params TsNode[] statements)
	{
		Body.AddRange(statements);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("for (");
		Left.Write(writer);
		writer.Write(" in ");
		Right.Write(writer);
		writer.WriteLine(") {");
		writer.Indent();

		foreach (TsNode statement in Body)
			statement.Write(writer);

		writer.Outdent();
		writer.WriteLine("}");
	}
}
