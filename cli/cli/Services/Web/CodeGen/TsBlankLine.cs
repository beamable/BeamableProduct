namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a blank line in TypeScript code.
/// </summary>
public class TsBlankLine : TsNode
{
	/// <summary>
	/// Add a blank line to the code.
	/// </summary>
	public TsBlankLine() { }

	public override void Write(TsCodeWriter writer) => writer.WriteLine();
}
