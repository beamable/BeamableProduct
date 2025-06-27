namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an empty statement (no-op).
/// </summary>
public class TsEmptyStatement : TsNode
{
	/// <summary>
	/// Add a no-op semicolon.
	/// </summary>
	public TsEmptyStatement() { }

	public override void Write(TsCodeWriter writer) => writer.WriteLine(";");
}
