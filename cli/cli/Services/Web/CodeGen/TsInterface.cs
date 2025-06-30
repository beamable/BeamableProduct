namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript interface.
/// </summary>
public class TsInterface : TsNode
{
	/// <summary>
	/// The identifier of the interface.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the interface.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type parameters for the interface.
	/// </summary>
	public List<TsGenericParameter> TypeParameters { get; } = new();

	/// <summary>
	/// The interfaces that this interface extends.
	/// </summary>
	public List<TsExpression> Extends { get; } = new();

	/// <summary>
	/// The properties of the interface.
	/// </summary>
	public List<TsProperty> Properties { get; } = new();

	/// <summary>
	/// The modifiers for the interface. Valid modifiers for interfaces are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Default"/>, and <see cref="TsModifier.Declare"/>.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a new interface.
	/// </summary>
	/// <param name="name"></param>
	public TsInterface(string name)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the interface.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for interfaces are:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Default"/>, and <see cref="TsModifier.Declare"/>.
	/// </param>
	/// <returns>The current <see cref="TsInterface"/> instance for chaining.</returns>
	public TsInterface AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Adds a type parameter to the interface.
	/// </summary>
	/// <param name="param">The type parameter to add.</param>
	/// <returns>The current <see cref="TsInterface"/> instance for chaining.</returns>
	public TsInterface AddTypeParameter(TsGenericParameter param)
	{
		TypeParameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds an interface to extend this interface.
	/// </summary>
	/// <param name="iface">The interface to extend.</param>
	/// <returns>The current <see cref="TsInterface"/> instance for chaining.</returns>
	public TsInterface AddExtends(TsExpression iface)
	{
		Extends.Add(iface);
		return this;
	}

	/// <summary>
	/// Adds a property to the interface.
	/// </summary>
	/// <param name="prop">The property to add.</param>
	/// <returns>The current <see cref="TsInterface"/> instance for chaining.</returns>
	public TsInterface AddProperty(TsProperty prop)
	{
		Properties.Add(prop);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		if (Modifiers.HasFlag(TsModifier.Export))
			writer.Write($"{TsModifierExtensions.Export} ");

		if (Modifiers.HasFlag(TsModifier.Default))
			writer.Write($"{TsModifierExtensions.Default} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		writer.Write("interface " + Name);

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

		if (Extends.Count > 0)
			writer.Write($" extends {string.Join(", ", Extends.Select(e => e.Render()))}");

		writer.WriteLine(" {");
		writer.Indent();

		foreach (TsProperty prop in Properties)
			prop.Write(writer);

		writer.Outdent();
		writer.WriteLine("}");
	}

	/// <summary>
	/// Ensures the chosen modifiers form a legal combination for a TypeScript <c>interface</c>.
	/// Throws <see cref="InvalidOperationException"/>when invalid flags are detected.
	/// <exception cref="InvalidOperationException">Thrown when illegal flags are detected.</exception>
	/// </summary>
	private void ValidateModifiers()
	{
		// whitelist: only export, declare, default
		TsModifier illegal = Modifiers & ~(TsModifier.Export | TsModifier.Declare | TsModifier.Default);
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on interface '{Name}': {illegal}");

		// default must be accompanied by export
		if (Modifiers.HasFlag(TsModifier.Default) && !Modifiers.HasFlag(TsModifier.Export))
			throw new InvalidOperationException("'default' can only be used together with 'export' on an interface.");

		// default cannot coexist with declare
		if (Modifiers.HasFlag(TsModifier.Default) && Modifiers.HasFlag(TsModifier.Declare))
			throw new InvalidOperationException("'default' cannot be combined with 'declare' on an interface.");
	}
}
