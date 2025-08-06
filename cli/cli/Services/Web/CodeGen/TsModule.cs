namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a TypeScript module declaration.
/// </summary>
public class TsModule : TsNode
{
	/// <summary>
	/// The name of the module.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// The statements inside the module block.
	/// </summary>
	public List<TsNode> Statements { get; } = new();

	/// <summary>
	/// Creates a new module.
	/// </summary>
	/// <param name="name">The name of the module (e.g., 'beamable-sdk').</param>
	public TsModule(string name)
	{
		Name = name;
	}

	/// <summary>
	/// Adds a statement to the module block's body.
	/// </summary>
	/// <param name="statement">The statement to add.</param>
	/// <returns>The current <see cref="TsModule"/> instance for chaining.</returns>
	public TsModule AddStatement(TsNode statement)
	{
		Statements.Add(statement);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		if (Statements.Count == 0)
		{
			writer.WriteLine($"module \'{Name}\';");
			return;
		}

		writer.Write($"module \'{Name}\'");
		writer.WriteLine(" {");
		writer.Indent();

		foreach (var stmt in Statements)
		{
			stmt.Write(writer);
		}

		writer.Outdent();
		writer.WriteLine("}");
	}
}
