using JetBrains.Annotations;
using Serilog.Enrichers.Sensitive;
using System.Text.RegularExpressions;

namespace cli.Utils;

public class TokenMasker : RegexMaskingOperator
{
	// public TokenMasker([NotNull] string regexString) : base(regexString)
	// {
	// }
	//
	// public TokenMasker([NotNull] string regexString, RegexOptions options) : base(regexString, options)
	// {
	// }

	public TokenMasker() : base("((token|Token|TOKEN).?.?.?.?.?)\"........-....-....-....-............")
	{
		
	}

	protected override string PreprocessMask(string mask, Match match)
	{
		return $"{match.Groups[1].Value}\"<hidden_token last4=({match.Value.Substring(match.Value.Length - 4)})>";
	}
}


// public class EmailAddressMaskingOperator : RegexMaskingOperator
// {
// 	private const string EmailPattern = "(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])";
//
// 	public EmailAddressMaskingOperator()
// 		: base("(?:[a-z0-9!#$%&'*+/=?^_`{|}~-]+(?:\\.[a-z0-9!#$%&'*+/=?^_`{|}~-]+)*|\"(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21\\x23-\\x5b\\x5d-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])*\")@(?:(?:[a-z0-9](?:[a-z0-9-]*[a-z0-9])?\\.)+[a-z0-9](?:[a-z0-9-]*[a-z0-9])?|\\[(?:(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\\.){3}(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?|[a-z0-9-]*[a-z0-9]:(?:[\\x01-\\x08\\x0b\\x0c\\x0e-\\x1f\\x21-\\x5a\\x53-\\x7f]|\\\\[\\x01-\\x09\\x0b\\x0c\\x0e-\\x7f])+)\\])", RegexOptions.Compiled | RegexOptions.IgnoreCase)
// 	{
// 	}
//
// 	protected override string PreprocessInput(string input)
// 	{
// 		if (input.Contains("%40"))
// 			input = input.Replace("%40", "@");
// 		return input;
// 	}
//
// 	protected override bool ShouldMaskInput(string input) => input.Contains("@");
// }
