namespace cli.Services.Web.CodeGen;

/// <summary>
/// A statement consisting of a single expression.
/// </summary>
public class TsExpressionStatement : TsNode
{
	/// <summary>
	/// The expression to emit.
	/// </summary>
	public TsExpression Expression { get; }

	/// <summary>
	/// Creates a new expression statement.
	/// </summary>
	public TsExpressionStatement(TsExpression expression) => Expression = expression;

	public override void Write(TsCodeWriter writer)
	{
		Expression.Write(writer);
		writer.WriteLine(";");
	}
}
