namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript variable.
/// </summary>
public class TsVariable : TsNode
{
	/// <summary>
	/// The identifier of the variable.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the variable.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type annotation of the variable.
	/// </summary>
	public TsType Type { get; private set; }

	/// <summary>
	/// If true, generates a <c>const</c>; otherwise, a <c>let</c>.
	/// </summary>
	public bool IsConst { get; private set; }

	/// <summary>
	/// Whether the variable declaration is an expression or a statement.
	/// If is true, the variable declaration is an expression (e.g. <c>let x = 1</c>).
	/// If is false, the variable declaration is a statement (e.g. <c>let x = 1;</c>).
	/// </summary>
	public bool IsExpression { get; private set; }

	/// <summary>
	/// The initializer expression of the variable.
	/// </summary>
	public TsExpression Initializer { get; private set; }

	/// <summary>
	/// The modifiers for the variable declaration. Currently only <see cref="TsModifier.Export"/> is supported.
	/// </summary>
	public TsModifier Modifiers { get; private set; }

	/// <summary>
	/// Creates a typescript variable.
	/// </summary>
	/// <param name="name">The name of the variable.</param>
	public TsVariable(string name)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the variable declaration.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for variable declaration are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Declare"/>.
	/// </param>
	/// <returns>The current <see cref="TsVariable"/> instance for chaining.</returns>
	public TsVariable AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Sets the type annotation of the variable.
	/// </summary>
	/// <param name="type">The type to set.</param>
	/// <returns>The current <see cref="TsVariable"/> instance for chaining.</returns>
	public TsVariable AsType(TsType type)
	{
		Type = type;
		return this;
	}

	/// <summary>
	/// Sets the variable to be const.
	/// </summary>
	/// <returns>The current <see cref="TsVariable"/> instance for chaining.</returns>
	public TsVariable AsConst()
	{
		IsConst = true;
		return this;
	}

	/// <summary>
	/// Sets the variable to be an expression.
	/// </summary>
	/// <returns>The current <see cref="TsVariable"/> instance for chaining.</returns>
	public TsVariable AsExpression()
	{
		IsExpression = true;
		return this;
	}

	/// <summary>
	/// Sets the initializer of the variable declaration.
	/// </summary>
	/// <param name="initializer">The initializer expression of the variable.</param>
	/// <returns>The current <see cref="TsVariable"/> instance for chaining.</returns>
	public TsVariable WithInitializer(TsExpression initializer)
	{
		Initializer = initializer;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		if (Modifiers.HasFlag(TsModifier.Export))
			writer.Write($"{TsModifierExtensions.Export} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		writer.Write(IsConst ? "const " : "let ");
		writer.Write(Name);

		if (Type != null)
		{
			writer.Write(": ");
			Type.Write(writer);
		}

		if (Initializer != null)
		{
			writer.Write(" = ");
			Initializer.Write(writer);
		}

		if (!IsExpression)
			writer.WriteLine(";");
	}

	/// <summary>
	/// Ensures the selected flags and initializer form a legal
	/// TypeScript variable declaration.
	/// Throws <see cref="InvalidOperationException"/> on error.
	/// </summary>
	private void ValidateModifiers()
	{
		// only export / declare allowed here
		TsModifier illegal = Modifiers & ~(TsModifier.Export | TsModifier.Declare);
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on variable: {illegal}");

		// default export of a variable statement is not legal TypeScript
		if (Modifiers.HasFlag(TsModifier.Default))
			throw new InvalidOperationException(
				"'export default' cannot be applied directly to a variable declaration. " +
				"Declare the variable first, then write 'export default foo;'."
			);

		// declare must NOT have an initializer
		if (Modifiers.HasFlag(TsModifier.Declare) && Initializer != null)
			throw new InvalidOperationException("Declared variables may not have an initializer (TS1039).");

		// const without initializer requires declare
		if (IsConst && Initializer == null && !Modifiers.HasFlag(TsModifier.Declare))
			throw new InvalidOperationException(
				"A runtime 'const' variable must have an initializer. " +
				"Use 'declare const' for ambient declarations without a value."
			);
	}
}
