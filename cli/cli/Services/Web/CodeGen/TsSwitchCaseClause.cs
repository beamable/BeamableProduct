namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>case</c> clause in a switch statement.
/// </summary>
public class TsSwitchCaseClause : TsNode
{
	/// <summary>
	/// The expression to check.
	/// </summary>
	public TsExpression CaseExpression { get; }

	/// <summary>
	/// The statements to execute if the expression matches.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// Creates a new <c>case</c> clause.
	/// </summary>
	/// <param name="caseExpression">The expression to check.</param>
	public TsSwitchCaseClause(TsExpression caseExpression) => CaseExpression = caseExpression;

	/// <summary>
	/// Adds a statement to the <c>case</c> block.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsSwitchCaseClause"/> instance for chaining.</returns>
	public TsSwitchCaseClause AddBody(params TsNode[] statements)
	{
		Body.AddRange(statements);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("case ");
		CaseExpression.Write(writer);
		writer.WriteLine(":");
		writer.Indent();

		foreach (TsNode statement in Body)
			statement.Write(writer);

		writer.Outdent();
	}
}
