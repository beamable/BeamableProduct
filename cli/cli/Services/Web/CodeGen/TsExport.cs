namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript export (re-export) statement.
/// </summary>
public class TsExport : TsNode
{
	/// <summary>
	/// The module to export from.
	/// </summary>
	public string Module { get; }

	/// <summary>
	/// The alias for the default export, if specified.
	/// </summary>
	public string DefaultExport { get; }

	/// <summary>
	/// The default export identifier.
	/// </summary>
	public TsExpression DefaultExportIdentifier { get; private set; }

	/// <summary>
	/// The named exports to include.
	/// </summary>
	public List<string> NamedExports { get; } = new();

	/// <summary>
	/// The named export identifiers.
	/// </summary>
	public Dictionary<string, TsExpression> NamedExportIdentifiers { get; } = new();

	/// <summary>
	/// Whether this export is a wildcard export (export * from ...).
	/// </summary>
	public bool IsWildcard => string.IsNullOrEmpty(DefaultExport) && NamedExports.Count == 0;

	/// <summary>
	/// Creates a new export for the given module.
	/// </summary>
	/// <param name="module">The module to export from.</param>
	/// <param name="defaultExport">The alias for the module's default export.</param>
	public TsExport(string module, string defaultExport = null)
	{
		Module = module;
		DefaultExport = defaultExport;
		if (!string.IsNullOrEmpty(defaultExport))
			DefaultExportIdentifier = new TsIdentifier(defaultExport);
	}

	/// <summary>
	/// Adds a named export to the export statement.
	/// </summary>
	/// <param name="name">The export name.</param>
	/// <returns>The current <see cref="TsExport"/> instance for chaining.</returns>
	public TsExport AddNamedExport(string name)
	{
		NamedExports.Add(name);
		NamedExportIdentifiers.Add(name, new TsIdentifier(name));
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		if (IsWildcard)
		{
			writer.WriteLine($"export * from '{Module}';");
		}
		else
		{
			var parts = new List<string>();
			if (!string.IsNullOrEmpty(DefaultExport))
				parts.Add($"default as {DefaultExport}");
			if (NamedExports.Count > 0)
				parts.AddRange(NamedExports);
			writer.WriteLine($"export {{ {string.Join(", ", parts)} }} from '{Module}';");
		}
	}
}
