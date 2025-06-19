namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript type alias.
/// </summary>
public class TsTypeAlias : TsNode
{
	/// <summary>
	/// The identifier of the type alias.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the type alias.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type parameters for the type alias.
	/// </summary>
	public List<TsGenericParameter> TypeParameters { get; } = new();

	/// <summary>
	/// The type of the type alias.
	/// </summary>
	public TsType Type { get; private set; }

	/// <summary>
	/// The modifiers for the type alias. Valid modifiers for type alias are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Declare"/>.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a typescript type alias.
	/// </summary>
	/// <param name="name">The name of the type alias.</param>
	public TsTypeAlias(string name)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the type alias.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for type alias are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Declare"/>.
	/// </param>
	/// <returns>The current <see cref="TsTypeAlias"/> instance for chaining.</returns>
	public TsTypeAlias AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Adds a type parameter to the type alias.
	/// </summary>
	/// <param name="param">The type parameter to add.</param>
	/// <returns>The current <see cref="TsTypeAlias"/> instance for chaining.</returns>
	public TsTypeAlias AddTypeParameter(TsGenericParameter param)
	{
		TypeParameters.Add(param);
		return this;
	}

	/// <summary>
	/// Sets the type of the type alias.
	/// </summary>
	/// <param name="type">The type of the type alias.</param>
	/// /// <returns>The current <see cref="TsTypeAlias"/> instance for chaining.</returns>
	public TsTypeAlias SetType(TsType type)
	{
		Type = type;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		if (Modifiers.HasFlag(TsModifier.Export))
			writer.Write($"{TsModifierExtensions.Export} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		writer.Write($"type {Name}");

		if (TypeParameters.Count > 0)
		{
			writer.Write("<");
			for (int i = 0; i < TypeParameters.Count; i++)
			{
				TypeParameters[i].Write(writer);
				if (i < TypeParameters.Count - 1)
					writer.Write(", ");
			}

			writer.Write(">");
		}

		writer.Write(" = ");
		Type.Write(writer);
		writer.WriteLine(";");
	}

	/// <summary>
	/// Ensures the chosen modifiers form a legal combination for a
	/// TypeScript <c>type</c> alias.
	/// Throws <see cref="InvalidOperationException"/> when invalid flags are detected.
	/// <exception cref="InvalidOperationException">Thrown when illegal flags are detected.</exception>
	/// </summary>
	private void ValidateModifiers()
	{
		// no flags outside Export | Declare
		TsModifier illegal = Modifiers & ~(TsModifier.Export | TsModifier.Declare);
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on type alias '{Name}': {illegal}");

		// 'export default' not allowed on aliases
		if (Modifiers.HasFlag(TsModifier.Default))
			throw new InvalidOperationException(
				"'export default' cannot be applied directly to a type alias (TS1316). " +
				"Export the alias first, then write 'export { Foo as default }'."
			);

		// Type must be set
		if (Type is null)
			throw new InvalidOperationException($"Type alias '{Name}' must have a target type set via SetType().");
	}
}
