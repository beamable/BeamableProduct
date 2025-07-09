namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a property within an object literal expression.
/// </summary>
public class TsObjectLiteralMember : TsNode
{
	/// <summary>
	/// The key expression of the object literal member.
	/// </summary>
	public TsExpression Key { get; }

	/// <summary>
	/// The value expression of the object literal member.
	/// </summary>
	public TsExpression Value { get; }

	/// <summary>
	/// Creates a new object literal member.
	/// </summary>
	/// <param name="key">The key expression.</param>
	/// <param name="value">The value expression.</param>
	public TsObjectLiteralMember(TsExpression key, TsExpression value)
	{
		Key = key;
		Value = value;
	}

	public override void Write(TsCodeWriter writer)
	{
		if (Key is TsIdentifier && Value is TsIdentifier && Key.Render() == Value.Render())
		{
			writer.Write(Key.Render());
			return;
		}

		Key.Write(writer);
		writer.Write(": ");
		Value.Write(writer);
	}
}

/// <summary>
/// Represents a TypeScript object literal expression.
/// </summary>
public class TsObjectLiteralExpression : TsExpression
{
	/// <summary>
	/// The members of the object literal expression.
	/// </summary>
	public List<TsObjectLiteralMember> Members { get; } = new();

	/// <summary>
	/// Adds a member to the object literal expression.
	/// </summary>
	/// <param name="member">The member to add.</param>
	/// <returns>The current <see cref="TsObjectLiteralExpression"/> instance for chaining.</returns>
	public TsObjectLiteralExpression AddMember(TsObjectLiteralMember member)
	{
		Members.Add(member);
		return this;
	}

	/// <summary>
	/// Adds a member to the object literal expression with the specified key and value.
	/// </summary>
	/// <param name="key">The key expression.</param>
	/// <param name="value">The value expression.</param>
	/// <returns>The current <see cref="TsObjectLiteralExpression"/> instance for chaining.</returns>
	public TsObjectLiteralExpression AddMember(TsExpression key, TsExpression value)
	{
		Members.Add(new TsObjectLiteralMember(key, value));
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		if (Members.Count == 0)
		{
			writer.Write("{}");
			return;
		}

		writer.WriteLine("{");
		writer.Indent();

		for (int i = 0; i < Members.Count; i++)
		{
			Members[i].Write(writer);
			if (i < Members.Count - 1)
				writer.WriteLine(",");
			else
				writer.WriteLine();
		}

		writer.Outdent();
		writer.Write("}");
	}
}
