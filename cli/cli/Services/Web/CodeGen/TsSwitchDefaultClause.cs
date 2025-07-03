namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents the <c>default</c> clause in a switch statement.
/// </summary>
public class TsSwitchDefaultClause : TsNode
{
	/// <summary>
	/// The body of the <c>default</c> clause.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// Creates a new <c>default</c> clause.
	/// </summary>
	public TsSwitchDefaultClause() { }

	/// <summary>
	/// Creates a new <c>default</c> clause with the specified body.
	/// </summary>
	/// <param name="statements">The body of the <c>default</c> clause.</param>
	/// <returns>The current <see cref="TsSwitchDefaultClause"/> instance for chaining.</returns>
	public TsSwitchDefaultClause AddBody(params TsNode[] statements)
	{
		Body.AddRange(statements);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.WriteLine("default:");
		writer.Indent();

		foreach (TsNode statement in Body)
			statement.Write(writer);

		writer.Outdent();
	}
}
