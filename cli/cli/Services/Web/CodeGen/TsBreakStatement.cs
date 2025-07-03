namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>break</c> statement.
/// </summary>
public class TsBreakStatement : TsNode
{
	/// <summary>
	/// Creates a break statement.
	/// </summary>
	public TsBreakStatement() { }

	public override void Write(TsCodeWriter writer) => writer.WriteLine("break;");
}
