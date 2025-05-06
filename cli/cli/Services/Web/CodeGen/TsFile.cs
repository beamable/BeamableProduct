namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript file.
/// </summary>
public class TsFile : TsNode
{
	/// <summary>
	/// The name of the TypeScript file.
	/// </summary>
	public string FileName { get; private set; }

	/// <summary>
	/// The imports of the file.
	/// </summary>
	private List<TsImport> Imports { get; } = new();

	/// <summary>
	/// The declarations of the file.
	/// </summary>
	private List<TsNode> Declarations { get; } = new();

	/// <summary>
	/// Creates a typescript file.
	/// </summary>
	/// <param name="fileName">The name of the TypeScript file.</param>
	public TsFile(string fileName) => FileName = fileName;

	/// <summary>
	/// Adds an import to the file.
	/// </summary>
	/// <param name="import">The import to add.</param>
	/// <returns>The same <see cref="TsFile"/> instance (for chaining).</returns>
	public TsFile AddImport(TsImport import)
	{
		Imports.Add(import);
		return this;
	}

	/// <summary>
	/// Adds a declaration to the file.
	/// </summary>
	/// <param name="decl">The declaration to add.</param>
	/// <returns>The same <see cref="TsFile"/> instance (for chaining).</returns>
	public TsFile AddDeclaration(TsNode decl)
	{
		Declarations.Add(decl);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		foreach (TsImport imp in Imports)
			imp.Write(writer);

		if (Imports.Count > 0)
			writer.WriteLine();

		foreach (TsNode decl in Declarations)
		{
			decl.Write(writer);
			if (decl != Declarations.Last())
				writer.WriteLine();
		}
	}
}
