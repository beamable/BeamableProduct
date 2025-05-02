namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a function or method invocation.
/// </summary>
public class TsInvokeExpression : TsExpression
{
	/// <summary>
	/// The function or method to invoke.
	/// </summary>
	public TsExpression Callee { get; }

	/// <summary>
	/// The arguments to pass to the function or method.
	/// </summary>
	public List<TsExpression> Arguments { get; } = new();

	/// <summary>
	/// Invokes a function or method.
	/// </summary>
	/// <param name="callee">The function or method to invoke.</param>
	/// <param name="arguments">The arguments to pass to the function or method.</param>
	public TsInvokeExpression(TsExpression callee, params TsExpression[] arguments)
	{
		Callee = callee;
		Arguments.AddRange(arguments);
	}

	public override void Write(TsCodeWriter writer)
	{
		Callee.Write(writer);
		writer.Write("(");

		for (int i = 0; i < Arguments.Count; i++)
		{
			Arguments[i].Write(writer);
			if (i < Arguments.Count - 1)
				writer.Write(", ");
		}

		writer.Write(")");
	}
}
