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

	public bool IsReturnable { get; set; }

	/// <summary>
	/// Creates a new await statement.
	/// </summary>
	/// <param name="expression">The expression to await.</param>
	public TsAwaitStatement(TsExpression expression, bool isReturnable = false)
	{
		Expression = expression;
		IsReturnable = isReturnable;
	}

	public override void Write(TsCodeWriter writer)
	{
		if (IsReturnable)
			writer.Write("return ");

		writer.Write("await ");
		Expression.Write(writer);
		writer.WriteLine(";");
	}
}
