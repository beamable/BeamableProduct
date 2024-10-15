using JetBrains.Annotations;
using Serilog;
using Serilog.Core;
using Serilog.Enrichers.Sensitive;
using Serilog.Events;
using System.Text.RegularExpressions;

namespace cli.Utils;

public partial class TokenMasker : IMaskingOperator
{

	[GeneratedRegex("((token|Token|TOKEN).?.?.?.?.?)\"........-....-....-....-............", RegexOptions.None, "en-US")]
	public static partial Regex TokenRegex();

	public MaskingResult Mask(string input, string mask)
	{
		string input1 = this.PreprocessInput(input);
		if (!this.ShouldMaskInput(input1))
			return MaskingResult.NoMatch;
		string str = TokenRegex().Replace(input1, (MatchEvaluator)(match => this.ShouldMaskMatch(match) ? match.Result(this.PreprocessMask(this.PreprocessMask(mask), match)) : match.Value));
		return new MaskingResult()
		{
			Result = str,
			Match = str != input
		};
	}

	protected virtual bool ShouldMaskInput(string input) => true;

	protected virtual string PreprocessInput(string input) => input;

	protected virtual string PreprocessMask(string mask) => mask;


	protected string PreprocessMask(string mask, Match match)
	{
		return $"{match.Groups[1].Value}\"<hidden_token last4=({match.Value.Substring(match.Value.Length - 4)})>";
	}

	protected virtual bool ShouldMaskMatch(Match match) => true;
}
