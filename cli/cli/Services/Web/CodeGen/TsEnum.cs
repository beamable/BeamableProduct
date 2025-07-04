namespace cli.Services.Web.CodeGen;

public enum TsEnumMemberValueType
{
	String,
	Number
}

/// <summary>
/// Represents a TypeScript enum member.
/// </summary>
public class TsEnumMember : TsNode
{
	/// <summary>
	/// The identifier of the enum member.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the enum member.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The value of the enum member.
	/// </summary>
	public string Value { get; }

	/// <summary>
	/// The type of the enum member value.
	/// </summary>
	public TsEnumMemberValueType ValueType { get; }

	/// <summary>
	/// Creates a new enum member.
	/// </summary>
	/// <param name="name">The name of the enum member.</param>
	/// <param name="value">The value of the enum member.</param>
	/// <param name="valueType">The value type of the enum member.</param>
	public TsEnumMember(string name, string value = null,
		TsEnumMemberValueType valueType = TsEnumMemberValueType.String)
	{
		Name = name;
		Value = value;
		ValueType = valueType;
		Identifier = new TsIdentifier(name);
	}

	public override void Write(TsCodeWriter writer)
	{
		switch (ValueType)
		{
			case TsEnumMemberValueType.Number:
				writer.Write(Value != null ? $"{Name} = {Value}" : $"{Name}");
				break;
			case TsEnumMemberValueType.String:
			default:
				writer.Write(Value != null ? $"{Name} = \"{Value}\"" : $"{Name}");
				break;
		}
	}
}

/// <summary>
/// Represents a TypeScript enum.
/// </summary>
public class TsEnum : TsNode
{
	/// <summary>
	/// The identifier of the enum.
	/// </summary>
	public TsExpression Identifier { get; private set; }

	/// <summary>
	/// The name of the enum.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Whether the enum is const.
	/// </summary>
	public bool IsConst { get; private set; }

	/// <summary>
	/// The enum members.
	/// </summary>
	public List<TsEnumMember> Members { get; } = new();

	/// <summary>
	/// The modifiers for the enum.
	/// </summary>
	public TsModifier Modifiers { get; private set; } = TsModifier.None;

	/// <summary>
	/// Creates a new enum.
	/// </summary>
	/// <param name="name">The name of the enum.</param>
	public TsEnum(string name)
	{
		Name = name;
		Identifier = new TsIdentifier(name);
	}

	/// <summary>
	/// Adds a modifier to the enum.
	/// </summary>
	/// <param name="modifier">
	/// The modifier to add. Valid modifiers for enums include:
	/// <see cref="TsModifier.Export"/>, <see cref="TsModifier.Default"/>, and <see cref="TsModifier.Declare"/>.
	/// </param>
	/// <returns>The current <see cref="TsEnum"/> instance for chaining.</returns>
	public TsEnum AddModifier(TsModifier modifier)
	{
		Modifiers |= modifier;
		return this;
	}

	/// <summary>
	/// Sets the enum to be const.
	/// </summary>
	/// <returns>The current <see cref="TsEnum"/> instance for chaining.</returns>
	public TsEnum AsConst()
	{
		IsConst = true;
		return this;
	}

	/// <summary>
	/// Adds a new enum member to the enum.
	/// </summary>
	/// <param name="member">The enum member to add.</param>
	/// <returns>The current <see cref="TsEnum"/> instance for chaining.</returns>
	public TsEnum AddMember(TsEnumMember member)
	{
		Members.Add(member);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateModifiers();

		// modifier order: export → default → declare → const
		if (Modifiers.HasFlag(TsModifier.Export))
			writer.Write($"{TsModifierExtensions.Export} ");

		if (Modifiers.HasFlag(TsModifier.Default))
			writer.Write($"{TsModifierExtensions.Default} ");

		if (Modifiers.HasFlag(TsModifier.Declare))
			writer.Write($"{TsModifierExtensions.Declare} ");

		if (IsConst)
			writer.Write("const ");

		writer.WriteLine($"enum {Name} {{");
		writer.Indent();

		foreach (TsEnumMember member in Members)
		{
			if (member != Members.Last())
			{
				member.Write(writer);
				writer.WriteLine(",");
			}
			else
			{
				member.Write(writer);
				writer.WriteLine();
			}
		}

		writer.Outdent();
		writer.WriteLine("}");
	}

	/// <summary>
	/// Ensures that the chosen modifiers form a legal combination for a
	/// TypeScript <c>enum</c>.
	/// Throws <see cref="InvalidOperationException"/> when an invalid mix is detected.
	/// <exception cref="InvalidOperationException">Thrown when illegal flags are detected.</exception>
	/// </summary>
	private void ValidateModifiers()
	{
		// default must also have export
		if (Modifiers.HasFlag(TsModifier.Default) && !Modifiers.HasFlag(TsModifier.Export))
			throw new InvalidOperationException("'default' can only be used together with 'export' on an enum.");

		// default and (declare / const) are mutually exclusive
		if (Modifiers.HasFlag(TsModifier.Default) && (Modifiers.HasFlag(TsModifier.Declare) || IsConst))
			throw new InvalidOperationException("'default' cannot be combined with 'declare' or a const-enum.");

		// check for disallowed flags (anything outside Export|Default|Declare)
		TsModifier illegal = Modifiers & ~(TsModifier.Export | TsModifier.Default | TsModifier.Declare);
		if (illegal != TsModifier.None)
			throw new InvalidOperationException($"Illegal modifier(s) on enum: {illegal}.");
	}
}
