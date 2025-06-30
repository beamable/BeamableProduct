namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>switch</c> statement.
/// </summary>
public class TsSwitchStatement : TsNode
{
	/// <summary>
	/// The expression to switch on.
	/// </summary>
	public TsExpression Expression { get; }

	/// <summary>
	/// The <c>case</c> clauses.
	/// </summary>
	public List<TsSwitchCaseClause> CaseClauses { get; } = new();

	/// <summary>
	/// The <c>default</c> clause.
	/// </summary>
	public TsSwitchDefaultClause DefaultClause { get; private set; } = new();

	/// <summary>
	/// Creates a new <c>switch</c> statement.
	/// </summary>
	/// <param name="expression">The expression to switch on.</param>
	public TsSwitchStatement(TsExpression expression) => Expression = expression;

	/// <summary>
	/// Adds a <c>case</c> clause to the switch statement.
	/// </summary>
	/// <param name="caseClause">The <c>case</c> clause to add.</param>
	/// <returns>The current <see cref="TsSwitchStatement"/> instance for chaining.</returns>
	public TsSwitchStatement AddCaseClause(TsSwitchCaseClause caseClause)
	{
		CaseClauses.Add(caseClause);
		return this;
	}

	/// <summary>
	/// Adds a <c>default</c> clause to the switch statement.
	/// </summary>
	/// <param name="defaultClause">The <c>default</c> clause to add.</param>
	/// <returns>The current <see cref="TsSwitchStatement"/> instance for chaining.</returns>
	public TsSwitchStatement AddDefaultClause(TsSwitchDefaultClause defaultClause)
	{
		DefaultClause = defaultClause;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("switch (");
		Expression.Write(writer);
		writer.WriteLine(") {");
		writer.Indent();

		foreach (TsSwitchCaseClause clause in CaseClauses)
			clause.Write(writer);

		DefaultClause?.Write(writer);

		writer.Outdent();
		writer.WriteLine("}");
	}
}
