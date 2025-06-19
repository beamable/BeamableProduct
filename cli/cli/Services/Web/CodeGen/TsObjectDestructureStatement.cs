namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an object destructuring statement in TypeScript, e.g., const { a, b, ...rest } = expr;
/// </summary>
public class TsObjectDestructureStatement : TsNode
{
	/// <summary>
	/// Whether to use const (true) or let (false) for the declaration.
	/// </summary>
	public bool IsConst { get; }

	/// <summary>
	/// The property names to destructure.
	/// </summary>
	public string[] Identifiers { get; }

	/// <summary>
	/// The optional rest identifier (e.g., ...rest).
	/// </summary>
	public string RestIdentifier { get; private set; }

	/// <summary>
	/// The initializer expression on the right-hand side.
	/// </summary>
	public TsExpression Initializer { get; }

	/// <summary>
	/// Creates a new object destructuring statement.
	/// </summary>
	/// <param name="identifiers">The property names to extract.</param>
	/// <param name="initializer">The expression to destructure.</param>
	/// <param name="isConst">True to use const; false to use let.</param>
	public TsObjectDestructureStatement(string[] identifiers, TsExpression initializer, bool isConst = true)
	{
		Identifiers = identifiers;
		Initializer = initializer;
		IsConst = isConst;
	}

	/// <summary>
	/// Sets the rest identifier for ...rest.
	/// </summary>
	public TsObjectDestructureStatement WithRest(string restIdentifier)
	{
		RestIdentifier = restIdentifier;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write(IsConst ? "const " : "let ");
		writer.WriteLine("{");
		writer.Indent();

		for (int i = 0; i < Identifiers.Length; i++)
		{
			writer.Write(Identifiers[i]);
			if (i < Identifiers.Length - 1)
				writer.WriteLine(",");
			else if (i == Identifiers.Length - 1 && RestIdentifier == null)
				writer.WriteLine();
		}

		if (RestIdentifier != null)
		{
			if (Identifiers.Length > 0)
				writer.WriteLine(",");

			writer.Write("...");
			writer.WriteLine(RestIdentifier);
		}

		writer.Outdent();
		writer.Write("} = ");
		Initializer.Write(writer);
		writer.WriteLine(";");
	}
}
