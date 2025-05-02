namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a binary operation (e.g. a + b).
/// </summary>
public class TsBinaryExpression : TsExpression
{
	/// <summary>
	/// The left operand of the binary operation.
	/// </summary>
	public TsExpression Left { get; }

	/// <summary>
	/// The operator of the binary operation.
	/// </summary>
	public TsBinaryOperatorType Operator { get; }

	/// <summary>
	/// The right operand of the binary operation.
	/// </summary>
	public TsExpression Right { get; }

	/// <summary>
	/// Creates a new binary expression.
	/// </summary>
	/// <param name="left">The left operand of the binary operation.</param>
	/// <param name="operator">The operator of the binary operation.</param>
	/// <param name="right">The right operand of the binary operation.</param>
	public TsBinaryExpression(TsExpression left, TsBinaryOperatorType @operator, TsExpression right)
	{
		Left = left;
		Operator = @operator;
		Right = right;
	}

	public override void Write(TsCodeWriter writer)
	{
		Left.Write(writer);

		string op = Operator switch
		{
			TsBinaryOperatorType.Plus => "+",
			TsBinaryOperatorType.Minus => "-",
			TsBinaryOperatorType.Multiply => "*",
			TsBinaryOperatorType.Divide => "/",
			TsBinaryOperatorType.Modulo => "%",
			TsBinaryOperatorType.EqualTo => "==",
			TsBinaryOperatorType.NotEqualTo => "!=",
			TsBinaryOperatorType.GreaterThan => ">",
			TsBinaryOperatorType.LessThan => "<",
			TsBinaryOperatorType.GreaterThanOrEqual => ">=",
			TsBinaryOperatorType.LessThanOrEqual => "<=",
			TsBinaryOperatorType.BitwiseAnd => "&",
			TsBinaryOperatorType.BitwiseOr => "|",
			TsBinaryOperatorType.BitwiseXor => "^",
			TsBinaryOperatorType.LeftShift => "<<",
			TsBinaryOperatorType.RightShift => ">>",
			_ => throw new ArgumentOutOfRangeException(nameof(Operator))
		};

		writer.Write($" {op} ");
		Right.Write(writer);
	}
}

public class TsBinaryOperator : TsNode
{
	/// <summary>
	/// The operator string.
	/// </summary>
	public string Operator { get; }

	/// <summary>
	/// Creates a new binary operator.
	/// </summary>
	/// <param name="operator">The operator string.</param>
	public TsBinaryOperator(string @operator)
	{
		Operator = @operator;
	}

	public override void Write(TsCodeWriter writer) => writer.Write(Operator);
}

public enum TsBinaryOperatorType
{
	Plus,
	Minus,
	Multiply,
	Divide,
	Modulo,
	EqualTo,
	NotEqualTo,
	GreaterThan,
	LessThan,
	GreaterThanOrEqual,
	LessThanOrEqual,
	BitwiseAnd,
	BitwiseOr,
	BitwiseXor,
	LeftShift,
	RightShift
}
