namespace cli.Services.Web.CodeGen;

/// <summary>
/// Represents an array destructuring parameter (e.g. [a, b]) in a parameter list.
/// </summary>
public class TsArrayDestructureParameter : TsParameter
{
	/// <summary>
	/// The element names within the array pattern.
	/// </summary>
	public string[] Elements { get; }

	/// <summary>
	/// Creates a new array destructuring parameter.
	/// </summary>
	public TsArrayDestructureParameter(params string[] elements)
		: base(string.Empty, TsType.Any)
	{
		Elements = elements;
	}

	public override void Write(TsCodeWriter writer)
	{
		writer.Write("[");
		for (int i = 0; i < Elements.Length; i++)
		{
			writer.Write(Elements[i]);
			if (i < Elements.Length - 1)
				writer.Write(", ");
		}

		writer.Write("]");
	}
}
