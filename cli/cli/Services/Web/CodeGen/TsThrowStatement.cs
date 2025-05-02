namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>throw</c> statement.
/// </summary>
public class TsThrowStatement : TsNode
{
	/// <summary>
	/// The expression to throw.
	/// </summary>
	public TsExpression Expression { get; }

	/// <summary>
	/// Creates a throw statement.
	/// </summary>
	public TsThrowStatement(TsExpression expression) => Expression = expression;

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("throw ");
		Expression.Write(writer);
		writer.WriteLine(";");
	}
}
