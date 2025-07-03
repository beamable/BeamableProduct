namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a member access (e.g. obj.prop).
/// </summary>
public class TsMemberAccessExpression : TsExpression
{
	// Whether to use dot notation (obj.prop) or bracket notation (obj['prop'])
	private readonly bool _useDotNotation;

	/// <summary>
	/// The object to access.
	/// </summary>
	public TsExpression Object { get; }

	/// <summary>
	/// The member of the object being accessed.
	/// </summary>
	public string Member { get; }

	/// <summary>
	/// Creates a new member access expression.
	/// </summary>
	/// <param name="obj">The object to access.</param>
	/// <param name="member">The member of the object being accessed.</param>
	/// <param name="useDotNotation">Whether to use dot notation (obj.prop) or bracket notation (obj['prop']).</param>
	public TsMemberAccessExpression(TsExpression obj, string member, bool useDotNotation = true)
	{
		Object = obj;
		Member = member;
		_useDotNotation = useDotNotation;
	}

	public override void Write(TsCodeWriter writer)
	{
		Object.Write(writer);
		writer.Write(_useDotNotation ? $".{Member}" : $"['{Member}']");
	}
}
