using NUnit.Framework;
using cli.Services.Web.Helpers;

namespace tests.Web.Helpers;

[TestFixture]
public class StringHelperTests
{
	[TestCase("abc:def?ghi", "abc_def_ghi")]
	[TestCase("123abc", "_123abc")]
	[TestCase("", "")]
	[TestCase("a b\tc", "a_b_c")]
	public void ToSafeIdentifier_SanitizesInput(string input, string expected)
	{
		var result = StringHelper.ToSafeIdentifier(input);
		Assert.AreEqual(expected, result);
	}

	[TestCase(null, "")]
	[TestCase("", "")]
	[TestCase("   ", "")]
	[TestCase("hello world-this_isAtest", "HelloWorldThis_IsAtest")]
	[TestCase("123abc def", "123abcDef")]
	public void ToPascalCaseIdentifier_ConvertsToPascalCase(string input, string expected)
	{
		var result = StringHelper.ToPascalCaseIdentifier(input);
		Assert.AreEqual(expected, result);
	}

	[TestCase("one,two,Three", "OneTwoThree")]
	public void ConcatCapitalize_ConcatenatesCapitalizedSegments(string segmentsCsv, string expected)
	{
		var segments = segmentsCsv.Split(',');
		var result = StringHelper.ConcatCapitalize(segments);
		Assert.AreEqual(expected, result);
	}

	[TestCase("test", "Test")]
	[TestCase("", "")]
	[TestCase(null, null)]
	public void Capitalize_UppercasesFirstLetter(string input, string expected)
	{
		var result = StringHelper.Capitalize(input);
		Assert.AreEqual(expected, result);
	}
}
