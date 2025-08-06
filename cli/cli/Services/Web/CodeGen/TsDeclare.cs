namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a declare statement in TypeScript.
/// </summary>
public class TsDeclare : TsNode
{
	/// <summary>
	/// The statement to declare.
	/// </summary>
	public TsNode Statement { get; }

	/// <summary>
	/// Creates a new declare statement.
	/// </summary>
	/// <param name="statement">The statement to declare.</param>
	public TsDeclare(TsNode statement)
	{
		Statement = statement;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("declare ");
		Statement.Write(writer);
	}
}
