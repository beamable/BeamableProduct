namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript node.
/// This is the base class for all other TypeScript nodes in the code generation process.
/// </summary>
public abstract class TsNode
{
	protected bool EmitJavaScript;

	/// <summary>
	/// Writes the node to the given <see cref="TsCodeWriter"/>.
	/// </summary>
	/// <param name="writer">The writer used for building the source code.</param>
	public abstract void Write(TsCodeWriter writer);

	/// <summary>
	/// Renders the node to a string.
	/// </summary>
	/// <returns>The rendered string.</returns>
	public string Render()
	{
		var writer = new TsCodeWriter();
		Write(writer);
		return writer.ToString();
	}
}
