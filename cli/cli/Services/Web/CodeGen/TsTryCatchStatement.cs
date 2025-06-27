namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>try</c>/<c>catch</c>/<c>finally</c> statement.
/// </summary>
public class TsTryCatchStatement : TsNode
{
	/// <summary>
	/// The statements in the <c>try</c> block.
	/// </summary>
	public List<TsNode> TryBlock { get; } = new();

	/// <summary>
	/// The catch identifier.
	/// </summary>
	public TsExpression CatchIdentifier { get; }

	/// <summary>
	/// The statements in the <c>catch</c> block.
	/// </summary>
	public List<TsNode> CatchBlock { get; } = new();

	/// <summary>
	/// The statements in the <c>finally</c> block.
	/// </summary>
	public List<TsNode> FinallyBlock { get; } = new();

	/// <summary>
	/// Creates a new <c>try</c>/<c>catch</c>/<c>finally</c> statement.
	/// </summary>
	/// <param name="catchIdentifier">The catch identifier.</param>
	public TsTryCatchStatement(TsExpression catchIdentifier = null) => CatchIdentifier = catchIdentifier;

	/// <summary>
	/// Adds a statement to the <c>try</c> block.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsTryCatchStatement"/> instance for chaining.</returns>
	public TsTryCatchStatement AddTry(params TsNode[] statements)
	{
		TryBlock.AddRange(statements);
		return this;
	}

	/// <summary>
	/// Adds a statement to the <c>catch</c> block.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsTryCatchStatement"/> instance for chaining.</returns>
	public TsTryCatchStatement AddCatch(params TsNode[] statements)
	{
		CatchBlock.AddRange(statements);
		return this;
	}

	/// <summary>
	/// Adds a statement to the <c>finally</c> block.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsTryCatchStatement"/> instance for chaining.</returns>
	public TsTryCatchStatement AddFinally(params TsNode[] statements)
	{
		FinallyBlock.AddRange(statements);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		ValidateTryStatement();

		writer.WriteLine("try {");
		writer.Indent();

		foreach (TsNode statement in TryBlock)
			statement.Write(writer);

		writer.Outdent();

		if (CatchBlock.Count > 0)
		{
			writer.Write("} catch");

			if (CatchIdentifier != null)
			{
				writer.Write(" (");
				CatchIdentifier.Write(writer);
				writer.Write(")");
			}

			writer.WriteLine(" {");
			writer.Indent();

			foreach (TsNode statement in CatchBlock)
				statement.Write(writer);

			writer.Outdent();
			writer.WriteLine("}");
		}

		if (FinallyBlock.Count > 0)
		{
			writer.WriteLine("} finally {");
			writer.Indent();

			foreach (TsNode statement in FinallyBlock)
				statement.Write(writer);

			writer.Outdent();
			writer.WriteLine("}");
		}
	}

	/// <summary>
	/// Validates the <c>try</c> block.
	/// Throw if no <c>try</c> block is present.
	/// Throw if no <c>catch</c> or <c>finally</c> block is present.
	/// </summary>
	/// <exception cref="InvalidOperationException">
	/// Thrown if no <c>try</c> block is present.
	/// Thrown if no <c>catch</c> or <c>finally</c> block is present.
	/// </exception>
	private void ValidateTryStatement()
	{
		if (TryBlock.Count == 0)
			throw new InvalidOperationException("A try statement must have at least one try block.");

		if (CatchBlock.Count == 0 || FinallyBlock.Count == 0)
			throw new InvalidOperationException("A try statement must have at least one catch or finally block.");
	}
}
