namespace cli.Services.Web.CodeGen;

public class TsConstructor : TsNode
{
	private string Name { get; }

	/// <summary>
	/// The parameters of the constructor.
	/// </summary>
	public List<TsConstructorParameter> Parameters { get; } = new();

	/// <summary>
	/// The implementation body as a sequence of nodes.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// Creates a constructor.
	/// </summary>
	public TsConstructor() => Name = "constructor";

	/// <summary>
	/// Adds a parameter to the constructor.
	/// </summary>
	/// <param name="param">The constructor parameter to add.</param>
	/// <returns>The current <see cref="TsConstructor"/> instance for chaining.</returns>
	public TsConstructor AddParameter(TsConstructorParameter param)
	{
		Parameters.Add(param);
		return this;
	}

	/// <summary>
	/// Adds statements to the implementation body of the constructor.
	/// </summary>
	/// <param name="nodes">The nodes making up the constructor body.</param>
	/// <returns>The current <see cref="TsConstructor"/> instance for chaining.</returns>
	public TsConstructor AddBody(params TsNode[] nodes)
	{
		Body.AddRange(nodes);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write(Name);

		if (Parameters.Count == 0)
			writer.Write("()");
		else
		{
			writer.WriteLine("(");
			writer.Indent();

			for (int i = 0; i < Parameters.Count; i++)
			{
				Parameters[i].Write(writer);
				if (i < Parameters.Count - 1)
					writer.WriteLine(",");
				else if (i == Parameters.Count - 1)
					writer.WriteLine();
			}

			writer.Outdent();
			writer.Write(")");
		}

		writer.WriteLine(" {");
		writer.Indent();

		foreach (TsNode statement in Body)
			statement.Write(writer);

		writer.Outdent();
		writer.WriteLine("}");
	}
}
