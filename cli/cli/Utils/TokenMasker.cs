using JetBrains.Annotations;
using Serilog.Enrichers.Sensitive;
using System.Text.RegularExpressions;

namespace cli.Utils;

public class TokenMasker : RegexMaskingOperator
{
	public TokenMasker() : base("((token|Token|TOKEN).?.?.?.?.?)\"........-....-....-....-............")
	{

	}

	protected override string PreprocessMask(string mask, Match match)
	{
		return $"{match.Groups[1].Value}\"<hidden_token last4=({match.Value.Substring(match.Value.Length - 4)})>";
	}
}
