namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an <c>if</c> / <c>else if</c> / <c>else</c> statement.
/// </summary>
public class TsConditionalStatement : TsNode
{
	/// <summary>
	/// The condition to check.
	/// </summary>
	public TsExpression Condition { get; }

	/// <summary>
	/// The statements to execute if the condition is true.
	/// </summary>
	public List<TsNode> ThenStatements { get; } = new();

	/// <summary>
	/// The statements to execute if the condition is false.
	/// </summary>
	public List<(TsExpression Condition, List<TsNode> Body)> ElseIfClauses { get; } = new();

	/// <summary>
	/// The statements to execute if none of the conditions are true.
	/// </summary>
	public List<TsNode> ElseStatements { get; } = new();

	/// <summary>
	/// Creates a new conditional statement.
	/// </summary>
	/// <param name="condition">The condition to check.</param>
	public TsConditionalStatement(TsExpression condition) => Condition = condition;

	/// <summary>
	/// Adds a statement to the <c>if</c> block.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsConditionalStatement"/> instance for chaining.</returns>
	public TsConditionalStatement AddThen(params TsNode[] statements)
	{
		ThenStatements.AddRange(statements);
		return this;
	}

	/// <summary>
	/// Adds an <c>else if</c> clause to the conditional statement.
	/// </summary>
	/// <param name="condition">The condition to check.</param>
	/// <param name="statements">The statements to execute if the condition is true.</param>
	/// <returns>The current <see cref="TsConditionalStatement"/> instance for chaining.</returns>
	public TsConditionalStatement AddElseIf(TsExpression condition, params TsNode[] statements)
	{
		ElseIfClauses.Add((condition, new List<TsNode>(statements)));
		return this;
	}

	/// <summary>
	/// Adds a statement to the <c>else</c> block.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsConditionalStatement"/> instance for chaining.</returns>
	public TsConditionalStatement AddElse(params TsNode[] statements)
	{
		ElseStatements.AddRange(statements);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("if (");
		Condition.Write(writer);
		writer.WriteLine(") {");
		writer.Indent();

		foreach (TsNode statement in ThenStatements)
			statement.Write(writer);

		writer.Outdent();

		if (ElseIfClauses.Count > 0 || ElseStatements.Count > 0)
			writer.Write("} ");
		else
			writer.WriteLine("}");

		foreach ((TsExpression Condition, List<TsNode> Body) clause in ElseIfClauses)
		{
			writer.Write("else if (");
			clause.Condition.Write(writer);
			writer.WriteLine(") {");
			writer.Indent();

			foreach (TsNode statement in clause.Body)
				statement.Write(writer);

			writer.Outdent();

			bool isLastClause = clause == ElseIfClauses[^1];
			if (isLastClause && ElseStatements.Count == 0)
				writer.WriteLine("}");
			else
				writer.Write("} ");
		}

		if (ElseStatements.Count > 0)
		{
			writer.WriteLine("else {");
			writer.Indent();

			foreach (TsNode statement in ElseStatements)
				statement.Write(writer);

			writer.Outdent();
			writer.WriteLine("}");
		}
	}
}
