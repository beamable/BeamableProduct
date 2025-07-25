namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript parameter.
/// </summary>
public abstract class TsParameter : TsNode
{
	/// <summary>
	/// The identifier of the parameter.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the parameter.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The type annotation of the parameter.
	/// </summary>
	public TsType Type { get; }

	/// <summary>
	/// Whether the parameter is optional.
	/// </summary>
	public bool IsOptional { get; protected set; }

	/// <summary>
	/// The default value of the parameter.
	/// </summary>
	public TsExpression DefaultValue { get; protected set; }

	/// <summary>
	/// Creates a typescript parameter.
	/// </summary>
	/// <param name="name">The name of the parameter.</param>
	/// <param name="type">The type of the parameter.</param>
	/// <param name="isOptional">Whether the parameter is optional.</param>
	/// <param name="defaultValue">The default value of the parameter.</param>
	protected TsParameter(string name, TsType type, bool emitJavaScript = false)
	{
		Name = name;
		Type = type;
		Identifier = new TsIdentifier(name);
		EmitJavaScript = emitJavaScript;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateParameter();

		writer.Write(Name);

		if (IsOptional && !EmitJavaScript)
			writer.Write("?");

		if (!EmitJavaScript)
		{
			writer.Write(": ");
			Type.Write(writer);
		}

		if (DefaultValue != null)
		{
			writer.Write(" = ");
			DefaultValue.Write(writer);
		}
	}

	/// <summary>
	/// Shared validity checks for all parameters.
	/// Throws <see cref="InvalidOperationException"/> when a parameter is invalid.
	/// </summary>
	/// <exception cref="InvalidOperationException">Invalid parameter.</exception>
	private void ValidateParameter()
	{
		// TS1015: A parameter cannot use both '?' and a default value.
		if (IsOptional && DefaultValue != null)
			throw new InvalidOperationException($"Parameter '{Name}' cannot be optional and have a default value.");
	}
}

/// <summary>
/// Represents a TypeScript function parameter.
/// </summary>
public class TsFunctionParameter : TsParameter
{
	public TsFunctionParameter(string name, TsType type, bool emitJavaScript = false) : base(name, type, emitJavaScript)
	{
	}

	/// <summary>
	/// Makes the parameter optional.
	/// </summary>
	/// <returns>The current <see cref="TsFunctionParameter"/> instance for chaining.</returns>
	public TsFunctionParameter AsOptional()
	{
		IsOptional = true;
		return this;
	}

	/// <summary>
	/// Sets the default value of the parameter.
	/// </summary>
	/// <param name="defaultValue">The default value of the parameter.</param>
	/// <returns>The current <see cref="TsFunctionParameter"/> instance for chaining.</returns>
	public TsFunctionParameter WithDefaultValue(TsExpression defaultValue)
	{
		DefaultValue = defaultValue;
		return this;
	}
}

/// <summary>
/// Represents a TypeScript constructor parameter.
/// </summary>
public class TsConstructorParameter : TsParameter
{
	/// <summary>
	/// The modifiers of the parameter. Valid modifiers for constructor parameter are:
	/// <see cref="TsModifier.Public"/>, <see cref="TsModifier.Protected"/>,
	/// <see cref="TsModifier.Private"/>, <see cref="TsModifier.Readonly"/>.
	/// </summary>
	public TsModifier Modifiers { get; private set; }

	public TsConstructorParameter(string name, TsType type, bool emitJavaScript = false)
		: base(name, type, emitJavaScript)
	{
	}

	/// <summary>
	/// Adds a modifier to the parameter.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for constructor parameter are:
	/// <see cref="TsModifier.Public"/>, <see cref="TsModifier.Protected"/>,
	/// <see cref="TsModifier.Private"/>, <see cref="TsModifier.Readonly"/>.
	/// </param>
	/// <returns>The current <see cref="TsConstructorParameter"/> instance for chaining.</returns>
	public TsConstructorParameter AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Makes the parameter optional.
	/// </summary>
	/// <returns>The current <see cref="TsConstructorParameter"/> instance for chaining.</returns>
	public TsConstructorParameter AsOptional()
	{
		IsOptional = true;
		return this;
	}

	/// <summary>
	/// Sets the default value of the parameter.
	/// </summary>
	/// <param name="defaultValue">The default value of the parameter.</param>
	/// <returns>The current <see cref="TsConstructorParameter"/> instance for chaining.</returns>
	public TsConstructorParameter WithDefaultValue(TsExpression defaultValue)
	{
		DefaultValue = defaultValue;
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

		if (Modifiers.HasFlag(TsModifier.Readonly))
			writer.Write($"{TsModifierExtensions.Readonly} ");

		base.Write(writer);
	}

	/// <summary>
	/// Ensures the selected flags form a legal combination for a TypeScript constructor parameter.
	/// Throws <see cref="InvalidOperationException"/> when an invalid mix is detected.
	/// </summary>
	/// <exception cref="InvalidOperationException">Invalid combination of modifiers.</exception>
	private void ValidateModifiers()
	{
		// disallow any flag outside the allowed set
		TsModifier illegal = Modifiers &
		                     ~(TsModifier.Public | TsModifier.Protected | TsModifier.Private | TsModifier.Readonly);

		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on constructor parameter '{Name}': {illegal}");

		// at most one access modifier
		int access =
			(Modifiers.HasFlag(TsModifier.Public) ? 1 : 0) +
			(Modifiers.HasFlag(TsModifier.Protected) ? 1 : 0) +
			(Modifiers.HasFlag(TsModifier.Private) ? 1 : 0);

		if (access > 1)
			throw new InvalidOperationException(
				$"Parameter property '{Name}' cannot specify more than one access modifier.");
	}
}
