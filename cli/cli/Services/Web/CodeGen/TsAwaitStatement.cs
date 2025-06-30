namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an <c>await</c> statement.
/// </summary>
public class TsAwaitStatement : TsNode
{
	/// <summary>
	/// The expression to await.
	/// </summary>
	public TsExpression Expression { get; }

	/// <summary>
	/// Creates a new await statement.
	/// </summary>
	/// <param name="expression">The expression to await.</param>
	public TsAwaitStatement(TsExpression expression) => Expression = expression;

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("await ");
		Expression.Write(writer);
		writer.WriteLine(";");
	}
}
