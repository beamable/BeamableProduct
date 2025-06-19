using System.Collections;

namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents a literal value (string, number, boolean, null, undefined, Array, Object).
/// <example>
/// <code>
/// new TsLiteralExpression("foo"); // "foo"
/// new TsLiteralExpression(42); // 42
/// new TsLiteralExpression(true); // true
/// new TsLiteralExpression(null); // null
/// new TsLiteralExpression("undefined"); // undefined
/// new TsLiteralExpression(new[] { 1, 2, 3 }); // [1, 2, 3]
/// new TsLiteralExpression(new Dictionary&lt;string, object&gt; { { "foo", "bar" } }); // { "foo": "bar" }
/// </code>
/// </example>
/// </summary>
public class TsLiteralExpression : TsExpression
{
	/// <summary>
	/// The literal value.
	/// </summary>
	public object Value { get; }

	/// <summary>
	/// Creates a new literal expression.
	/// </summary>
	/// <param name="value"></param>
	public TsLiteralExpression(object value) => Value = value;

	public override void Write(TsCodeWriter writer)
	{
		switch (Value)
		{
			case null:
				writer.Write("null");
				break;
			case "undefined":
				writer.Write("undefined");
				break;
			case string s:
				writer.Write($"\"{s}\"");
				break;
			case bool b:
				writer.Write(b.ToString().ToLower());
				break;
			case IDictionary dictionary:
			{
				writer.WriteLine("{");
				writer.Indent();
				bool first = true;

				foreach (DictionaryEntry entry in dictionary)
				{
					if (!first)
						writer.WriteLine(",");

					first = false;
					new TsLiteralExpression(entry.Key).Write(writer);
					writer.Write(": ");
					new TsLiteralExpression(entry.Value).Write(writer);
				}

				writer.WriteLine();
				writer.Outdent();
				writer.Write("}");
				break;
			}
			case IEnumerable enumerable:
			{
				writer.Write("[");
				bool first = true;

				foreach (object item in enumerable)
				{
					if (!first)
						writer.Write(", ");

					first = false;
					new TsLiteralExpression(item).Write(writer);
				}

				writer.Write("]");
				break;
			}
			default:
				writer.Write(Value.ToString());
				break;
		}
	}
}
