namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a class in TypeScript.
/// </summary>
public class TsClass : TsNode
{
	/// <summary>
	/// The identifier of the class.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the class.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type parameters of the class.
	/// </summary>
	public List<TsGenericParameter> TypeParameters { get; } = new();

	/// <summary>
	/// The base class of the class.
	/// </summary>
	public TsExpression Extends { get; private set; }

	/// <summary>
	/// The interfaces that the class implements.
	/// </summary>
	public List<TsExpression> Implements { get; } = new();

	/// <summary>
	/// The properties of the class.
	/// </summary>
	public List<TsProperty> Properties { get; } = new();

	/// <summary>
	/// The constructor of the class.
	/// </summary>
	public TsConstructor Constructor { get; private set; }

	/// <summary>
	/// The methods of the class.
	/// </summary>
	public List<TsMethod> Methods { get; } = new();

	/// <summary>
	/// The modifiers for the class. Valid modifiers for classes are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Default"/>,
	/// <see cref="TsModifier.Declare"/> and <see cref="TsModifier.Abstract"/>.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a new class.
	/// </summary>
	/// <param name="name">The name of the class.</param>
	public TsClass(string name)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the class.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for classes are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Default"/>,
	/// <see cref="TsModifier.Declare"/> and <see cref="TsModifier.Abstract"/>.
	/// </param>
	/// <returns></returns>
	public TsClass AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Adds a type parameter to the class.
	/// </summary>
	/// <param name="param">The type parameter to add.</param>
	/// <returns>The current <see cref="TsClass"/> instance for chaining.</returns>
	public TsClass AddTypeParameter(TsGenericParameter param)
	{
		TypeParameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds an interface that the class implements.
	/// </summary>
	/// <param name="iface">The interface to add.</param>
	/// <returns>The current <see cref="TsClass"/> instance for chaining.</returns>
	public TsClass AddImplements(TsExpression iface)
	{
		Implements.Add(iface);
		return this;
	}

	/// <summary>
	/// Adds a property to the class.
	/// </summary>
	/// <param name="prop">The property to add.</param>
	/// <returns>The current <see cref="TsClass"/> instance for chaining.</returns>
	public TsClass AddProperty(TsProperty prop)
	{
		Properties.Add(prop);
		return this;
	}

	/// <summary>
	/// Adds a method to the class.
	/// </summary>
	/// <param name="method">The method to add.</param>
	/// <returns>The current <see cref="TsClass"/> instance for chaining.</returns>
	public TsClass AddMethod(TsMethod method)
	{
		Methods.Add(method);
		return this;
	}

	/// <summary>
	/// Sets the constructor of the class.
	/// </summary>
	/// <param name="method">The method to add.</param>
	/// <returns>The current <see cref="TsClass"/> instance for chaining.</returns>
	public TsClass SetConstructor(TsConstructor constructor)
	{
		Constructor = constructor;
		return this;
	}

	/// <summary>
	/// Sets the base class of the class.
	/// </summary>
	/// <param name="baseClass">The base class to set.</param>
	/// <returns>The current <see cref="TsClass"/> instance for chaining.</returns>
	public TsClass SetExtends(TsExpression baseClass)
	{
		Extends = baseClass;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		// export → default / declare → abstract
		if (Modifiers.HasFlag(TsModifier.Export))
			writer.Write($"{TsModifierExtensions.Export} ");

		if (Modifiers.HasFlag(TsModifier.Default))
			writer.Write($"{TsModifierExtensions.Default} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		if (Modifiers.HasFlag(TsModifier.Abstract))
			writer.Write($"{TsModifierExtensions.Abstract} ");

		writer.Write("class " + Name);

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

		if (Extends != null)
		{
			writer.Write(" extends ");
			Extends.Write(writer);
		}

		if (Implements.Count > 0)
			writer.Write(" implements " + string.Join(", ", Implements.Select(i => i.Render())));

		writer.WriteLine(" {");
		writer.Indent();

		foreach (TsProperty prop in Properties)
			prop.Write(writer);

		if (Properties.Count > 0 && (Methods.Count > 0 || Constructor != null))
			writer.WriteLine();

		Constructor?.Write(writer);

		if (Methods.Count > 0 && Constructor != null)
			writer.WriteLine();

		foreach (TsMethod method in Methods)
		{
			method.Write(writer);

			if (Methods.Count <= 1)
				continue;

			// Add a blank line after each method except the last one
			if (method != Methods.Last())
				writer.WriteLine();
		}

		writer.Outdent();
		writer.WriteLine("}");
	}

	/// <summary>
	/// Ensures that the selected modifier flags form a legal combination for a TypeScript <c>class</c>.
	/// Throws <see cref="InvalidOperationException"/> when invalid flags are detected.
	/// <exception cref="InvalidOperationException">Thrown when illegal flags are detected.</exception>
	/// </summary>
	private void ValidateModifiers()
	{
		// white-list only export / default / declare / abstract
		const TsModifier legal = TsModifier.Export | TsModifier.Default | TsModifier.Declare | TsModifier.Abstract;
		TsModifier illegal = Modifiers & ~legal;
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on class '{Name}': {illegal}");

		// default must also have export
		if (Modifiers.HasFlag(TsModifier.Default) && !Modifiers.HasFlag(TsModifier.Export))
			throw new InvalidOperationException("'default' can only be used together with 'export' on a class.");

		// default cannot be combined with declare
		if (Modifiers.HasFlag(TsModifier.Default) && Modifiers.HasFlag(TsModifier.Declare))
			throw new InvalidOperationException("'default' cannot be combined with 'declare' on a class.");
	}
}
