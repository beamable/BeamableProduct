namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript property.
/// </summary>
public class TsProperty : TsNode
{
	/// <summary>
	/// The identifier of the property.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the property.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type annotation of the property.
	/// </summary>
	public TsType Type { get; }

	/// <summary>
	/// Whether the property is optional.
	/// </summary>
	public bool IsOptional { get; private set; }

	/// <summary>
	/// The initializer expression of the property.
	/// </summary>
	public TsExpression Initializer { get; private set; }

	/// <summary>
	/// The modifiers of the property.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a typescript property.
	/// </summary>
	/// <param name="name">The name of the property.</param>
	/// <param name="type">The type of the property.</param>
	public TsProperty(string name, TsType type)
	{
		Name = name;
		Type = type;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the property.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for property are:
	/// <see cref="TsModifier.Public"/>, <see cref="TsModifier.Protected"/>,
	/// <see cref="TsModifier.Private"/>, <see cref="TsModifier.Static"/>,
	/// <see cref="TsModifier.Readonly"/>, <see cref="TsModifier.Declare"/>,
	/// <see cref="TsModifier.Override"/>, <see cref="TsModifier.Abstract"/>.
	/// </param>
	/// <returns>The current <see cref="TsProperty"/> instance for chaining.</returns>
	public TsProperty AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Makes the property optional.
	/// </summary>
	/// <returns>The current <see cref="TsProperty"/> instance for chaining.</returns>
	public TsProperty AsOptional()
	{
		IsOptional = true;
		return this;
	}

	/// <summary>
	/// Sets the initializer of the property.
	/// </summary>
	/// <param name="initializer">The initializer expression of the property.</param>
	/// <returns>The current <see cref="TsProperty"/> instance for chaining.</returns>
	public TsProperty WithInitializer(TsExpression initializer)
	{
		Initializer = initializer;
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		if (Modifiers.HasFlag(TsModifier.Public))
			writer.Write($"{TsModifierExtensions.Public} ");
		else if (Modifiers.HasFlag(TsModifier.Protected))
			writer.Write($"{TsModifierExtensions.Protected} ");
		else if (Modifiers.HasFlag(TsModifier.Private))
			writer.Write($"{TsModifierExtensions.Private} ");

		if (Modifiers.HasFlag(TsModifier.Static))
			writer.Write($"{TsModifierExtensions.Static} ");

		if (Modifiers.HasFlag(TsModifier.Readonly))
			writer.Write($"{TsModifierExtensions.Readonly} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		if (Modifiers.HasFlag(TsModifier.Override))
			writer.Write($"{TsModifierExtensions.Override} ");

		if (Modifiers.HasFlag(TsModifier.Abstract))
			writer.Write($"{TsModifierExtensions.Abstract} ");

		writer.Write(Name);

		if (IsOptional)
			writer.Write("?");

		writer.Write(": ");
		Type.Write(writer);

		if (Initializer != null && !Modifiers.HasFlag(TsModifier.Abstract))
		{
			writer.Write(" = ");
			Initializer.Write(writer);
		}

		writer.WriteLine(";");
	}

	/// <summary>
	/// Ensures that the chosen modifier flags form a legal combination
	/// for a TypeScript class property.
	/// Throws <see cref="InvalidOperationException"/> when an invalid mix is detected.
	/// </summary>
	/// <exception cref="InvalidOperationException">Invalid combination of modifiers.</exception>
	private void ValidateModifiers()
	{
		// access-modifier exclusivity
		int accessCount =
			(Modifiers.HasFlag(TsModifier.Public) ? 1 : 0) +
			(Modifiers.HasFlag(TsModifier.Protected) ? 1 : 0) +
			(Modifiers.HasFlag(TsModifier.Private) ? 1 : 0);
		if (accessCount > 1)
			throw new InvalidOperationException("A property can specify at most one access modifier.");

		// static conflicts
		if (Modifiers.HasFlag(TsModifier.Static) &&
		    (Modifiers.HasFlag(TsModifier.Abstract) ||
		     Modifiers.HasFlag(TsModifier.Override)))
			throw new InvalidOperationException("A property cannot be both static and abstract/override.");

		// abstract conflicts
		if (Modifiers.HasFlag(TsModifier.Abstract))
		{
			if (Modifiers.HasFlag(TsModifier.Declare))
				throw new InvalidOperationException("abstract and declare cannot be combined.");
			if (Initializer != null)
				throw new InvalidOperationException("Abstract properties cannot have an initializer.");
			if (Modifiers.HasFlag(TsModifier.Private))
				throw new InvalidOperationException("Abstract properties cannot be private.");
		}

		// declare conflicts
		if (Modifiers.HasFlag(TsModifier.Declare))
		{
			if (Modifiers.HasFlag(TsModifier.Abstract))
				throw new InvalidOperationException("declare cannot be combined with abstract.");

			if (Modifiers.HasFlag(TsModifier.Override))
				throw new InvalidOperationException("declare cannot be combined with override (see TS issue #51515).");

			if (Initializer != null)
				throw new InvalidOperationException("Declared properties may not have an initializer.");
		}
	}
}
