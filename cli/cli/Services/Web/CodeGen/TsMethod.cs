namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a method in TypeScript.
/// </summary>
public class TsMethod : TsNode
{
	/// <summary>
	/// The identifier of the method.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the method.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type parameters of the method.
	/// </summary>
	public List<TsGenericParameter> TypeParameters { get; } = new();

	/// <summary>
	/// The parameters of the method.
	/// </summary>
	public List<TsParameter> Parameters { get; } = new();

	/// <summary>
	/// The return type of the method.
	/// </summary>
	public TsType ReturnType { get; private set; } = TsType.Void;

	/// <summary>
	/// The implementation body as a sequence of nodes.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// The comments preceding the method.
	/// </summary>
	public List<TsComment> Comments { get; } = new();

	/// <summary>
	/// The modifiers of the method. Valid flags are: <see cref="TsModifier.Export"/>,
	/// <see cref="TsModifier.Default"/>, <see cref="TsModifier.Async"/>,
	/// <see cref="TsModifier.Override"/>, <see cref="TsModifier.Abstract"/>,
	/// <see cref="TsModifier.Static"/>.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a new method.
	/// </summary>
	/// <param name="name">The name of the method.</param>
	public TsMethod(string name, bool emitJavaScript = false)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
		EmitJavaScript = emitJavaScript;
	}

	/// <summary>
	/// Adds a comment before the method.
	/// </summary>
	/// <param name="comment">
	/// The comment to add.
	/// </param>
	/// <returns>The current <see cref="TsMethod"/> instance for chaining.</returns>
	public TsMethod AddComment(TsComment comment)
	{
		Comments.Add(comment);
		return this;
	}

	/// <summary>
	/// Adds a modifier to the method.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid flags are: <see cref="TsModifier.Export"/>,
	/// <see cref="TsModifier.Default"/>, <see cref="TsModifier.Async"/>,
	/// <see cref="TsModifier.Override"/>, <see cref="TsModifier.Abstract"/>,
	/// <see cref="TsModifier.Static"/>.
	/// </param>
	/// <returns>The current <see cref="TsMethod"/> instance for chaining.</returns>
	public TsMethod AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Adds a type parameter to the method.
	/// </summary>
	/// <param name="param">The type parameter to add.</param>
	/// <returns>The current <see cref="TsMethod"/> instance for chaining.</returns>
	public TsMethod AddTypeParameter(TsGenericParameter param)
	{
		TypeParameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds a parameter to the method.
	/// </summary>
	/// <param name="param">The parameter to add.</param>
	/// <returns>The current <see cref="TsMethod"/> instance for chaining.</returns>
	public TsMethod AddParameter(TsParameter param)
	{
		Parameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds statements to the implementation body of the method.
	/// </summary>
	/// <param name="nodes">The nodes making up the method body.</param>
	/// <returns>The current <see cref="TsMethod"/> instance for chaining.</returns>
	public TsMethod AddBody(params TsNode[] nodes)
	{
		Body.AddRange(nodes);
		return this;
	}

	/// <summary>
	/// Sets the return type of the method.
	/// </summary>
	/// <param name="type">The return type of the method.</param>
	/// <returns>The current <see cref="TsMethod"/> instance for chaining.</returns>
	public TsMethod SetReturnType(TsType type)
	{
		ReturnType = type;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		foreach (TsComment comment in Comments)
			comment.Write(writer);

		if (Modifiers.HasFlag(TsModifier.Public))
			writer.Write($"{TsModifierExtensions.Public} ");
		else if (Modifiers.HasFlag(TsModifier.Protected))
			writer.Write($"{TsModifierExtensions.Protected} ");
		else if (Modifiers.HasFlag(TsModifier.Private))
			writer.Write($"{TsModifierExtensions.Private} ");

		// static → async → override → abstract
		if (Modifiers.HasFlag(TsModifier.Static))
			writer.Write($"{TsModifierExtensions.Static} ");

		if (Modifiers.HasFlag(TsModifier.Async))
			writer.Write($"{TsModifierExtensions.Async} ");

		if (Modifiers.HasFlag(TsModifier.Override))
			writer.Write($"{TsModifierExtensions.Override} ");

		if (Modifiers.HasFlag(TsModifier.Abstract))
			writer.Write($"{TsModifierExtensions.Abstract} ");

		writer.Write(Name);

		if (TypeParameters.Count > 0 && !EmitJavaScript)
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

		if (!EmitJavaScript)
		{
			writer.Write(": ");
			ReturnType.Write(writer);
		}

		if (Modifiers.HasFlag(TsModifier.Abstract))
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
	/// Ensures that the chosen modifier flags form a legal combination for a TypeScript method.
	/// Throws <see cref="InvalidOperationException"/> when an invalid mix is detected.
	/// </summary>
	/// <exception cref="InvalidOperationException">Invalid combination of modifiers.</exception>
	private void ValidateModifiers()
	{
		// only permitted flags
		const TsModifier legal = TsModifier.Public | TsModifier.Protected | TsModifier.Private |
		                         TsModifier.Static | TsModifier.Async |
		                         TsModifier.Override | TsModifier.Abstract;
		TsModifier illegal = Modifiers & ~legal;
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on method '{Name}': {illegal}");

		// single access modifier
		int accessCount =
			(Modifiers.HasFlag(TsModifier.Public) ? 1 : 0) +
			(Modifiers.HasFlag(TsModifier.Protected) ? 1 : 0) +
			(Modifiers.HasFlag(TsModifier.Private) ? 1 : 0);
		if (accessCount > 1)
			throw new InvalidOperationException($"Method '{Name}' cannot specify more than one access modifier.");

		// static conflicts
		if (Modifiers.HasFlag(TsModifier.Static) &&
		    (Modifiers.HasFlag(TsModifier.Abstract) ||
		     Modifiers.HasFlag(TsModifier.Override)))
			throw new InvalidOperationException("A property cannot be both static and abstract/override.");

		// abstract methods must not have a body
		if (Modifiers.HasFlag(TsModifier.Abstract) && Body.Count > 0)
			throw new InvalidOperationException("Abstract methods must not have an implementation body.");
	}
}
