namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>continue</c> statement.
/// </summary>
public class TsContinueStatement : TsNode
{
	/// <summary>
	/// Creates a continue statement.
	/// </summary>
	public TsContinueStatement() { }

	public override void Write(TsCodeWriter writer) => writer.WriteLine("continue;");
}
