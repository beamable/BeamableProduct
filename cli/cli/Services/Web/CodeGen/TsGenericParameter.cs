namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript generic parameter.
/// </summary>
public class TsGenericParameter : TsNode
{
	/// <summary>
	/// The identifier of the generic parameter.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the generic parameter.
	/// Usually a single uppercase letter (e.g. <c>T</c>, <c>K</c>, <c>U</c>, <c>V</c> etc).
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The constraint of the generic parameter.
	/// </summary>
	public TsType Constraint { get; }

	/// <summary>
	/// Creates a new generic parameter.
	/// </summary>
	/// <param name="name">
	/// The name of the generic parameter.
	/// Usually a single uppercase letter (e.g. <c>T</c>, <c>K</c>, <c>U</c>, <c>V</c> etc).
	/// </param>
	/// <param name="constraint">The constraint of the generic parameter.</param>
	public TsGenericParameter(string name, TsType constraint = null)
	{
		Name = name;
		Constraint = constraint;
		Identifier = new TsIdentifier(name);
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write(Name);
		if (Constraint == null)
			return;

		writer.Write(" extends ");
		Constraint.Write(writer);
	}
}
