namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an assignment statement.
/// </summary>
public class TsAssignmentStatement : TsNode
{
	/// <summary>
	/// The left-hand side of the assignment.
	/// </summary>
	public TsExpression Left { get; }

	/// <summary>
	/// The right-hand side of the assignment.
	/// </summary>
	public TsExpression Right { get; }

	/// <summary>
	/// Creates a new assignment statement.
	/// </summary>
	/// <param name="left">The left-hand side of the assignment.</param>
	/// <param name="right">The right-hand side of the assignment.</param>
	public TsAssignmentStatement(TsExpression left, TsExpression right)
	{
		Left = left;
		Right = right;
	}

	public override void Write(TsCodeWriter writer)
	{
		Left.Write(writer);
		writer.Write($" = ");
		Right.Write(writer);
		writer.WriteLine(";");
	}
}
