namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an identifier.
/// </summary>
public class TsIdentifier : TsExpression
{
	/// <summary>
	/// The identifier.
	/// </summary>
	public string Identifier { get; }

	/// <summary>
	/// Create an instance for the given identifier.
	/// </summary>
	/// <param name="identifier">The identifier.</param>
	public TsIdentifier(string identifier) => Identifier = identifier;

	public override void Write(TsCodeWriter writer) => writer.Write(Identifier);
}
