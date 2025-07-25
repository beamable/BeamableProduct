namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript file.
/// </summary>
public class TsFile : TsNode
{
	private readonly bool _includeAutoGenComment;

	/// <summary>
	/// The name of the TypeScript file.
	/// </summary>
	public string FileName { get; private set; }

	/// <summary>
	/// The imports of the file.
	/// </summary>
	private List<TsImport> Imports { get; } = new();

	/// <summary>
	/// The exports of the file.
	/// </summary>
	private List<TsExport> Exports { get; } = new();

	/// <summary>
	/// The declarations of the file.
	/// </summary>
	private List<TsNode> Declarations { get; } = new();

	/// <summary>
	/// Creates a typescript file.
	/// </summary>
	/// <param name="fileName">The name of the TypeScript file.</param>
	public TsFile(string fileName, bool includeAutoGenComment = true)
	{
		FileName = fileName;
		_includeAutoGenComment = includeAutoGenComment;
	}

	/// <summary>
	/// Adds an import module to the file.
	/// </summary>
	/// <param name="import">The import module to add.</param>
	/// <returns>The same <see cref="TsFile"/> instance (for chaining).</returns>
	public TsFile AddImport(TsImport import)
	{
		Imports.Add(import);
		return this;
	}

	/// <summary>
	/// Adds an export module to the file.
	/// </summary>
	/// <param name="import">The export module to add.</param>
	/// <returns>The same <see cref="TsFile"/> instance (for chaining).</returns>
	public TsFile AddExport(TsExport export)
	{
		Exports.Add(export);
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
		if (_includeAutoGenComment)
		{
			var autoGenComment = new TsComment(
				"⚠️ THIS FILE IS AUTO-GENERATED. DO NOT EDIT MANUALLY.\nAll manual edits will be lost when this file is regenerated.",
				TsCommentStyle.Doc);

			autoGenComment.Write(writer);
			writer.WriteLine();
		}

		foreach (TsImport imp in Imports)
			imp.Write(writer);

		if (Imports.Count > 0 && (Exports.Count > 0 || Declarations.Count > 0))
			writer.WriteLine();

		foreach (TsExport exp in Exports)
			exp.Write(writer);

		if (Exports.Count > 0 && Declarations.Count > 0)
			writer.WriteLine();

		foreach (TsNode decl in Declarations)
		{
			decl.Write(writer);
			if (decl != Declarations.Last())
				writer.WriteLine();
		}
	}
}
