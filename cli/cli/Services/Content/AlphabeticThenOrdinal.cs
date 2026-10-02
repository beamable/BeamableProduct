namespace cli.Content;

/// <summary>
/// Orders strings alphabetically so that 'Sydney' and 'system' both come after 'address' and 'Adelaide', then uses Ordinal as a tie-breaker so that 'Steam' is always before 'steam'.
/// </summary>
internal sealed class AlphabeticThenOrdinal : IComparer<string>
{
	public static readonly AlphabeticThenOrdinal Instance = new();

	public int Compare(string x, string y)
	{
		var c = StringComparer.OrdinalIgnoreCase.Compare(x, y);
		return c != 0 ? c : StringComparer.Ordinal.Compare(x, y);
	}
}
