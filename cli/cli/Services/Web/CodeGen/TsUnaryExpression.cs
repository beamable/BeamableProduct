namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a unary operation (e.g. a + b, a - b, !a, ~a, ++a, --a, a++, a--).
/// </summary>
public class TsUnaryExpression : TsExpression
{
	// The position of the operator
	private readonly TsUnaryOperatorPosition _position;

	/// <summary>
	/// The operator of the unary operation.
	/// </summary>
	public TsUnaryOperatorType Operator { get; }

	/// <summary>
	/// The operand of the unary operation.
	/// </summary>
	public TsExpression Operand { get; }

	/// <summary>
	/// Creates a new unary expression.
	/// </summary>
	/// <param name="operand">The operand of the unary operation.</param>
	/// <param name="operator">The operator of the unary operation.</param>
	public TsUnaryExpression(TsExpression operand, TsUnaryOperatorType @operator,
		TsUnaryOperatorPosition position = TsUnaryOperatorPosition.Prefix)
	{
		Operator = @operator;
		Operand = operand;
		_position = position;
	}

	public override void Write(TsCodeWriter writer)
	{
		string op = Operator switch
		{
			TsUnaryOperatorType.Not => "!",
			TsUnaryOperatorType.BitwiseNot => "~",
			TsUnaryOperatorType.Increment => "++",
			TsUnaryOperatorType.Decrement => "--",
			_ => throw new ArgumentOutOfRangeException(nameof(Operator))
		};

		if (_position == TsUnaryOperatorPosition.Postfix)
		{
			Operand.Write(writer);
			writer.Write($"{op}");
		}
		else
		{
			writer.Write($"{op}");
			Operand.Write(writer);
		}
	}
}

public enum TsUnaryOperatorType
{
	Not,
	BitwiseNot,
	Increment,
	Decrement
}

public enum TsUnaryOperatorPosition
{
	Prefix,
	Postfix,
}
