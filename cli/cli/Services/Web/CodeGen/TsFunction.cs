namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>function</c> in TypeScript.
/// </summary>
public class TsFunction : TsNode
{
	/// <summary>
	/// The identifier of the function.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the function.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type parameters of the function.
	/// </summary>
	public List<TsGenericParameter> TypeParameters { get; } = new();

	/// <summary>
	/// The parameters of the function.
	/// </summary>
	public List<TsParameter> Parameters { get; } = new();

	/// <summary>
	/// The return type of the function.
	/// </summary>
	public TsType ReturnType { get; private set; } = TsType.Void;

	/// <summary>
	/// The implementation body as a sequence of nodes.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// The modifiers of the function. Valid flags are: <see cref="TsModifier.Export"/>,
	/// <see cref="TsModifier.Default"/>, <see cref="TsModifier.Declare"/>,
	/// <see cref="TsModifier.Async"/>.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a new function.
	/// </summary>
	/// <param name="name">The name of the function.</param>
	public TsFunction(string name)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the function.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid flags are: <see cref="TsModifier.Export"/>,
	/// <see cref="TsModifier.Default"/>, <see cref="TsModifier.Declare"/>,
	/// <see cref="TsModifier.Async"/>.
	/// </param>
	/// <returns>The current <see cref="TsFunction"/> instance for chaining.</returns>
	public TsFunction AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Adds a type parameter to the function.
	/// </summary>
	/// <param name="param">The type parameter to add.</param>
	/// <returns>The current <see cref="TsFunction"/> instance for chaining.</returns>
	public TsFunction AddTypeParameter(TsGenericParameter param)
	{
		TypeParameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds a parameter to the function.
	/// </summary>
	/// <param name="param">The parameter to add.</param>
	/// <returns>The current <see cref="TsFunction"/> instance for chaining.</returns>
	public TsFunction AddParameter(TsParameter param)
	{
		Parameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds statements to the implementation body of the function.
	/// </summary>
	/// <param name="nodes">The nodes making up the function body.</param>
	/// <returns>The current <see cref="TsFunction"/> instance for chaining.</returns>
	public TsFunction AddBody(params TsNode[] nodes)
	{
		Body.AddRange(nodes);
		return this;
	}

	/// <summary>
	/// Sets the return type of the function.
	/// </summary>
	/// <param name="type">The return type of the function.</param>
	/// <returns>The current <see cref="TsFunction"/> instance for chaining.</returns>
	public TsFunction SetReturnType(TsType type)
	{
		ReturnType = type;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		// export → default / declare → async
		if (Modifiers.HasFlag(TsModifier.Export))
			writer.Write($"{TsModifierExtensions.Export} ");

		if (Modifiers.HasFlag(TsModifier.Default))
			writer.Write($"{TsModifierExtensions.Default} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		if (Modifiers.HasFlag(TsModifier.Async))
			writer.Write($"{TsModifierExtensions.Async} ");

		writer.Write($"function {Name}");

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

		writer.Write("(");

		for (int i = 0; i < Parameters.Count; i++)
		{
			Parameters[i].Write(writer);
			if (i < Parameters.Count - 1)
				writer.Write(", ");
		}

		writer.Write(")");
		writer.Write(": ");
		ReturnType.Write(writer);

		if (Modifiers.HasFlag(TsModifier.Declare))
		{
			writer.WriteLine(";");
			return;
		}

		writer.WriteLine(" {");
		writer.Indent();

		foreach (TsNode statement in Body)
			statement.Write(writer);

		writer.Outdent();
		writer.WriteLine("}");
	}

	/// <summary>
	/// Ensures that the chosen modifier flags form a legal combination for a TypeScript <c>function</c>.
	/// Throws <see cref="InvalidOperationException"/> when an invalid mix is detected.
	/// </summary>
	/// <exception cref="InvalidOperationException">Invalid combination of modifiers.</exception>
	private void ValidateModifiers()
	{
		// only export | default | declare | async allowed
		const TsModifier legal = TsModifier.Export | TsModifier.Default | TsModifier.Declare | TsModifier.Async;
		TsModifier illegal = Modifiers & ~legal;
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on function '{Name}': {illegal}");

		// default requires export
		if (Modifiers.HasFlag(TsModifier.Default) && !Modifiers.HasFlag(TsModifier.Export))
			throw new InvalidOperationException("'default' can only be used with 'export' on a function.");

		// default cannot coexist with declare
		if (Modifiers.HasFlag(TsModifier.Default) && Modifiers.HasFlag(TsModifier.Declare))
			throw new InvalidOperationException("'default' cannot be combined with 'declare'.");

		// declare must not have a body
		if (Modifiers.HasFlag(TsModifier.Declare) && Body.Count > 0)
			throw new InvalidOperationException("Declared functions must not have an implementation body.");
	}
}
