namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an arrow function expression, e.g. (<paramref name="parameters"/>) => <paramref name="body"/>.
/// </summary>
public class TsArrowExpression : TsExpression
{
	/// <summary>
	/// The parameters of the arrow function.
	/// </summary>
	public List<TsParameter> Parameters { get; } = new();

	/// <summary>
	/// The body expression of the arrow function.
	/// </summary>
	public TsExpression Body { get; }

	/// <summary>
	/// Creates an arrow function with a single parameter.
	/// </summary>
	public TsArrowExpression(TsParameter parameter, TsExpression body)
	{
		Parameters.Add(parameter);
		Body = body;
	}

	/// <summary>
	/// Creates an arrow function with multiple parameters.
	/// </summary>
	public TsArrowExpression(IEnumerable<TsParameter> parameters, TsExpression body)
	{
		Parameters.AddRange(parameters);
		Body = body;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("(");
		for (int i = 0; i < Parameters.Count; i++)
		{
			Parameters[i].Write(writer);
			if (i < Parameters.Count - 1)
				writer.Write(", ");
		}

		writer.Write(") => ");
		Body.Write(writer);
	}
}
