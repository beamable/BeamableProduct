namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>return</c> statement.
/// </summary>
public class TsReturnStatement : TsNode
{
	/// <summary>
	/// The optional return expression.
	/// </summary>
	public TsExpression Expression { get; }

	/// <summary>
	/// Creates a return statement.
	/// </summary>
	public TsReturnStatement(TsExpression expression = null) => Expression = expression;

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("return");
		if (Expression != null)
		{
			writer.Write(" ");
			Expression.Write(writer);
		}

		writer.WriteLine(";");
	}
}
