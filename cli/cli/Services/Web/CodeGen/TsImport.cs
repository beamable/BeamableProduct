namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript import.
/// </summary>
public class TsImport : TsNode
{
	public bool TypeImportOnly { get; private set; }

	/// <summary>
	/// The default import identifier.
	/// </summary>
	public TsExpression DefaultImportIdentifier { get; private set; }

	/// <summary>
	/// The named import identifiers.
	/// </summary>
	public Dictionary<string, TsExpression> NamedImportIdentifiers { get; private set; } = new();

	/// <summary>
	/// The module to import from.
	/// </summary>
	public string Module { get; }

	/// <summary>
	/// The default import to use if no named imports are specified.
	/// </summary>
	public string DefaultImport { get; }

	/// <summary>
	/// The named imports to use if any.
	/// </summary>
	public List<string> NamedImports { get; } = new();

	/// <summary>
	/// Whether this import is a side-effect only import.
	/// </summary>
	public bool IsSideEffectOnly => DefaultImport == null && NamedImports.Count == 0;

	/// <summary>
	/// Creates a new import.
	/// </summary>
	/// <param name="module">The module to import from.</param>
	/// <param name="defaultImport">The default import to use if no named imports are specified.</param>
	/// <param name="typeImportOnly">Whether this import is a type-only import.</param>
	public TsImport(string module, string defaultImport = null, bool typeImportOnly = false)
	{
		Module = module;
		DefaultImport = defaultImport;
		DefaultImportIdentifier = new TsIdentifier(defaultImport);
		TypeImportOnly = typeImportOnly;
	}

	/// <summary>
	/// Adds a named import to the import.
	/// </summary>
	/// <param name="name">The name of the named import.</param>
	/// <returns>The current <see cref="TsImport"/> instance for chaining.</returns>
	public TsImport AddNamedImport(string name)
	{
		NamedImports.Add(name);
		NamedImportIdentifiers.Add(name, new TsIdentifier(name));
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateTypeOnlyImport();

		if (IsSideEffectOnly)
		{
			writer.WriteLine($"import '{Module}';");
		}
		else
		{
			var parts = new List<string>();

			if (!string.IsNullOrEmpty(DefaultImport))
				parts.Add(DefaultImport);

			if (NamedImports.Count > 0)
				parts.Add($"{{ {string.Join(", ", NamedImports)} }}");

			var type = TypeImportOnly ? "type " : string.Empty;
			writer.WriteLine($"import {type}{string.Join(", ", parts)} from '{Module}';");
		}
	}

	private void ValidateTypeOnlyImport()
	{
		if (!TypeImportOnly)
			return;

		if (DefaultImport != null && NamedImports.Count > 0)
			throw new InvalidOperationException(
				"Type-only imports cannot have both a default import and named imports.");
	}
}
