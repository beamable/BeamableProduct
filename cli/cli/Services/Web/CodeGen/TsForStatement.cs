namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a <c>for</c> loop.
/// </summary>
public class TsForStatement : TsNode
{
	/// <summary>
	/// The initializer of the loop.
	/// </summary>
	public TsNode Initializer { get; }

	/// <summary>
	/// The condition of the loop.
	/// </summary>
	public TsExpression Condition { get; }

	/// <summary>
	/// The iterator of the loop.
	/// </summary>
	public TsExpression Iterator { get; }

	/// <summary>
	/// The statements in the body of the loop.
	/// </summary>
	public List<TsNode> Body { get; } = new();

	/// <summary>
	/// Creates a new <c>for</c> loop.
	/// </summary>
	/// <param name="initializer">The initializer of the loop.</param>
	/// <param name="condition">The condition of the loop.</param>
	/// <param name="iterator">The iterator of the loop.</param>
	/// <example>
	/// <code>
	/// var init = new TsVariable("i").WithInitializer(new TsLiteralExpression(0)).AsExpression();
	/// var cond = new TsBinaryExpression(iVar.Identifier, TsBinaryOperator.LessThan, new TsLiteralExpression(10));
	/// var iter = new TsUnaryExpression(iVar.Identifier, TsUnaryOperator.Increment, TsUnaryOperatorPosition.Postfix);
	/// new TsForStatement(init, cond, iter)
	///     .AddBody(new TsInvokeExpression(new TsIdentifier("console.log"), new TsIdentifier("i")));
	/// </code>
	/// </example>
	/// <remarks>The example is equivalent to the following:
	/// <code>
	/// for (let i = 0; i &lt; 10; i++) {
	///     console.log(i);
	/// }
	/// </code>
	/// </remarks>
	public TsForStatement(TsNode initializer, TsExpression condition, TsExpression iterator)
	{
		Initializer = initializer;
		Condition = condition;
		Iterator = iterator;
	}

	/// <summary>
	/// Adds a statement to the body of the loop.
	/// </summary>
	/// <param name="statements">The statements to add.</param>
	/// <returns>The current <see cref="TsForStatement"/> instance for chaining.</returns>
	public TsForStatement AddBody(params TsNode[] statements)
	{
		Body.AddRange(statements);
		return this;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("for (");
		Initializer?.Write(writer);
		writer.Write("; ");
		Condition?.Write(writer);
		writer.Write("; ");
		Iterator?.Write(writer);
		writer.WriteLine(") {");
		writer.Indent();

		foreach (TsNode statement in Body)
			statement.Write(writer);

		writer.Outdent();
		writer.WriteLine("}");
	}
}
